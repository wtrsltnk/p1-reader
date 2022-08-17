using Microsoft.AspNetCore.SignalR;
using P1Reader.Domain.Interfaces;
using P1Reader.Domain.P1;
using Serilog;
using System.Threading.Tasks;

namespace P1LiveView.Hubs
{
    public class P1Hub :
        Hub
    {
        private readonly IStorage _storage;
        private readonly ILogger _logger;

        public P1Hub(
            IStorage storage,
            ILogger logger)
        {
            _storage = storage;
            _logger = logger;
        }

        public async Task ReceiveMeasurement(
            Measurement measurement)
        {
            _logger.Verbose("ReceiveMeasurement()");

            await Clients.All.SendAsync("ReceiveMeasurement", measurement);

            await _storage.SaveP1MeasurementAsync(measurement);
        }
    }
}
