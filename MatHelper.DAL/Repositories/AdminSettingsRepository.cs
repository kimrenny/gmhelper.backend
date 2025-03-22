using MatHelper.DAL.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MatHelper.DAL.Models;
using MatHelper.CORE.Models;
using System.Security.Claims;

namespace MatHelper.DAL.Repositories
{
    public class AdminSettingsRepository
    {
        private readonly AppDbContext _context;

        public AdminSettingsRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AdminSettings?> GetByUserIdAsync(Guid userId)
        {
            return await _context.AdminSettings
                .Include(a => a.Sections)
                .ThenInclude(s => s.Switches)
                .FirstOrDefaultAsync(a => a.UserId == userId);
        }

        public async Task CreateAsync(AdminSettings adminSettings)
        {
            await _context.AdminSettings.AddAsync(adminSettings);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateSwitchAsync(Guid userId, int sectionId, string switchLabel, bool newValue)
        {
            var adminSettings = await _context.AdminSettings
                .Include(a => a.Sections)
                .ThenInclude(s => s.Switches)
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (adminSettings == null)
            {
                return false;
            }

            var section = adminSettings.Sections.FirstOrDefault(s => s.Id == sectionId);
            if(section == null)
            {
                return false;
            }

            var switchToUpdate = section.Switches.FirstOrDefault(sw => sw.Label == switchLabel);
            if (switchToUpdate == null)
            {
                return false;
            }

            switchToUpdate.Value = newValue;
            await _context.SaveChangesAsync();
            return true;

        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
