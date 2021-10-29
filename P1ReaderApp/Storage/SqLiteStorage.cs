using Microsoft.Data.Sqlite;
using P1ReaderApp.Exceptions;
using P1ReaderApp.Model;
using Polly;
using Polly.Retry;
using Serilog;
using System;
using System.Threading.Tasks;

namespace P1ReaderApp.Storage
{
    public class SqLiteStorage : IStorage
    {
        private readonly Func<DateTime, Task<SqliteConnection>> _connectionFactory;
        private readonly AsyncRetryPolicy _retryPolicy;

        public SqLiteStorage(
            Func<DateTime, Task<SqliteConnection>> connectionFactory)
        {
            _connectionFactory = connectionFactory;

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryForeverAsync(
                    sleepDurationProvider: (retryAttempt) =>
                    {
                        return TimeSpan.FromSeconds(5);
                    },
                    onRetry: (exception, retryDelay) =>
                    {
                        Log.Error(exception, "Exception during save to sqlite, retrying after {retryDelay}", retryDelay);
                    });
        }

        public async Task SaveP1Measurement(
            P1Measurements p1Measurements)
        {
            var conn = await _connectionFactory(p1Measurements.TimeStamp);

            if (conn == null)
            {
                return;
            }

            var command = conn.CreateCommand();

            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = InsertQuery;

            command.Parameters.AddWithValue("@" + nameof(P1Measurements.ActualElectricityPowerDelivery), p1Measurements.ActualElectricityPowerDelivery);
            command.Parameters.AddWithValue("@" + nameof(P1Measurements.ActualElectricityPowerDraw), p1Measurements.ActualElectricityPowerDraw);
            command.Parameters.AddWithValue("@" + nameof(P1Measurements.ElectricityDeliveredByClientTariff1), p1Measurements.ElectricityDeliveredByClientTariff1);
            command.Parameters.AddWithValue("@" + nameof(P1Measurements.ElectricityDeliveredByClientTariff2), p1Measurements.ElectricityDeliveredByClientTariff2);
            command.Parameters.AddWithValue("@" + nameof(P1Measurements.ElectricityDeliveredToClientTariff1), p1Measurements.ElectricityDeliveredToClientTariff1);
            command.Parameters.AddWithValue("@" + nameof(P1Measurements.ElectricityDeliveredToClientTariff2), p1Measurements.ElectricityDeliveredToClientTariff2);
            command.Parameters.AddWithValue("@" + nameof(P1Measurements.InstantaneousActivePowerDeliveryL1), p1Measurements.InstantaneousActivePowerDeliveryL1);
            command.Parameters.AddWithValue("@" + nameof(P1Measurements.InstantaneousActivePowerDeliveryL2), p1Measurements.InstantaneousActivePowerDeliveryL2);
            command.Parameters.AddWithValue("@" + nameof(P1Measurements.InstantaneousActivePowerDeliveryL3), p1Measurements.InstantaneousActivePowerDeliveryL3);
            command.Parameters.AddWithValue("@" + nameof(P1Measurements.InstantaneousActivePowerDrawL1), p1Measurements.InstantaneousActivePowerDrawL1);
            command.Parameters.AddWithValue("@" + nameof(P1Measurements.InstantaneousActivePowerDrawL2), p1Measurements.InstantaneousActivePowerDrawL2);
            command.Parameters.AddWithValue("@" + nameof(P1Measurements.InstantaneousActivePowerDrawL3), p1Measurements.InstantaneousActivePowerDrawL3);
            command.Parameters.AddWithValue("@" + nameof(P1Measurements.InstantaneousCurrentL1), p1Measurements.InstantaneousCurrentL1);
            command.Parameters.AddWithValue("@" + nameof(P1Measurements.InstantaneousCurrentL2), p1Measurements.InstantaneousCurrentL2);
            command.Parameters.AddWithValue("@" + nameof(P1Measurements.InstantaneousCurrentL3), p1Measurements.InstantaneousCurrentL3);
            command.Parameters.AddWithValue("@" + nameof(P1Measurements.InstantaneousVoltageL1), p1Measurements.InstantaneousVoltageL1);
            command.Parameters.AddWithValue("@" + nameof(P1Measurements.InstantaneousVoltageL2), p1Measurements.InstantaneousVoltageL2);
            command.Parameters.AddWithValue("@" + nameof(P1Measurements.InstantaneousVoltageL3), p1Measurements.InstantaneousVoltageL3);
            command.Parameters.AddWithValue("@" + nameof(P1Measurements.NetActualElectricityPower), p1Measurements.NetActualElectricityPower);
            command.Parameters.AddWithValue("@" + nameof(P1Measurements.TotalInstantaneousCurrent), p1Measurements.TotalInstantaneousCurrent);
            command.Parameters.AddWithValue("@" + nameof(P1Measurements.TotalInstantaneousVoltage), p1Measurements.TotalInstantaneousVoltage);
            command.Parameters.AddWithValue("@" + nameof(P1Measurements.TimeStamp), p1Measurements.TimeStamp);

            await _retryPolicy.ExecuteAsync(async () =>
            {
                var result = await command.ExecuteNonQueryAsync();

                if (result != 1)
                {
                    throw new StorageWriteException($"Error writing to sqlite: {result} rows affected");
                }

                Log.Verbose("Saving P1 measurement to Sqlite was succesfull");
            });
        }

