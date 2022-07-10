using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using P1LiveView.Models;
using P1Reader.Domain.P1;
using P1Reader.Infra.Sqlite.Interfaces;
using P1Reader.Infra.Sqlite.Services;
using System.Diagnostics;

namespace P1LiveView.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConnectionFactory<SqliteConnection> _connectionFactory;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IConnectionFactory<SqliteConnection> connectionFactory,
            ILogger<HomeController> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var conn = await _connectionFactory.Create(DateTime.Now, SqLiteStorage.CreateTableQuery);

            if (conn == null)
            {
                return View(Enumerable.Empty<Measurement>());
            }

            var now = DateTime.Now;
            var measurements = LoadMeasurements(conn, now.AddHours(-1), now).ToArray();

            return View(measurements);
        }

        public async Task<IActionResult> LastHour()
        {
            var conn = await _connectionFactory.Create(DateTime.Now, SqLiteStorage.CreateTableQuery);

            if (conn == null)
            {
                return View(Enumerable.Empty<Measurement>());
            }

            // TODO select last 5 minutes ofmeasurements and pass these to the View
            var now = DateTime.Now;
            var measurements = LoadMeasurements(conn, now.AddHours(-1), now).ToArray();

            return View(measurements);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private IEnumerable<Measurement> LoadMeasurements(
            SqliteConnection conn,
            DateTime minDate,
            DateTime maxDate)
        {
            using (var cmd = conn.CreateCommand())
            {
                var minDateParam = cmd.Parameters.Add("@MinDate", SqliteType.Text);
                minDateParam.Value = minDate.ToString("yyyy-MM-dd HH:mm:ss");

                var maxDateParam = cmd.Parameters.Add("@MaxDate", SqliteType.Text);
                maxDateParam.Value = maxDate.ToString("yyyy-MM-dd HH:mm:ss");

                cmd.CommandText = @"Select 
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
    timestamp between @MinDate and @MaxDate";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return new Measurement
                        {
                            ActualElectricityPowerDelivery = reader.GetDecimal(0),
                            ActualElectricityPowerDraw = reader.GetDecimal(1),
                            ElectricityDeliveredByClientTariff1 = reader.GetDecimal(2),
                            ElectricityDeliveredByClientTariff2 = reader.GetDecimal(3),
                            ElectricityDeliveredToClientTariff1 = reader.GetDecimal(4),
                            ElectricityDeliveredToClientTariff2 = reader.GetDecimal(5),
                            InstantaneousActivePowerDeliveryL1 = reader.GetDecimal(6),
                            InstantaneousActivePowerDeliveryL2 = reader.GetDecimal(7),
                            InstantaneousActivePowerDeliveryL3 = reader.GetDecimal(8),
                            InstantaneousActivePowerDrawL1 = reader.GetDecimal(9),
                            InstantaneousActivePowerDrawL2 = reader.GetDecimal(10),
                            InstantaneousActivePowerDrawL3 = reader.GetDecimal(11),
                            InstantaneousCurrentL1 = reader.GetInt16(12),
                            InstantaneousCurrentL2 = reader.GetInt16(13),
                            InstantaneousCurrentL3 = reader.GetInt16(14),
                            InstantaneousVoltageL1 = reader.GetInt16(15),
                            InstantaneousVoltageL2 = reader.GetInt16(16),
                            InstantaneousVoltageL3 = reader.GetInt16(17),
                            NetActualElectricityPower = reader.GetDecimal(18),
                            TotalInstantaneousCurrent = reader.GetInt16(19),
                            TotalInstantaneousVoltage = reader.GetDecimal(20),
                            TimeStamp = reader.GetDateTime(21)
                        };
                    }
                }
            }
        }
    }
}