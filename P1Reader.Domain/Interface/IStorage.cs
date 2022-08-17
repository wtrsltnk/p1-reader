using P1Reader.Domain.P1;
using System.Threading.Tasks;

namespace P1Reader.Domain.Interfaces
{
    public interface IStorage
    {
        Task SaveP1MeasurementAsync(
            Measurement p1Measurements);
    }
}