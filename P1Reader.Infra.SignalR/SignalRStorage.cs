using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using P1Reader.Domain.Interfaces;
using P1Reader.Domain.P1;
using P1Reader.Domain.Reporting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace P1Reader.Infra.SignalR
{
    public class SignalRStorage :
        IStorage
    {
        private HubConnection _connection;
        private readonly IConfiguration _config;

        public SignalRStorage(
            IConfiguration config)
        {
            _config = config;
        }

        private async Task Connect()
        {
            if (_connection != null)
            {
                return;
            }

            var url = _config.GetSection("SignalRUrl").Value;

            _connection = new HubConnectionBuilder()
                .WithUrl(url)
                .Build();

            _connection.Closed += async (eror) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await _connection.StartAsync();
            };
            await _connection.StartAsync();
        }

        public Task<ElectricityNumbers> GetElectricityNumbersBetweenAsync(
            DateTime start,
            DateTime end)
        {
            return Task.FromResult(new ElectricityNumbers());
        }

        public Task<IEnumerable<Measurement>> GetMeasurementsBetweenAsync(
            DateTime start,
            DateTime end)
        {
            return Task.FromResult(Enumerable.Empty<Measurement>());
        }

        public async Task SaveP1MeasurementAsync(
            Measurement p1Measurements)
        {
            await Connect();

            await _connection.SendAsync("ReceiveMeasurement", p1Measurements);
        }
    }
}
