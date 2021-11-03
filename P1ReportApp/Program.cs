using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace P1ReportApp
{
    class Program
    {
        private static Regex _dateRegex = new Regex(@"^(?<Year>[0-9]{4})(?<Month>[0-9]{2})(?<Day>[0-9]{2})");

        private static async Task Main(
            string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            foreach (var arg in args)
            {
                if (!File.Exists(arg)) continue;

                var match = _dateRegex.Match(new FileInfo(arg).Name);
                if (!match.Success) continue;

                int year = int.Parse(match.Groups["Year"].Value);
                int month = int.Parse(match.Groups["Month"].Value);
                int day = int.Parse(match.Groups["Day"].Value);
                var baseDate = new DateTime(year, month, day);

                var connection = new SqliteConnection($"Data Source={arg}");

                await connection.OpenAsync();
                {
                    var data = GetElectricityNumbers(baseDate, connection);

                    CreateGraph($"Hour of the day on {year}-{month}-{day}", $"p1power_perhour_{year}-{month}-{day}.png", data);
                }
                {
                    var data = GetNumbersBetween(connection, baseDate, baseDate.AddDays(1), day);
                    Console.WriteLine($"Totaal Opgeleverd   : {data.ActualElectricityPowerDelivery:000000.000000}");
                    Console.WriteLine($"Totaal Afgenomen    : {data.ActualElectricityPowerDraw:000000.000000}");
                    Console.WriteLine($"Totaal Netto afname : {data.NetActualElectricityPower:000000.000000}");
                }
            }
        }

        private static void CreateGraph(
            string label,
            string filename,
            IEnumerable<ElectricityNumbers> data)
        {
            var xs = data.Select(i => (double)i.hour).ToArray();
            var y3 = data.Select(i => (double)i.ActualElectricityPowerDelivery).ToArray();
            var y4 = data.Select(i => (double)i.ActualElectricityPowerDraw).ToArray();
            var y5 = data.Select(i => (double)i.NetActualElectricityPower).ToArray();
            {
                var plt = new ScottPlot.Plot(600, 400);

                // plot the data
                plt.AddScatter(xs, y3, label: "Opgeleverd");
                plt.AddScatter(xs, y4, label: "Afgenomen");
                plt.AddScatter(xs, y5, label: "Netto afname");

                // customize the axis labels
                plt.XLabel(label);

                plt.XTicks(xs, xs.Select(s => s.ToString()).ToArray());

                plt.Legend(location: ScottPlot.Alignment.LowerLeft);

                plt.SaveFig($"c:\\temp\\{filename}");
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

                yield return GetNumbersBetween(connection, minDate, maxDate, i);
            }
        }

        private static ElectricityNumbers GetNumbersBetween(
            SqliteConnection connection,
            DateTime minDate,
            DateTime maxDate,
            int i)
        {
            using var cmd = connection.CreateCommand();

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
                    ElectricityDeliveredByClient = reader.GetDecimal(3) - reader.GetDecimal(2),
                    ElectricityDeliveredToClient = reader.GetDecimal(1) - reader.GetDecimal(0),
                    ActualElectricityPowerDelivery = reader.GetDecimal(4),
                    ActualElectricityPowerDraw = reader.GetDecimal(5),
                    NetActualElectricityPower = reader.GetDecimal(6),
                };
            }
        }
    }
}

