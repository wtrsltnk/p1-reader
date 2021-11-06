using Microsoft.Extensions.Configuration;
using P1Report.Infra.Pdf.Services;
using System;
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

            foreach (var arg in args)
            {
                Console.Write($"Building report for {arg}...");
                var d = new DayReportBuilderService(config, new SynchronizedConverter(new PdfTools()));

                await d.BuildReport(new FileInfo(arg));
                
                Console.Write($"done!\n");
            }
        }
    }
}

