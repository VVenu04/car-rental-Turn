using CarRentalSystem.Data;
using CarRentalSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CarRentalSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly CarRentalDbContext _context;

        public AdminController(CarRentalDbContext context)
        {
            _context = context;
        }

        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext filterContext)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                filterContext.Result = new RedirectToActionResult("Login", "Account", null);
            }
            base.OnActionExecuting(filterContext);
        }
        //dashboard
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalCars = await _context.Cars.CountAsync();
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.TotalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer");
            return View();
        }
        //view customer
        public async Task<IActionResult> ViewCustomers()
        {
            var customers = await _context.Users.Where(u => u.Role == "Customer").ToListAsync();
            return View(customers);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null && user.Role == "Customer")
            {
                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Customer '{user.Username}' status has been updated.";
            }
            return RedirectToAction(nameof(ViewCustomers));
        }

        // GET: /Admin/SendNotification/5
        public async Task<IActionResult> SendNotification(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.Role != "Customer")
            {
                return NotFound();
            }

            // Pass the entire user object to the view for the details panel
            ViewBag.Recipient = user;

            return View(new Notification { UserID = id });
        }

        // POST: /Admin/SendNotification
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendNotification(Notification notification)
        {
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                notification.DateSent = DateTime.Now;
                notification.IsRead = false;
                _context.Add(notification);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Notification sent successfully.";
                return RedirectToAction(nameof(ViewCustomers));
            }

            // If validation fails, repopulate the Recipient details before returning the view
            var user = await _context.Users.FindAsync(notification.UserID);
            ViewBag.Recipient = user;

            return View(notification);
        }

        public async Task<IActionResult> CustomerDetails(int? id)
        {
            if (id == null) return NotFound();
            var customer = await _context.Users
                .Include(u => u.Bookings)
                    .ThenInclude(b => b.Car)
                .FirstOrDefaultAsync(m => m.UserID == id);
            if (customer == null || customer.Role != "Customer") return NotFound();
            return View(customer);
        }

        // === SITE SETTINGS MANAGEMENT ===

        [HttpGet]
        public async Task<IActionResult> ManageContactInfo()
        {
            var settings = await _context.SiteSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new SiteSetting();
            }
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageContactInfo(SiteSetting model)
        {
            if (ModelState.IsValid)
            {
                var settingInDb = await _context.SiteSettings.FindAsync(model.SettingID);
                if (settingInDb != null)
                {
                    settingInDb.ContactEmail = model.ContactEmail;
                    settingInDb.ContactPhone = model.ContactPhone;
                    settingInDb.Address = model.Address;
                    _context.Update(settingInDb);
                }
                else
                {
                    _context.Add(model);
                }
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Site information updated successfully!";
                return RedirectToAction("Dashboard");
            }
            return View(model);
        }
    }
}