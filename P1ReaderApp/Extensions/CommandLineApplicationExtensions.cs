using Microsoft.Extensions.CommandLineUtils;

namespace P1ReaderApp.Extensions
{
    public static class CommandLineApplicationExtensions
    {
        public static CommandOption CreateInfluxDatabaseOption(this CommandLineApplication app) =>
            app.Option("--influxdatabase", "InfluxDB database name", CommandOptionType.SingleValue);

        public static CommandOption CreateInfluxHostOption(this CommandLineApplication app) =>
            app.Option("--influxhost", "InfluxDB server host", CommandOptionType.SingleValue);

        public static CommandOption CreateInfluxPasswordOption(this CommandLineApplication app) =>
            app.Option("--influxpassword", "InfluxDB database password", CommandOptionType.SingleValue);

        public static CommandOption CreateInfluxUserNameOption(this CommandLineApplication app) =>
            app.Option("--influxusername", "InfluxDB database username", CommandOptionType.SingleValue);

        public static CommandOption CreateLoggingOption(this CommandLineApplication app) =>
            app.Option("--logging", "Log level to display (0 = Verbose, 1 = Debug, 2 = Information, 3 = Warning, 4 = Error, 5 = Fatal)", CommandOptionType.SingleValue);
    }
}