        public const string InsertQuery = @"INSERT INTO p1power (ActualElectricityPowerDelivery, ActualElectricityPowerDraw, ElectricityDeliveredByClientTariff1, ElectricityDeliveredByClientTariff2, ElectricityDeliveredToClientTariff1, ElectricityDeliveredToClientTariff2, InstantaneousActivePowerDeliveryL1, InstantaneousActivePowerDeliveryL2, InstantaneousActivePowerDeliveryL3, InstantaneousActivePowerDrawL1, InstantaneousActivePowerDrawL2, InstantaneousActivePowerDrawL3, InstantaneousCurrentL1, InstantaneousCurrentL2, InstantaneousCurrentL3, InstantaneousVoltageL1, InstantaneousVoltageL2, InstantaneousVoltageL3, NetActualElectricityPower, TotalInstantaneousCurrent, TotalInstantaneousVoltage, TimeStamp) VALUES (@ActualElectricityPowerDelivery, @ActualElectricityPowerDraw, @ElectricityDeliveredByClientTariff1, @ElectricityDeliveredByClientTariff2, @ElectricityDeliveredToClientTariff1, @ElectricityDeliveredToClientTariff2, @InstantaneousActivePowerDeliveryL1, @InstantaneousActivePowerDeliveryL2, @InstantaneousActivePowerDeliveryL3, @InstantaneousActivePowerDrawL1, @InstantaneousActivePowerDrawL2, @InstantaneousActivePowerDrawL3, @InstantaneousCurrentL1, @InstantaneousCurrentL2, @InstantaneousCurrentL3, @InstantaneousVoltageL1, @InstantaneousVoltageL2, @InstantaneousVoltageL3, @NetActualElectricityPower, @TotalInstantaneousCurrent, @TotalInstantaneousVoltage, @TimeStamp)";
        public const string CreateTableQuery = @"CREATE TABLE IF NOT EXISTS p1power (
    MeasurementId INT AUTO_INCREMENT PRIMARY KEY,
    ActualElectricityPowerDelivery  DECIMAL(10,3),
    ActualElectricityPowerDraw  DECIMAL(10,3),
    ElectricityDeliveredByClientTariff1  DECIMAL(10,3),
    ElectricityDeliveredByClientTariff2  DECIMAL(10,3),
    ElectricityDeliveredToClientTariff1  DECIMAL(10,3),
    ElectricityDeliveredToClientTariff2  DECIMAL(10,3),
    InstantaneousActivePowerDeliveryL1  DECIMAL(10,3),
    InstantaneousActivePowerDeliveryL2  DECIMAL(10,3),
    InstantaneousActivePowerDeliveryL3  DECIMAL(10,3),
    InstantaneousActivePowerDrawL1  DECIMAL(10,3),
    InstantaneousActivePowerDrawL2  DECIMAL(10,3),
    InstantaneousActivePowerDrawL3  DECIMAL(10,3),
    InstantaneousCurrentL1 INT,
    InstantaneousCurrentL2 INT,
    InstantaneousCurrentL3 INT,
    InstantaneousVoltageL1 DECIMAL(10,1),
    InstantaneousVoltageL2 DECIMAL(10,1),
    InstantaneousVoltageL3 DECIMAL(10,1),
    NetActualElectricityPower DECIMAL(10,3),
    TotalInstantaneousCurrent INT,
    TotalInstantaneousVoltage DECIMAL(10,1),
    TimeStamp TIMESTAMP);";
    }
}
