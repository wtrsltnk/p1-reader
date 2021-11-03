using InfluxDB.LineProtocol.Client;
using InfluxDB.LineProtocol.Payload;
using Microsoft.Extensions.Configuration;
using P1ReaderApp.Exceptions;
using P1ReaderApp.Model;
using Polly;
using Polly.Retry;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace P1ReaderApp.Storage
{
    public class InfluxDbStorage : IStorage
    {
        private readonly LineProtocolClient _client;
        private readonly AsyncRetryPolicy _retryPolicy;

        public InfluxDbStorage(
            IConfiguration config)
        {
            var section = config.GetSection("InfluxdbSettings");

            if (section == null)
            {
                throw new ArgumentException("Config does not contain mandatory section with name 'InfluxdbSettings'", nameof(config));
            }

            string serverAddress = section.GetValue<string>("ServerAddress");
            string database = section.GetValue<string>("Database");
            string username = section.GetValue<string>("Username");
            string password = section.GetValue<string>("Password");

            _client = new LineProtocolClient(new Uri(serverAddress), database, username, password);

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryForeverAsync(
                    sleepDurationProvider: (retryAttempt) =>
                    {
                        return TimeSpan.FromSeconds(5);
                    },
                    onRetry: (exception, retryDelay) =>
                    {
                        Log.Error(exception, "Exception during save to influx, retrying after {retryDelay}", retryDelay);
                    });
        }

        public async Task SaveP1Measurement(P1Measurements p1Measurements)
        {
            Log.Verbose("Saving P1 measurement ({timestamp}) to InfluxDB {@measurements}", p1Measurements.TimeStamp, p1Measurements);

            var payload = new LineProtocolPayload();

            payload.Add(new LineProtocolPoint(
                measurement: "p1power",
                fields: new Dictionary<string, object>
                {
                    { nameof(P1Measurements.ActualElectricityPowerDelivery), p1Measurements.ActualElectricityPowerDelivery },
                    { nameof(P1Measurements.ActualElectricityPowerDraw), p1Measurements.ActualElectricityPowerDraw },
                    { nameof(P1Measurements.ElectricityDeliveredByClientTariff1), p1Measurements.ElectricityDeliveredByClientTariff1 },
                    { nameof(P1Measurements.ElectricityDeliveredByClientTariff2), p1Measurements.ElectricityDeliveredByClientTariff2 },
                    { nameof(P1Measurements.ElectricityDeliveredToClientTariff1), p1Measurements.ElectricityDeliveredToClientTariff1 },
                    { nameof(P1Measurements.ElectricityDeliveredToClientTariff2), p1Measurements.ElectricityDeliveredToClientTariff2 },
                    { nameof(P1Measurements.InstantaneousActivePowerDeliveryL1), p1Measurements.InstantaneousActivePowerDeliveryL1 },
                    { nameof(P1Measurements.InstantaneousActivePowerDeliveryL2), p1Measurements.InstantaneousActivePowerDeliveryL2 },
                    { nameof(P1Measurements.InstantaneousActivePowerDeliveryL3), p1Measurements.InstantaneousActivePowerDeliveryL3 },
                    { nameof(P1Measurements.InstantaneousActivePowerDrawL1), p1Measurements.InstantaneousActivePowerDrawL1 },
                    { nameof(P1Measurements.InstantaneousActivePowerDrawL2), p1Measurements.InstantaneousActivePowerDrawL2 },
                    { nameof(P1Measurements.InstantaneousActivePowerDrawL3), p1Measurements.InstantaneousActivePowerDrawL3 },
                    { nameof(P1Measurements.InstantaneousCurrentL1), p1Measurements.InstantaneousCurrentL1 },
                    { nameof(P1Measurements.InstantaneousCurrentL2), p1Measurements.InstantaneousCurrentL2 },
                    { nameof(P1Measurements.InstantaneousCurrentL3), p1Measurements.InstantaneousCurrentL3 },
                    { nameof(P1Measurements.InstantaneousVoltageL1), p1Measurements.InstantaneousVoltageL1 },
                    { nameof(P1Measurements.InstantaneousVoltageL2), p1Measurements.InstantaneousVoltageL2 },
                    { nameof(P1Measurements.InstantaneousVoltageL3), p1Measurements.InstantaneousVoltageL3 },
                    { nameof(P1Measurements.NetActualElectricityPower), p1Measurements.NetActualElectricityPower },
                    { nameof(P1Measurements.TotalInstantaneousCurrent), p1Measurements.TotalInstantaneousCurrent },
                    { nameof(P1Measurements.TotalInstantaneousVoltage), p1Measurements.TotalInstantaneousVoltage }
                },
                tags: new Dictionary<string, string>(),
                utcTimestamp: p1Measurements.TimeStamp));

            await _retryPolicy.ExecuteAsync(async () =>
            {
                var influxResult = await _client.WriteAsync(payload);

                if (!influxResult.Success)
                {
                    throw new StorageWriteException($"Error writing to influxdb: {influxResult.ErrorMessage}");
                }

                Log.Debug("Saving P1 measurement to InfluxDB was succesfull");
            });
        }
    }
}