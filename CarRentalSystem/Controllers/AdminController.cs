using CarRentalSystem.Data;
using CarRentalSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly CarRentalDbContext _context;

        public AdminController(CarRentalDbContext context)
        {
            _context = context;
        }

        // Action filter to check for admin role
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
            ViewBag.TotalCars = await _context.Cars.CountAsync();
            ViewBag.AvailableCars = await _context.Cars.CountAsync(c => c.IsAvailable);
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();

            return View();
        }

        // GET: /Admin/ViewCustomers
        public async Task<IActionResult> ViewCustomers()
        {
            // We only want to list users with the "Customer" role
            var customers = await _context.Users
                .Where(u => u.Role == "Customer")
                .OrderByDescending(u => u.DateJoined)
                .ToListAsync();

            return View(customers);
        }

        // GET: /Admin/CustomerDetails/5
        public async Task<IActionResult> CustomerDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // We include the bookings and the car details for each booking
            var customer = await _context.Users
                .Include(u => u.Bookings)
                    .ThenInclude(b => b.Car)
                .FirstOrDefaultAsync(m => m.UserID == id);

            if (customer == null || customer.Role != "Customer")
            {
                return NotFound();
            }

            return View(customer);
        }

        // GET: /Admin/ManageContactInfo
        public async Task<IActionResult> ManageContactInfo()
        {
            // We assume there is only one settings record with ID = 1
            var settings = await _context.SiteSettings.FindAsync(1);
            if (settings == null)
            {
                return NotFound(); // Should not happen if seeding is correct
            }
            return View(settings);
        }

        // POST: /Admin/ManageContactInfo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageContactInfo(SiteSetting siteSetting)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(siteSetting);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Contact information updated successfully!";
                    return RedirectToAction(nameof(Dashboard));
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Handle concurrency error if necessary
                    ModelState.AddModelError("", "Unable to save changes. The record was modified by another user.");
                }
            }
            // If we get here, something failed, redisplay form
            return View(siteSetting);
        }
    }
}