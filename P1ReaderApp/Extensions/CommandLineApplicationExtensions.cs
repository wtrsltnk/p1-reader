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
    }
}