using System.Threading.Tasks;

namespace P1Report.Infra.Pdf.Interfaces
{
    public interface IReportBuilder<TArgs>
    {
        Task BuildReport(
            TArgs args);
    }
}
