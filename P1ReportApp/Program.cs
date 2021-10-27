using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
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

                var connection = new SqliteConnection($"Data Source={arg}");

                await connection.OpenAsync();
                var data = GetElectricityNumbers(year, month, day, connection);

                var xs = data.Select(i => (double)i.hour).ToArray();
                var y1 = data.Select(i => (double)i.ElectricityDeliveredByClient).ToArray();
                var y2 = data.Select(i => (double)i.ElectricityDeliveredToClient).ToArray();
                {
                    var plt = new ScottPlot.Plot(600, 400);

                    var bar1 = plt.AddBar(y1, xs);
                    bar1.FillColor = Color.Gray;
                    bar1.FillColorHatch = Color.Black;
                    bar1.Label = "ElectricityDeliveredByClient";

                    var bar2 = plt.AddBar(y2, xs);
                    bar2.FillColor = Color.DodgerBlue;
                    bar2.FillColorHatch = Color.DeepSkyBlue;
                    bar2.Label = "ElectricityDeliveredToClient";

                    plt.XTicks(xs, xs.Select(s => s.ToString()).ToArray());

                    plt.Legend(location: ScottPlot.Alignment.UpperRight);

                    plt.SaveFig("c:\\temp\\bar_pattern.png");
                }
                {
                    var plt = new ScottPlot.Plot(600, 400);

                    // plot the data
                    plt.AddScatter(xs, y1, label: "ElectricityDeliveredByClient");
                    plt.AddScatter(xs, y2, label: "ElectricityDeliveredToClient");

                    // customize the axis labels
                    plt.XLabel("Hour of the day");

                    plt.XTicks(xs, xs.Select(s => s.ToString()).ToArray());

                    plt.Legend(location: ScottPlot.Alignment.UpperRight);

                    plt.SaveFig("c:\\temp\\quickstart_scatter.png");
                }
            }
        }

        struct ElectricityNumbers
        {
            public int hour;
            public decimal ElectricityDeliveredToClient;
            public decimal ElectricityDeliveredByClient;
        }

        private static IEnumerable<ElectricityNumbers> GetElectricityNumbers(int year, int month, int day, SqliteConnection connection)
        {
            var baseDate = new DateTime(year, month, day);

            for (int i = 0; i < 24; i++)
            {
                using var cmd = connection.CreateCommand();

                var minDate = cmd.Parameters.Add("@MinDate", SqliteType.Text);
                minDate.Value = baseDate.AddHours(i).ToString("yyyy-MM-dd HH:mm:ss");

                var maxDate = cmd.Parameters.Add("@MaxDate", SqliteType.Text);
                maxDate.Value = baseDate.AddHours(i + 1).ToString("yyyy-MM-dd HH:mm:ss");

                cmd.CommandText = @"
select
    min(ElectricityDeliveredToClientTariff1+ElectricityDeliveredToClientTariff2),
    max(ElectricityDeliveredToClientTariff1+ElectricityDeliveredToClientTariff2),
    min(ElectricityDeliveredByClientTariff1+ElectricityDeliveredByClientTariff2),
    max(ElectricityDeliveredByClientTariff1+ElectricityDeliveredByClientTariff2)
from
    p1power
where
    timestamp between @MinDate and @MaxDate;";

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        if (reader.IsDBNull(0) || reader.IsDBNull(1)) continue;

                        yield return new ElectricityNumbers
                        {
                            hour = i,
                            ElectricityDeliveredByClient = reader.GetDecimal(3) - reader.GetDecimal(2),
                            ElectricityDeliveredToClient = reader.GetDecimal(1) - reader.GetDecimal(0),
                        };
                    }
                }
            }
        }
    }
}
