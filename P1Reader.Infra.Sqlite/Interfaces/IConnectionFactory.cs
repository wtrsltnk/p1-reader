using System;
using System.Threading.Tasks;

namespace P1Reader.Infra.Sqlite.Interfaces
{
    public interface IConnectionFactory<TConnection>
    {
        Task<TConnection> Create(
            DateTime timestamp,
            string initQuery);
    }
}
