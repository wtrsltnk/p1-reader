using P1ReaderApp.Interfaces;
using System.IO;

namespace P1Reader.Infra.Sqlite.Services
{
    public class OnSqliteDbRotationTrigger :
        ITrigger<FileInfo>
    {
        public event TriggerEventHandler<FileInfo> Trigger;

        public void FireTrigger(
            FileInfo args)
        {
            Trigger?.Invoke(args);
        }
    }
}
