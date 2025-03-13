using MatHelper.CORE.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IUserMapper
    {
        AdminUserDto MapToAdminUserDto(User user);
    }
}
