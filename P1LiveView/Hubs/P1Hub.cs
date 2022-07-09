using Microsoft.AspNetCore.SignalR;
using P1Reader.Domain.Interfaces;
using P1Reader.Domain.P1;

namespace P1LiveView.Hubs
{
    public class P1Hub :
        Hub
    {
        private readonly IStorage _storage;

        public P1Hub(
            IStorage storage)
        {
            _storage = storage;
        }

        public async Task ReceiveMeasurement(
            Measurement measurement)
        {
            await Clients.All.SendAsync("ReceiveMeasurement", measurement);

            await _storage.SaveP1MeasurementAsync(measurement);
        }
    }
}
