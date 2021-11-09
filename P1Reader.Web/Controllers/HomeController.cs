using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using P1Reader.Domain.Interfaces;
using P1Reader.Web.Models;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace P1Reader.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IStorage _storage;

        public HomeController(
            IStorage storage,
            ILogger<HomeController> logger)
        {
            _storage = storage;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Data(
            int? year,
            int? month)
        {
            if (!year.HasValue) year = DateTime.Now.Year;
            if (!month.HasValue) month = DateTime.Now.Month;

            var start = new DateTime(year.Value, month.Value, 1);

            var measurement = await _storage
                .GetElectricityNumbersBetweenAsync(start, start.AddMonths(1));

            return Json(measurement);
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
    }
}
