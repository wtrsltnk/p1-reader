using Microsoft.Extensions.Configuration;
using P1Report.Infra.Pdf.Services;
using Serilog;
using System.IO;
using System.Threading.Tasks;
using WkHtmlToPdfDotNet;

namespace P1ReportApp
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

            args = new string[]
            {
                @"\\neofelis\yt-dl\slimmemeter\20220328-p1power.db",
            };

            foreach (var arg in args)
            {
                Log.Logger.Information($"Building report for {arg}...");

                var d = new DayReportBuilderService(config, new SynchronizedConverter(new PdfTools()), Log.Logger);

                await d.BuildReport(new FileInfo(arg));

                Log.Logger.Information($"done!\n");
            }
        }
    }
}

