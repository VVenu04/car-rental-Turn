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

        public async Task<IActionResult> Dashboard()
        {
            // ... (Your dashboard logic) ...
            return View();
        }

        // === ENHANCED CUSTOMER MANAGEMENT ===

        // GET: /Admin/ViewCustomers
        public async Task<IActionResult> ViewCustomers()
        {
            // This now only fetches Customers, as requested
            var customers = await _context.Users.Where(u => u.Role == "Customer").ToListAsync();
            return View(customers);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null && user.Role == "Customer") // Extra check to only affect customers
            {
                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Customer '{user.Username}' status has been updated.";
            }
            return RedirectToAction(nameof(ViewCustomers));
        }

        public async Task<IActionResult> SendNotification(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.Role != "Customer") return NotFound();

            ViewBag.UserName = user.Username;
            return View(new Notification { UserID = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendNotification(Notification notification)
        {
            // --- FIX ---
            // Remove the User object from model validation, as we only need the UserID.
            ModelState.Remove("User");
            // --- END FIX ---

            if (ModelState.IsValid)
            {
                notification.DateSent = DateTime.Now;
                notification.IsRead = false;
                _context.Add(notification);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Notification sent successfully.";
                return RedirectToAction(nameof(ViewCustomers)); // Changed to redirect to the customer list
            }

            // If model is invalid, find the username again to display in the view title
            var user = await _context.Users.FindAsync(notification.UserID);
            ViewBag.UserName = user?.Username ?? "User";
            return View(notification);
        }

        // ... (Other actions like CustomerDetails remain) ...
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




        
        [HttpGet]
        public async Task<IActionResult> ManageContactInfo()
        {
            // Find the first (or only) site setting in your database.
            var settings = await _context.SiteSettings.FirstOrDefaultAsync();

            // If no settings exist yet, create a new empty one.
            if (settings == null)
            {
                settings = new SiteSetting();
            }

            // Pass the settings object to the view.
            return View(settings);
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageContactInfo(SiteSetting model)
        {
            if (ModelState.IsValid)
            {
                // here Checking if the setting already exists in the database
                var settingInDb = await _context.SiteSettings.FindAsync(model.SettingID);

                if (settingInDb != null)
                {
                    // If it exists, update its properties
                    settingInDb.ContactEmail = model.ContactEmail;
                    settingInDb.ContactPhone = model.ContactPhone;
                    settingInDb.Address = model.Address;
                    _context.Update(settingInDb);
                }
                else
                {
                    // If it's a new setting, add it
                    _context.Add(model);
                }

                await _context.SaveChangesAsync(); // Save the changes to the database

                TempData["SuccessMessage"] = "Site information updated successfully!";
                return RedirectToAction("Dashboard"); // Redirect to the dashboard after saving
            }

            // If the model is not valid, return to the view with the entered data
            return View(model);
        }
    }
}