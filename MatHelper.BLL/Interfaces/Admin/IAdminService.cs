using MatHelper.CORE.Models;
using MatHelper.DAL.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IAdminService
    {
        Task<AdminData> GetAdminDataAsync(Guid userId);
    }
}