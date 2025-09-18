using CarRentalSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem.Services
{
    public class NotificationService
    {
        private readonly CarRentalDbContext _context;

        public NotificationService(CarRentalDbContext context)
        {
            _context = context;
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserID == userId && !n.IsRead);
        }
    }
}