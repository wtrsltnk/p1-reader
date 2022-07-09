using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using P1Reader.Domain.Interface;
using P1Reader.Domain.Interfaces;
using P1Reader.Domain.P1;
using P1Reader.Infra.SignalR;
using P1ReaderApp.Interfaces;
using P1ReaderApp.Model;
using P1ReaderApp.Services;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Contracts;

namespace P1ReaderApp
{
    internal class Program
    {
        public Program(
            IConfiguration config)
        {
            Configuration = config;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(
            IServiceCollection services,
            CommandLineApplication commandLineApplication)
        {
            services.AddScoped(s => Configuration);

            services.AddScoped(sp => Log.Logger);
            services.AddScoped<IMessageBuffer<P1MessageCollection>, MessageBuffer<P1MessageCollection>>();
            services.AddScoped<IMessageBuffer<Measurement>, MessageBuffer<Measurement>>();
            services.AddScoped<MessageParser>();
            services.AddScoped<IMapper<P1Measurements, Measurement>, P1MeasurementsMapper>();
            services.AddScoped<SerialPortReader>();

            services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

            commandLineApplication
                .Command("signalr", command =>
                {
                    var fakeRunOption = commandLineApplication.Option("--fake-run", "Use a fake serial port for data", CommandOptionType.NoValue);

                    command.Description = "Write to signalr";
                    command.HelpOption("-? | -h | --help");

                    command.OnExecute(() =>
                    {
                        if (!fakeRunOption.HasValue())
                        {
                            services.AddScoped<ISerialPort, SerialPortWrapper>();
                        }
                        else
                        {
                            services.AddScoped<ISerialPort, FakeSerialPort>();
                        }

                        services.AddScoped<IStorage, SignalRStorage>();

                        return 0;
                    });
                });
        }


        private static async Task Main(
            string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var program = new Program(config);

            var services = new ServiceCollection();
            var commandLineApplication = new CommandLineApplication(throwOnUnexpectedArg: false);

            commandLineApplication
                .HelpOption("-? | -h | --help");

            program
                .ConfigureServices(services, commandLineApplication);

            commandLineApplication
                .OnExecute(() =>
                {
                    commandLineApplication.ShowHelp();

                    return 1;
                });

            if (commandLineApplication.Execute(args) != 0)
            {
                return;
            }

            using var serviceProvider = services
                .BuildServiceProvider();

            await Run(serviceProvider);
        }

        public static async Task Run(
            IServiceProvider serviceProvider)
        {
            try
            {
                var config = serviceProvider
                    .GetService<IConfiguration>();

                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(config)
                    .CreateLogger();

                var serialMessageBuffer = serviceProvider
                    .GetService<IMessageBuffer<P1MessageCollection>>();

                var measurementsBuffer = serviceProvider
                    .GetService<IMessageBuffer<Measurement>>();

                var messageParser = serviceProvider
                    .GetService<MessageParser>();

                var storage = serviceProvider
                    .GetService<IStorage>();

                serialMessageBuffer
                    .RegisterMessageHandler(messageParser.ParseSerialMessages);

                measurementsBuffer
                    .RegisterMessageHandler(storage.SaveP1MeasurementAsync);

                var serialPortReader = serviceProvider
                    .GetService<SerialPortReader>();

                serialPortReader
                    .StartReading();

                Console.ReadLine();

                await WaitForCancellation();
            }
            catch (Exception exc)
            {
                Log.Fatal(exc, "Fatal exception during application execute");
                throw;
            }
        }

        private static async Task<int> WaitForCancellation()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            AppDomain.CurrentDomain.ProcessExit += (s, e) => cancellationTokenSource.Cancel();
            Console.CancelKeyPress += (s, e) => cancellationTokenSource.Cancel();
            return await Task.Delay(-1, cancellationTokenSource.Token).ContinueWith(t => { return 1; });
        }
    }
}