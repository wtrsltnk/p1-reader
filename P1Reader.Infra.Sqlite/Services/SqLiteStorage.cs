using Microsoft.Data.Sqlite;
using P1Reader.Domain;
using P1Reader.Domain.Interfaces;
using P1Reader.Domain.P1;
using P1Reader.Infra.Sqlite.Interfaces;
using Polly;
using Polly.Retry;
using Serilog;
using System;
using System.Threading.Tasks;

namespace P1Reader.Infra.Sqlite.Services
{
    public class SqLiteStorage :
        IStorage
    {
        private readonly IConnectionFactory<SqliteConnection> _connectionFactory;
        private readonly ILogger _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public SqLiteStorage(
            IConnectionFactory<SqliteConnection> connectionFactory,
            ILogger logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryForeverAsync(
                    sleepDurationProvider: (retryAttempt) =>
                    {
                        return TimeSpan.FromSeconds(5);
                    },
                    onRetry: (exception, retryDelay) =>
                    {
                        _logger.Error(exception, "Exception during save to sqlite, retrying after {retryDelay}", retryDelay);
                    });
        }

        public async Task SaveP1MeasurementAsync(
            Measurement p1Measurements)
        {
            var conn = await _connectionFactory.Create(p1Measurements.TimeStamp, CreateTableQuery);

            if (conn == null)
            {
                return;
            }

            var command = conn.CreateCommand();

            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = InsertQuery;

            command.Parameters.AddWithValue("@" + nameof(Measurement.ActualElectricityPowerDelivery), p1Measurements.ActualElectricityPowerDelivery);
            command.Parameters.AddWithValue("@" + nameof(Measurement.ActualElectricityPowerDraw), p1Measurements.ActualElectricityPowerDraw);
            command.Parameters.AddWithValue("@" + nameof(Measurement.ElectricityDeliveredByClientTariff1), p1Measurements.ElectricityDeliveredByClientTariff1);
            command.Parameters.AddWithValue("@" + nameof(Measurement.ElectricityDeliveredByClientTariff2), p1Measurements.ElectricityDeliveredByClientTariff2);
            command.Parameters.AddWithValue("@" + nameof(Measurement.ElectricityDeliveredToClientTariff1), p1Measurements.ElectricityDeliveredToClientTariff1);
            command.Parameters.AddWithValue("@" + nameof(Measurement.ElectricityDeliveredToClientTariff2), p1Measurements.ElectricityDeliveredToClientTariff2);
            command.Parameters.AddWithValue("@" + nameof(Measurement.InstantaneousActivePowerDeliveryL1), p1Measurements.InstantaneousActivePowerDeliveryL1);
            command.Parameters.AddWithValue("@" + nameof(Measurement.InstantaneousActivePowerDeliveryL2), p1Measurements.InstantaneousActivePowerDeliveryL2);
            command.Parameters.AddWithValue("@" + nameof(Measurement.InstantaneousActivePowerDeliveryL3), p1Measurements.InstantaneousActivePowerDeliveryL3);
            command.Parameters.AddWithValue("@" + nameof(Measurement.InstantaneousActivePowerDrawL1), p1Measurements.InstantaneousActivePowerDrawL1);
            command.Parameters.AddWithValue("@" + nameof(Measurement.InstantaneousActivePowerDrawL2), p1Measurements.InstantaneousActivePowerDrawL2);
            command.Parameters.AddWithValue("@" + nameof(Measurement.InstantaneousActivePowerDrawL3), p1Measurements.InstantaneousActivePowerDrawL3);
            command.Parameters.AddWithValue("@" + nameof(Measurement.InstantaneousCurrentL1), p1Measurements.InstantaneousCurrentL1);
            command.Parameters.AddWithValue("@" + nameof(Measurement.InstantaneousCurrentL2), p1Measurements.InstantaneousCurrentL2);
            command.Parameters.AddWithValue("@" + nameof(Measurement.InstantaneousCurrentL3), p1Measurements.InstantaneousCurrentL3);
            command.Parameters.AddWithValue("@" + nameof(Measurement.InstantaneousVoltageL1), p1Measurements.InstantaneousVoltageL1);
            command.Parameters.AddWithValue("@" + nameof(Measurement.InstantaneousVoltageL2), p1Measurements.InstantaneousVoltageL2);
            command.Parameters.AddWithValue("@" + nameof(Measurement.InstantaneousVoltageL3), p1Measurements.InstantaneousVoltageL3);
            command.Parameters.AddWithValue("@" + nameof(Measurement.NetActualElectricityPower), p1Measurements.NetActualElectricityPower);
            command.Parameters.AddWithValue("@" + nameof(Measurement.TotalInstantaneousCurrent), p1Measurements.TotalInstantaneousCurrent);
            command.Parameters.AddWithValue("@" + nameof(Measurement.TotalInstantaneousVoltage), p1Measurements.TotalInstantaneousVoltage);
            command.Parameters.AddWithValue("@" + nameof(Measurement.TimeStamp), p1Measurements.TimeStamp);

            await _retryPolicy.ExecuteAsync(async () =>
            {
                var result = await command.ExecuteNonQueryAsync();

                if (result != 1)
                {
                    _logger.Error("Error writing to sqlite: {result} rows affected", result);

                    throw new StorageWriteException($"Error writing to sqlite: {result} rows affected");
                }

                _logger.Verbose("Saving P1 measurement to Sqlite was succesfull");
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
