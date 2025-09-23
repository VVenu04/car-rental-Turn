using CarRentalSystem.Data;
using CarRentalSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem.Services
{
    public class SiteSettingsService
    {
        private readonly CarRentalDbContext _context;
        private SiteSetting _cachedSettings;

        public SiteSettingsService(CarRentalDbContext context)
        {
            _context = context;
        }

        public async Task<SiteSetting> GetSiteSettingsAsync()
        {
            // Simple caching: fetch from DB only once per request.
            if (_cachedSettings == null)
            {
                _cachedSettings = await _context.SiteSettings.FirstOrDefaultAsync(s => s.SettingID == 1);
            }
            return _cachedSettings;
        }
    }
}