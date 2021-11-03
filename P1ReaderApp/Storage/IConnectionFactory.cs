using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P1ReaderApp.Storage
{
    public interface IConnectionFactory<TConnection>
    {
        Task<TConnection> Create(
            DateTime timestamp);
    }
}
