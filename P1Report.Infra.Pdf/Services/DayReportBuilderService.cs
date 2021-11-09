using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using P1Report.Infra.Pdf.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Contracts;

namespace P1Report.Infra.Pdf.Services
{
    public class DayReportBuilderService :
        IReportBuilder<FileInfo>
    {
        private readonly Regex _dateRegex = new Regex(@"^(?<Year>[0-9]{4})(?<Month>[0-9]{2})(?<Day>[0-9]{2})");
        private readonly IConverter _converter;
        private readonly ILogger _logger;

        public string ReportOutputPath { get; set; }

        public DayReportBuilderService(
            IConfiguration config,
            IConverter converter,
            ILogger logger)
        {
            _converter = converter;
            _logger = logger;

            ReportOutputPath = config.GetValue<string>("ReportOutputPath");
        }

        public async Task BuildReport(
            FileInfo dataSourceFile)
        {
            _logger.Debug("Building report for data soruce file {dataSourceFile}", dataSourceFile);

            if (!dataSourceFile.Exists)
            {
                _logger.Error("Data source file does not exist");

                return;
            }

            var match = _dateRegex.Match(dataSourceFile.Name);
            if (!match.Success)
            {
                _logger.Error("Data source file filename does not start with a date. The first 8 characters should mark a date. example: '20211101-p1power.db'");

                return;
            }

            int year = int.Parse(match.Groups["Year"].Value);
            int month = int.Parse(match.Groups["Month"].Value);
            int day = int.Parse(match.Groups["Day"].Value);
            var baseDate = new DateTime(year, month, day);

            using (var connection = new SqliteConnection($"Data Source={dataSourceFile.FullName}"))
            {
                _logger.Debug("Connecting to data source file {dataSourceFile}", dataSourceFile);

                await connection.OpenAsync();

                _logger.Debug("Getting data");

                var electricityNumbersPerHourData = GetElectricityNumbers(baseDate, connection);
                var electricityNumbersAllDayData = GetNumbersBetween(connection, baseDate, baseDate.AddDays(1), day);

                _logger.Debug("Creating graph");

                var graphData = CreateGraph(
                    $"Uur van de dag",
                    electricityNumbersPerHourData);

                var sb = new StringBuilder();

                sb.AppendLine("<html>");
                sb.AppendLine("<style type=\"text/css\">");
                sb.AppendLine("html, body, h1, p, table, tr, td { font-family: \"Segoe UI\"; }");
                sb.AppendLine("</style>");
                sb.AppendLine("<body>");
                sb.AppendLine($"<h1>Slimme meter data voor {baseDate:dd MMMM yyyy}</h1>");
                sb.AppendLine("<table style=\"border-spacing:10px;\">");
                sb.AppendLine($"<tr><td style=\"width:150px;text-align:right;\">Totaal opgeleverd:</td><td>{electricityNumbersAllDayData.ActualElectricityPowerDelivery:0000.00000} kWh</td></tr>");
                sb.AppendLine($"<tr><td style=\"text-align:right;\">Totaal afgenomen:</td><td>{electricityNumbersAllDayData.ActualElectricityPowerDraw:0000.00000} kWh</td></tr>");
                sb.AppendLine($"<tr><td style=\"text-align:right;\">Totaal netto afname:</td><td>{electricityNumbersAllDayData.NetActualElectricityPower:0000.00000} kWh</td></tr>");
                sb.AppendLine("</table>");
                sb.AppendLine(GetDataURL(graphData));
                sb.AppendLine("</body>");
                sb.AppendLine("</html>");

                var doc = new HtmlToPdfDocument()
                {
                    GlobalSettings =
                    {
                        ColorMode = ColorMode.Color,
                        Orientation = Orientation.Portrait,
                        PaperSize = PaperKind.A4Plus,
                    },
                    Objects =
                    {
                        new ObjectSettings()
                        {
                            PagesCount = true,
                            HtmlContent = sb.ToString(),
                            WebSettings = { DefaultEncoding = "utf-8" },
                        }
                    }
                };

                _logger.Debug("Convert to pdf");

                byte[] pdf = _converter.Convert(doc);

                var outputFile = Path.Combine(ReportOutputPath, dataSourceFile.Name.Replace(dataSourceFile.Extension, ".pdf"));

                _logger.Debug($"Wrinting pdf data to {outputFile}");

                File.WriteAllBytes(outputFile, pdf);

                _logger.Debug("Done building report");
            }
        }

        public static string GetDataURL(
            byte[] bitmap)
        {
            return "<img src=\"data:image/bmp"
                        + ";base64,"
                        + Convert.ToBase64String(bitmap) + "\" />";
        }

        private static byte[] CreateGraph(
            string label,
            IEnumerable<ElectricityNumbers> data)
        {
            var xs = data.Select(i => (double)i.hour).ToArray();
            var y3 = data.Select(i => (double)i.ActualElectricityPowerDelivery).ToArray();
            var y4 = data.Select(i => (double)i.ActualElectricityPowerDraw).ToArray();
            var y5 = data.Select(i => (double)i.NetActualElectricityPower).ToArray();
            {
                var plt = new ScottPlot.Plot(1024, 768);

                // plot the data
                plt.AddScatter(xs, y3, label: "Opgeleverd");
                plt.AddScatter(xs, y4, label: "Afgenomen");
                plt.AddScatter(xs, y5, label: "Netto afname");

                plt.XTicks(xs, xs.Select(s => s.ToString()).ToArray());

                plt.Legend(location: ScottPlot.Alignment.LowerLeft);

                return plt.GetImageBytes(false, 1);
            }
        }

        class ElectricityNumbers
        {
            public int hour;
            public decimal ElectricityDeliveredToClient;
            public decimal ElectricityDeliveredByClient;
            public decimal ActualElectricityPowerDelivery;
            public decimal ActualElectricityPowerDraw;
            public decimal NetActualElectricityPower;
        }

        private static IEnumerable<ElectricityNumbers> GetElectricityNumbers(
            DateTime baseDate,
            SqliteConnection connection)
        {
            for (int i = 0; i < 24; i++)
            {
                var minDate = baseDate.AddHours(i);

                var maxDate = baseDate.AddHours(i + 1);

                var result = GetNumbersBetween(connection, minDate, maxDate, i);

                if (result != null)
                {
                    yield return result;
                }
            }
        }

        private static ElectricityNumbers GetNumbersBetween(
            SqliteConnection connection,
            DateTime minDate,
            DateTime maxDate,
            int i)
        {
            using (var cmd = connection.CreateCommand())
            {

                var minDateParam = cmd.Parameters.Add("@MinDate", SqliteType.Text);
                minDateParam.Value = minDate.ToString("yyyy-MM-dd HH:mm:ss");

                var maxDateParam = cmd.Parameters.Add("@MaxDate", SqliteType.Text);
                maxDateParam.Value = maxDate.ToString("yyyy-MM-dd HH:mm:ss");

                cmd.CommandText = @"
select
    min(ElectricityDeliveredToClientTariff1+ElectricityDeliveredToClientTariff2),
    max(ElectricityDeliveredToClientTariff1+ElectricityDeliveredToClientTariff2),
    min(ElectricityDeliveredByClientTariff1+ElectricityDeliveredByClientTariff2),
    max(ElectricityDeliveredByClientTariff1+ElectricityDeliveredByClientTariff2),
    sum(ActualElectricityPowerDelivery),
    sum(ActualElectricityPowerDraw),
    sum(NetActualElectricityPower)
from
    p1power
where
    timestamp between @MinDate and @MaxDate
	and ElectricityDeliveredToClientTariff1 > 0 
	and ElectricityDeliveredToClientTariff2 > 0
	and ElectricityDeliveredByClientTariff1 > 0 
	and ElectricityDeliveredByClientTariff2 > 0;";

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2) || reader.IsDBNull(3) || reader.IsDBNull(4) || reader.IsDBNull(5) || reader.IsDBNull(6))
                    {
                        return null;
                    }

                    return new ElectricityNumbers
                    {
                        hour = i,
                        ElectricityDeliveredByClient = (reader.GetDecimal(3) - reader.GetDecimal(2)) / 100,
                        ElectricityDeliveredToClient = (reader.GetDecimal(1) - reader.GetDecimal(0)) / 100,
                        ActualElectricityPowerDelivery = reader.GetDecimal(4) / 100,
                        ActualElectricityPowerDraw = reader.GetDecimal(5) / 100,
                        NetActualElectricityPower = reader.GetDecimal(6) / 100,
                    };
                }
            }
        }
    }
}
