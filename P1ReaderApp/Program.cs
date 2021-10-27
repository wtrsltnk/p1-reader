using Microsoft.Data.Sqlite;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using P1ReaderApp.Exceptions;
using P1ReaderApp.Extensions;
using P1ReaderApp.Model;
using P1ReaderApp.Services;
using P1ReaderApp.Storage;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace P1ReaderApp
{
    internal static class Program
    {
        private static IMessageBuffer<P1Measurements> _measurementsBuffer;
        private static IMessageBuffer<P1MessageCollection> _serialMessageBuffer;

        private static void CreateDaemonLogger(
            int minLogLevel)
        {
            Log.Logger = new LoggerConfiguration()
                                .MinimumLevel.Is((LogEventLevel)minLogLevel)
                                .WriteTo.Console()
                                .WriteTo.File("log.txt", restrictedToMinimumLevel: LogEventLevel.Warning)
                                .CreateLogger();
        }

        private static void CreateStatusLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Error()
                .WriteTo.Console()
                .WriteTo.File("log.txt", restrictedToMinimumLevel: LogEventLevel.Warning)
                .CreateLogger();
        }

        private static Action<CommandLineApplication> DebugApplication(
            P1Config config)
        {
            return (target) =>
            {
                target.Description = "Show debug information";
                target.HelpOption("-? | -h | --help");

                target.OnExecute(async () =>
                {
                    try
                    {
                        CreateStatusLogger();

                        IStatusPrintService statusPrintService = new ConsoleStatusPrintService();
                        _measurementsBuffer.RegisterMessageHandler(statusPrintService.UpdateP1Measurements);
                        _serialMessageBuffer.RegisterMessageHandler(statusPrintService.UpdateRawData);

                        var serialPortReader = new SerialPortReader(config.Port, config.BaudRate, config.StopBits, config.Parity, config.DataBits, _serialMessageBuffer);
                        serialPortReader.StartReading();

                        while (true)
                        {
                            Console.ReadLine();
                        }
                    }
                    catch (ConfigurationValueRequiredException exception)
                    {
                        Console.WriteLine(exception.Message);
                    }
                    catch (Exception exception)
                    {
                        Log.Fatal(exception, "Unexpected exception during startup");
                    }

                    return await WaitForCancellation();
                });
            };
        }

        private static Action<CommandLineApplication> MysqlDbApplication(
            P1Config config,
            string mysqldbConnectionstring)
        {
            return (target) =>
            {
                target.Description = "Write to mysqldb";
                target.HelpOption("-? | -h | --help");

                var loggingOption = target.CreateLoggingOption();

                target.OnExecute(async () =>
                {
                    try
                    {
                        var loglevel = loggingOption.GetOptionalIntValue(3);
                        CreateDaemonLogger(loglevel);

                        IStorage storage = new MysqlDbStorage(mysqldbConnectionstring);
                        _measurementsBuffer.RegisterMessageHandler(storage.SaveP1Measurement);

                        var serialPortReader = new SerialPortReader(config.Port, config.BaudRate, config.StopBits, config.Parity, config.DataBits, _serialMessageBuffer);
                        serialPortReader.StartReading();

                        Console.ReadLine();
                    }
                    catch (ConfigurationValueRequiredException exception)
                    {
                        Console.WriteLine(exception.Message);
                    }
                    catch (Exception exception)
                    {
                        Log.Fatal(exception, "Unexpected exception during startup");
                    }

                    return await WaitForCancellation();
                });
            };
        }

        private static Action<CommandLineApplication> SqliteApplication(
            P1Config config,
            IConfigurationSection configurationSection)
        {
            return (target) =>
            {
                target.Description = "Write to sqlite";
                target.HelpOption("-? | -h | --help");

                var loggingOption = target.CreateLoggingOption();

                target.OnExecute(async () =>
                {
                    try
                    {
                        var loglevel = loggingOption.GetOptionalIntValue(3);
                        CreateDaemonLogger(loglevel);

                        IStorage storage = new SqLiteStorage(async timestamp => await CreateSqliteConnection(timestamp, configurationSection));
                        _measurementsBuffer.RegisterMessageHandler(storage.SaveP1Measurement);

                        var serialPortReader = new SerialPortReader(config.Port, config.BaudRate, config.StopBits, config.Parity, config.DataBits, _serialMessageBuffer);
                        serialPortReader.StartReading();

                        Console.ReadLine();
                    }
                    catch (ConfigurationValueRequiredException exception)
                    {
                        Console.WriteLine(exception.Message);
                    }
                    catch (Exception exception)
                    {
                        Log.Fatal(exception, "Unexpected exception during startup");
                    }

                    return await WaitForCancellation();
                });
            };
        }

        private static DateTime? _lastTimeStamp = null;
        private static SqliteConnection _currentConnection = null;

        private static async Task<SqliteConnection> CreateSqliteConnection(
                DateTime timestamp,
                IConfigurationSection configurationSection)
        {
            if (!_lastTimeStamp.HasValue)
            {
                _lastTimeStamp = timestamp;
            }

            var diffDays = (timestamp.Date - _lastTimeStamp.Value.Date).TotalDays;

            if (diffDays < 0)
            {
                // This means that we already moved on to the next day, but there is still a measurement coming in from the previous day
                return null;
            }

            var localDbPath = configurationSection["LocalDbPath"];
            var archiveDbPath = configurationSection["ArchiveDbPath"];
            var dbFileNameForTimestamp = Path.Combine(localDbPath, $"{timestamp:yyyyMMdd}-p1power.db");

            if (diffDays > 0 && _currentConnection != null)
            {
                await RotateCurrentConnectionToArchive(archiveDbPath);
                _currentConnection = null;
            }

            if (_currentConnection == null)
            {
                _currentConnection = await InitConnection(dbFileNameForTimestamp);
            }

            return _currentConnection;
        }

        private static async Task<SqliteConnection> InitConnection(
            string dbFilePath)
        {
            var connection = new SqliteConnection($"Data Source={dbFilePath}");

            await connection.OpenAsync();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = SqLiteStorage.CreateTableQuery;

                await cmd.ExecuteNonQueryAsync();
            }

            return connection;
        }

        private static async Task RotateCurrentConnectionToArchive(
            string archiveDbPath)
        {
            var dataSourceFileInfo = new FileInfo(_currentConnection.DataSource);

            await _currentConnection.CloseAsync()
                .ContinueWith(t =>
                {
                    File.Move(dataSourceFileInfo.FullName, Path.Combine(archiveDbPath, dataSourceFileInfo.Name));
                });
        }

        private static Action<CommandLineApplication> InfluxDbApplication(
            P1Config config)
        {
            return (target) =>
            {
                target.Description = "Write to influxdb";
                target.HelpOption("-? | -h | --help");

                var loggingOption = target.CreateLoggingOption();

                var influxHostOption = target.CreateInfluxHostOption();
                var influxDatabaseOption = target.CreateInfluxDatabaseOption();
                var influxUsernameOption = target.CreateInfluxUserNameOption();
                var influxPasswordOption = target.CreateInfluxPasswordOption();

                target.OnExecute(async () =>
                {
                    try
                    {
                        var loglevel = loggingOption.GetOptionalIntValue(3);
                        CreateDaemonLogger(loglevel);

                        var influxHost = influxHostOption.GetRequiredStringValue();
                        var influxDatabase = influxDatabaseOption.GetRequiredStringValue();
                        var influxUsername = influxUsernameOption.GetOptionalStringValue(null);
                        var influxPassword = influxPasswordOption.GetOptionalStringValue(null);

                        IStorage storage = new InfluxDbStorage(influxHost, influxDatabase, influxUsername, influxPassword);
                        _measurementsBuffer.RegisterMessageHandler(storage.SaveP1Measurement);

                        var serialPortReader = new SerialPortReader(config.Port, config.BaudRate, config.StopBits, config.Parity, config.DataBits, _serialMessageBuffer);
                        serialPortReader.StartReading();

                        Console.ReadLine();
                    }
                    catch (ConfigurationValueRequiredException exception)
                    {
                        Console.WriteLine(exception.Message);
                    }
                    catch (Exception exception)
                    {
                        Log.Fatal(exception, "Unexpected exception during startup");
                    }

                    return await WaitForCancellation();
                });
            };
        }

        private static void Main(
            string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var p1Config = new P1Config();

            config.GetSection("P1Config").Bind(p1Config);

            _serialMessageBuffer = new MessageBuffer<P1MessageCollection>();
            _measurementsBuffer = new MessageBuffer<P1Measurements>();
            var messageParser = new MessageParser(_measurementsBuffer);

            _serialMessageBuffer.RegisterMessageHandler(messageParser.ParseSerialMessages);

            var commandLineApplication = new CommandLineApplication(throwOnUnexpectedArg: false);

            commandLineApplication.HelpOption("-? | -h | --help");

            commandLineApplication.Command("influxdb", InfluxDbApplication(p1Config));
            commandLineApplication.Command("mysqldb", MysqlDbApplication(p1Config, config["MysqlConnection"]));
            commandLineApplication.Command("sqlite", SqliteApplication(p1Config, config.GetSection("SqliteFactorySettings")));
            commandLineApplication.Command("debug", DebugApplication(p1Config));

            commandLineApplication.OnExecute(() =>
            {
                commandLineApplication.ShowHelp();

                return 1;
            });

            try
            {
                commandLineApplication.Execute(args);
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