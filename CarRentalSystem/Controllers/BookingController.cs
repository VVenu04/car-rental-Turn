using CarRentalSystem.Data;
using CarRentalSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem.Controllers
{
    public class BookingsController : Controller
    {
        private readonly CarRentalDbContext _context;

        public BookingsController(CarRentalDbContext context)
        {
            _context = context;
        }

        // GET: Bookings (Admin view of all bookings)
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized();
            }

            var bookings = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Car)
                .OrderByDescending(b => b.BookingDate);
            return View(await bookings.ToListAsync());
        }

        // GET: Bookings/Create
        public async Task<IActionResult> Create(int carId)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var car = await _context.Cars.FindAsync(carId);
            if (car == null || !car.IsAvailable)
            {
                TempData["ErrorMessage"] = "This car is not available for booking.";
                return RedirectToAction("Index", "Cars");
            }

            var booking = new Booking
            {
                CarID = carId,
                Car = car,
                CustomerID = userId.Value,
                PickupDate = DateTime.Today.AddDays(1),
                ReturnDate = DateTime.Today.AddDays(2)
            };

            return View(booking);
        }

        // POST: Bookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CarID,PickupDate,ReturnDate,SpecialRequirements,PickupLocation,ReturnLocation")] Booking booking)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Manually set the IDs and fetch the related Car object
            booking.CustomerID = userId.Value;
            var car = await _context.Cars.FindAsync(booking.CarID);
            if (car == null) return NotFound();

            booking.Car = car;

            // --- CORRECTION ---
            // Remove the navigation property validation errors, as we are handling them manually via IDs.
            ModelState.Remove("Customer");
            ModelState.Remove("Car");
            // --- END CORRECTION ---


            // --- Server-side Validation ---
            if (booking.PickupDate < DateTime.Today)
            {
                ModelState.AddModelError("PickupDate", "Pickup date cannot be in the past.");
            }
            if (booking.ReturnDate <= booking.PickupDate)
            {
                ModelState.AddModelError("ReturnDate", "Return date must be after the pickup date.");
            }

            // Check for overlapping bookings
            var isOverlapping = await _context.Bookings
                .AnyAsync(b => b.CarID == booking.CarID &&
                               b.BookingStatus != "Cancelled" &&
                               ((booking.PickupDate >= b.PickupDate && booking.PickupDate <= b.ReturnDate) ||
                                (booking.ReturnDate >= b.PickupDate && booking.ReturnDate <= b.ReturnDate)));

            if (isOverlapping)
            {
                ModelState.AddModelError("", "This car is already booked for the selected dates. Please choose different dates.");
            }


            if (ModelState.IsValid)
            {
                booking.TotalCost = booking.TotalDays * car.DailyRate;
                booking.BookingDate = DateTime.Now;
                booking.BookingStatus = "Confirmed";

                _context.Add(booking);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your booking has been confirmed!";
                return RedirectToAction("Profile", "Account");
            }

            // If we get here, something failed, redisplay form with error messages
            return View(booking);
        }
    }
}