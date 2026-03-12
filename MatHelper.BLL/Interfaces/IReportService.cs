using MatHelper.CORE.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IReportService
    {
        Task SubmitNotFoundReportAsync(NotFoundReportRequest request);
    }
}