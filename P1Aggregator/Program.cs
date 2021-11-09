using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using P1Reader.Domain.P1;
using P1ReaderApp.Storage;
using Serilog;
using System.IO;
using System.Threading.Tasks;

namespace P1Aggregator
{
    class Program
    {
        private static async Task Main(
            string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .CreateLogger();

            var mysqlStorage = new MysqlDbStorage(config);

            foreach (var arg in args)
            {
                if (!File.Exists(arg))
                {
                    Log.Logger.Information($"Cannot find sqlite db file '{arg}'");

                    continue;
                }

                Log.Logger.Information($"Aggregating data from {arg}...");

                using (var connection = new SqliteConnection($"Data Source={arg}"))
                {
                    await connection.OpenAsync();
                    using var command = connection.CreateCommand();

                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = query;

                    using var reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        var measurement = new Measurement
                        {
                            ActualElectricityPowerDelivery = reader.GetDecimal(1),
                            ActualElectricityPowerDraw = reader.GetDecimal(2),
                            ElectricityDeliveredByClientTariff1 = reader.GetDecimal(3),
                            ElectricityDeliveredByClientTariff2 = reader.GetDecimal(4),
                            ElectricityDeliveredToClientTariff1 = reader.GetDecimal(5),
                            ElectricityDeliveredToClientTariff2 = reader.GetDecimal(6),
                            InstantaneousActivePowerDeliveryL1 = reader.GetDecimal(7),
                            InstantaneousActivePowerDeliveryL2 = reader.GetDecimal(8),
                            InstantaneousActivePowerDeliveryL3 = reader.GetDecimal(9),
                            InstantaneousActivePowerDrawL1 = reader.GetDecimal(10),
                            InstantaneousActivePowerDrawL2 = reader.GetDecimal(11),
                            InstantaneousActivePowerDrawL3 = reader.GetDecimal(12),
                            InstantaneousCurrentL1 = reader.GetInt32(13),
                            InstantaneousCurrentL2 = reader.GetInt32(14),
                            InstantaneousCurrentL3 = reader.GetInt32(15),
                            InstantaneousVoltageL1 = reader.GetDecimal(16),
                            InstantaneousVoltageL2 = reader.GetDecimal(17),
                            InstantaneousVoltageL3 = reader.GetDecimal(18),
                            NetActualElectricityPower = reader.GetDecimal(19),
                            TotalInstantaneousCurrent = reader.GetInt32(20),
                            TotalInstantaneousVoltage = reader.GetDecimal(21),
                            TimeStamp = reader.GetDateTime(22)
                        };

                        await mysqlStorage.SaveP1MeasurementAsync(measurement);
                    }
                }

                File.Move(arg, arg + ".done");

                Log.Logger.Information($"done!\n");
            }
        }
        const string query = @"select 
	MeasurementId,
    ActualElectricityPowerDelivery,
    ActualElectricityPowerDraw,
    ElectricityDeliveredByClientTariff1,
    ElectricityDeliveredByClientTariff2,
    ElectricityDeliveredToClientTariff1,
    ElectricityDeliveredToClientTariff2,
    InstantaneousActivePowerDeliveryL1,
    InstantaneousActivePowerDeliveryL2,
    InstantaneousActivePowerDeliveryL3,
    InstantaneousActivePowerDrawL1,
    InstantaneousActivePowerDrawL2,
    InstantaneousActivePowerDrawL3,
    InstantaneousCurrentL1,
    InstantaneousCurrentL2,
    InstantaneousCurrentL3,
    InstantaneousVoltageL1,
    InstantaneousVoltageL2,
    InstantaneousVoltageL3,
    NetActualElectricityPower,
    TotalInstantaneousCurrent,
    TotalInstantaneousVoltage,
    TimeStamp
from
    p1power;";
    }
}
