using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace P1ReaderApp.Storage
{
    public class SqliteConnectionFactory :
        IConnectionFactory<SqliteConnection>
    {
        private readonly IConfigurationSection configurationSection;
        private DateTime? _lastTimeStamp = null;
        private SqliteConnection _currentConnection = null;

        public SqliteConnectionFactory(
            IConfiguration config)
        {
            configurationSection = config.GetSection("SqliteFactorySettings");
        }

        public async Task<SqliteConnection> Create(
            DateTime timestamp)
        {
            if (!_lastTimeStamp.HasValue)
            {
                _lastTimeStamp = timestamp;
            }

            var diffDays = (timestamp.Date - _lastTimeStamp.Value.Date).TotalDays;

            Log.Debug("A sqlite connection requested with diffDays={diffDays}", diffDays);

            if (diffDays < 0)
            {
                _lastTimeStamp = timestamp;

                Log.Warning("Unexpected timestamp. This means that we already moved on to the next day, but there is still a measurement coming in from the previous day.");

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

            _lastTimeStamp = timestamp;

            return _currentConnection;
        }

        private async Task<SqliteConnection> InitConnection(
            string dbFilePath)
        {
            Log.Information("Initializing new sqlite connection to {dbFilePath}", dbFilePath);

            var connection = new SqliteConnection($"Data Source={dbFilePath}");

            await connection.OpenAsync();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = SqLiteStorage.CreateTableQuery;

                await cmd.ExecuteNonQueryAsync();
            }

            return connection;
        }

        private async Task RotateCurrentConnectionToArchive(
            string archiveDbPath)
        {
            var dataSourceFileInfo = new FileInfo(_currentConnection.DataSource);

            Log.Information("Rotating {dataSourceFileInfo} to archive @ {archiveDbPath}", dataSourceFileInfo, archiveDbPath);

            await _currentConnection.CloseAsync();

            try
            {
                File.Move(dataSourceFileInfo.FullName, Path.Combine(archiveDbPath, dataSourceFileInfo.Name));
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Unexpected exception during rotation of sqlite connection {dataSourceFileInfo}", dataSourceFileInfo);
            }
        }
    }
}
