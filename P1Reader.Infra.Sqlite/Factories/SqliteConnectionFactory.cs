using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using P1Reader.Infra.Sqlite.Interfaces;
using P1ReaderApp.Interfaces;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace P1Reader.Infra.Sqlite.Factories
{
    public class SqliteConnectionFactory :
        IConnectionFactory<SqliteConnection>
    {
        private readonly IConfigurationSection configurationSection;
        private readonly ITrigger<FileInfo> _onSqliteDbRepotationTrigger;
        private DateTime? _lastTimeStamp = null;
        private SqliteConnection _currentConnection = null;
        private readonly ILogger _logger;

        public SqliteConnectionFactory(
            IConfiguration config,
            ITrigger<FileInfo> onSqliteDbRepotationTrigger,
            ILogger logger)
        {
            configurationSection = config.GetSection("SqliteFactorySettings");
            _onSqliteDbRepotationTrigger = onSqliteDbRepotationTrigger;
            _logger = logger;
        }

        public async Task<SqliteConnection> Create(
            DateTime timestamp,
            string initQuery)
        {
            if (!_lastTimeStamp.HasValue)
            {
                _lastTimeStamp = timestamp;
            }

            var diffDays = (timestamp.Date - _lastTimeStamp.Value.Date).TotalDays;

            Log.Verbose("A sqlite connection requested with diffDays={diffDays}", diffDays);

            if (diffDays < 0)
            {
                _lastTimeStamp = timestamp;

                _logger.Warning("Unexpected timestamp. This means that we already moved on to the next day, but there is still a measurement coming in from the previous day.");

                // This means that we already moved on to the next day, but there is still a measurement coming in from the previous day
                return null;
            }

            var localDbPath = configurationSection["LocalDbPath"];
            var archiveDbPath = configurationSection["ArchiveDbPath"];
            var dbFileNameForTimestamp = Path.Combine(localDbPath, $"{timestamp:yyyyMMdd}-p1power.db");

            if (diffDays > 0 && _currentConnection != null)
            {
                RotateCurrentConnectionToArchive(archiveDbPath);
                _currentConnection = null;
            }

            if (_currentConnection == null)
            {
                _currentConnection = await InitConnection(dbFileNameForTimestamp, initQuery);
            }

            _lastTimeStamp = timestamp;

            return _currentConnection;
        }

        private void RotateCurrentConnectionToArchive(
            string archiveDbPath)
        {
            var dataSourceFileInfo = new FileInfo(_currentConnection.DataSource);
            var dataTargetFileInfo = new FileInfo(Path.Combine(archiveDbPath, dataSourceFileInfo.Name));

            _logger.Information("Rotating {dataSourceFileInfo} to archive @ {archiveDbPath}", dataSourceFileInfo, archiveDbPath);

            _currentConnection.Dispose();

            try
            {
                File.Move(dataSourceFileInfo.FullName, dataTargetFileInfo.FullName);

                _onSqliteDbRepotationTrigger.FireTrigger(dataTargetFileInfo);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unexpected exception during rotation of sqlite connection {dataSourceFileInfo}", dataSourceFileInfo);
            }
        }

        public async Task<SqliteConnection> InitConnection(
            string dbFilePath,
            string initQuery)
        {
            _logger.Information("Initializing new sqlite connection to {dbFilePath}", dbFilePath);

            var connection = new SqliteConnection($"Data Source={dbFilePath}");

            await connection.OpenAsync();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = initQuery;

                await cmd.ExecuteNonQueryAsync();
            }

            return connection;
        }
    }
}
