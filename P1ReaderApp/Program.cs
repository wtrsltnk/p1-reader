using Microsoft.Data.Sqlite;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using P1ReaderApp.Model;
using P1ReaderApp.Services;
using P1ReaderApp.Storage;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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

            services.AddScoped<IMessageBuffer<P1MessageCollection>, MessageBuffer<P1MessageCollection>>();
            services.AddScoped<IMessageBuffer<P1Measurements>, MessageBuffer<P1Measurements>>();
            services.AddScoped<MessageParser>();
            services.AddScoped<SerialPortReader>();

            commandLineApplication.Command("sqlite", target =>
            {
                target.Description = "Write to sqlite";
                target.HelpOption("-? | -h | --help");

                target.OnExecute(() =>
                {
                    services.AddScoped<IConnectionFactory<SqliteConnection>, SqliteConnectionFactory>();
                    services.AddScoped<IStorage, SqLiteStorage>();

                    return 0;
                });
            });

            commandLineApplication.Command("mysql", target =>
            {
                target.Description = "Write to mysql";
                target.HelpOption("-? | -h | --help");

                target.OnExecute(() =>
                {
                    services.AddScoped<IStorage, MysqlDbStorage>();

                    return 0;
                });
            });

            commandLineApplication.Command("influxdb", target =>
            {
                target.Description = "Write to influxdb";
                target.HelpOption("-? | -h | --help");

                target.OnExecute(() =>
                {
                    services.AddScoped<IStorage, InfluxDbStorage>();

                    return 0;
                });
            });
        }

        public async Task Run(
            IServiceProvider serviceProvider)
        {
            try
            {
                var config = serviceProvider.GetService<IConfiguration>();

                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(config)
                    .CreateLogger();

                var serialMessageBuffer = serviceProvider.GetService<IMessageBuffer<P1MessageCollection>>();
                var measurementsBuffer = serviceProvider.GetService<IMessageBuffer<P1Measurements>>();
                var messageParser = serviceProvider.GetService<MessageParser>();
                var storage = serviceProvider.GetService<IStorage>();

                serialMessageBuffer.RegisterMessageHandler(messageParser.ParseSerialMessages);
                measurementsBuffer.RegisterMessageHandler(storage.SaveP1Measurement);

                var serialPortReader = serviceProvider.GetService<SerialPortReader>();

                serialPortReader.StartReading();

                Console.ReadLine();

                await WaitForCancellation();
            }
            catch (Exception exc)
            {
                Log.Fatal(exc, "Fatal exception during application execute");
                throw;
            }
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

            commandLineApplication.HelpOption("-? | -h | --help");

            program.ConfigureServices(services, commandLineApplication);

            commandLineApplication.OnExecute(() =>
            {
                commandLineApplication.ShowHelp();

                return 1;
            });

            if (commandLineApplication.Execute(args) != 0)
            {
                return;
            }

            using var serviceProvider = services.BuildServiceProvider();

            await program.Run(serviceProvider);
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