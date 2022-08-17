using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using P1Reader.Domain;
using P1Reader.Domain.Interfaces;
using P1Reader.Domain.P1;
using P1Reader.Domain.Reporting;
using Polly;
using Polly.Retry;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace P1ReaderApp.Storage
{
    public class MysqlDbStorage :
        IStorage,
        IMeasurementsService
    {
        private readonly ILogger _logger;
        private readonly MySqlConnection _connection;
        private readonly AsyncRetryPolicy _retryPolicy;

        public MysqlDbStorage(
            IConfiguration config,
            ILogger logger)
        {
            _logger= logger;
            _connection = new MySqlConnection(config.GetSection("MysqlConnection").Value);
            _connection.Open();

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryForeverAsync(
                    sleepDurationProvider: (retryAttempt) =>
                    {
                        return TimeSpan.FromSeconds(5);
                    },
                    onRetry: (exception, retryDelay) =>
                    {
                        _logger.Error(exception, "Exception during save to influx, retrying after {retryDelay}", retryDelay);
                    });
        }

        public async Task SaveP1MeasurementAsync(
            Measurement p1Measurements)
        {
            _logger.Verbose("Saving P1 measurement ({timestamp}) to MysqlDB {@measurements}", p1Measurements.TimeStamp, p1Measurements);

            var command = _connection.CreateCommand();

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
                    throw new StorageWriteException($"Error writing to mysqldb: {result} rows affected");
                }

                _logger.Debug("Saving P1 measurement to MysqlDB was succesfull");
            });
        }

        public async Task<IEnumerable<Measurement>> GetMeasurementsBetweenAsync(
            DateTime start,
            DateTime end)
        {
            var command = _connection.CreateCommand();

            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = SelectBetweenQuery;

            command.Parameters.AddWithValue("@StartDate", start);
            command.Parameters.AddWithValue("@EndDate", end);

            var measurements = await _retryPolicy
                .ExecuteAndCaptureAsync<IEnumerable<Measurement>>(async () =>
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var result = new List<Measurement>();

                        while (await reader.ReadAsync())
                        {
                            result.Add(new Measurement
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
                            });
                        }

                        return result;
                    }
                });

            return measurements.Result;
        }

        public async Task<ElectricityNumbers> GetElectricityNumbersBetweenAsync(
            DateTime start,
            DateTime end)
        {
            var command = _connection.CreateCommand();

            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = SelectElectricityNumbersBetween;

            command.Parameters.AddWithValue("@StartDate", start);
            command.Parameters.AddWithValue("@EndDate", end);

            var measurements = await _retryPolicy
                .ExecuteAndCaptureAsync<ElectricityNumbers>(async () =>
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var result = new List<ElectricityNumbers>();

                        if (await reader.ReadAsync())
                        {
                            if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2) || reader.IsDBNull(3) || reader.IsDBNull(4) || reader.IsDBNull(5) || reader.IsDBNull(6))
                            {
                                return null;
                            }

                            return new ElectricityNumbers
                            {
                                Start = start,
                                End = end,
                                ElectricityDeliveredByClient = (reader.GetDecimal(3) - reader.GetDecimal(2)) / 100,
                                ElectricityDeliveredToClient = (reader.GetDecimal(1) - reader.GetDecimal(0)) / 100,
                                ActualElectricityPowerDelivery = reader.GetDecimal(4) / 100,
                                ActualElectricityPowerDraw = reader.GetDecimal(5) / 100,
                                NetActualElectricityPower = reader.GetDecimal(6) / 100,
                            };
                        }

                        return null;
                    }
                });

            return measurements.Result;
        }

        const string InsertQuery = @"INSERT INTO p1power (ActualElectricityPowerDelivery, ActualElectricityPowerDraw, ElectricityDeliveredByClientTariff1, ElectricityDeliveredByClientTariff2, ElectricityDeliveredToClientTariff1, ElectricityDeliveredToClientTariff2, InstantaneousActivePowerDeliveryL1, InstantaneousActivePowerDeliveryL2, InstantaneousActivePowerDeliveryL3, InstantaneousActivePowerDrawL1, InstantaneousActivePowerDrawL2, InstantaneousActivePowerDrawL3, InstantaneousCurrentL1, InstantaneousCurrentL2, InstantaneousCurrentL3, InstantaneousVoltageL1, InstantaneousVoltageL2, InstantaneousVoltageL3, NetActualElectricityPower, TotalInstantaneousCurrent, TotalInstantaneousVoltage, TimeStamp) VALUES (@ActualElectricityPowerDelivery, @ActualElectricityPowerDraw, @ElectricityDeliveredByClientTariff1, @ElectricityDeliveredByClientTariff2, @ElectricityDeliveredToClientTariff1, @ElectricityDeliveredToClientTariff2, @InstantaneousActivePowerDeliveryL1, @InstantaneousActivePowerDeliveryL2, @InstantaneousActivePowerDeliveryL3, @InstantaneousActivePowerDrawL1, @InstantaneousActivePowerDrawL2, @InstantaneousActivePowerDrawL3, @InstantaneousCurrentL1, @InstantaneousCurrentL2, @InstantaneousCurrentL3, @InstantaneousVoltageL1, @InstantaneousVoltageL2, @InstantaneousVoltageL3, @NetActualElectricityPower, @TotalInstantaneousCurrent, @TotalInstantaneousVoltage, @TimeStamp)";
        const string SelectBetweenQuery = @"select
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
    p1power
where
    TimeStamp between @StartDate and @EndDate;";

        const string SelectElectricityNumbersBetween = @"
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
    }
}