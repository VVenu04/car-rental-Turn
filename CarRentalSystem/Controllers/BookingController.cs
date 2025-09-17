using CarRentalSystem.Data;
using CarRentalSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CarRentalSystem.Controllers
{
    public class BookingsController : Controller
    {
        private readonly CarRentalDbContext _context;

        public BookingsController(CarRentalDbContext context)
        {
            _context = context;
        }

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

            // --- NEW ---
            // Fetch the separate, predefined locations for this car
            var pickupLocations = await _context.CarLocations
                .Where(l => l.CarId == carId && l.LocationType == "Pickup")
                .Select(l => l.LocationName)
                .ToListAsync();

            var dropoffLocations = await _context.CarLocations
                .Where(l => l.CarId == carId && l.LocationType == "Dropoff")
                .Select(l => l.LocationName)
                .ToListAsync();

            ViewBag.PickupLocations = new SelectList(pickupLocations);
            ViewBag.DropoffLocations = new SelectList(dropoffLocations);
            // --- END NEW ---

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CarID,PickupDate,ReturnDate,SpecialRequirements,PickupLocation,ReturnLocation")] Booking booking)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToAction("Login", "Account");

            booking.CustomerID = userId.Value;
            var car = await _context.Cars.FindAsync(booking.CarID);
            if (car == null) return NotFound();
            booking.Car = car;

            ModelState.Remove("Customer");
            ModelState.Remove("Car");

            if (booking.ReturnDate <= booking.PickupDate)
            {
                ModelState.AddModelError("ReturnDate", "Return date must be after the pickup date.");
            }

            if (ModelState.IsValid)
            {
                booking.TotalCost = booking.TotalDays * car.DailyRate;
                var bookingJson = JsonSerializer.Serialize(booking);
                HttpContext.Session.SetString("TemporaryBooking", bookingJson);
                return RedirectToAction(nameof(BookingSummary));
            }

            return View(booking);
        }

        public IActionResult BookingSummary()
        {
            var bookingJson = HttpContext.Session.GetString("TemporaryBooking");
            if (string.IsNullOrEmpty(bookingJson))
            {
                return RedirectToAction("Index", "Home");
            }

            var booking = JsonSerializer.Deserialize<Booking>(bookingJson);
            booking.Car = _context.Cars.Find(booking.CarID);
            return View(booking);
        }

        // NEW ACTION: Handles the choice from the summary page
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitSummary(string paymentMethod)
        {
            var bookingJson = HttpContext.Session.GetString("TemporaryBooking");
            if (string.IsNullOrEmpty(bookingJson))
            {
                return RedirectToAction("Index", "Home");
            }

            var booking = JsonSerializer.Deserialize<Booking>(bookingJson);
            booking.PaymentMethod = paymentMethod;

            if (paymentMethod == "PayOnPickup")
            {
                // --- FIX ---
                // Tell EF Core that the deserialized Car object already exists in the DB.
                _context.Entry(booking.Car).State = EntityState.Unchanged;
                // --- END FIX ---

                booking.BookingStatus = "Confirmed";
                booking.PaymentStatus = "Due at Pickup";
                booking.BookingDate = DateTime.Now;

                _context.Add(booking);
                _context.SaveChanges();

                HttpContext.Session.Remove("TemporaryBooking");
                TempData["BookingId"] = booking.BookingID;
                return RedirectToAction(nameof(BookingConfirmation));
            }
            else // Default to "PayNow"
            {
                var updatedBookingJson = JsonSerializer.Serialize(booking);
                HttpContext.Session.SetString("TemporaryBooking", updatedBookingJson);
                return RedirectToAction(nameof(Payment));
            }
        }

        public IActionResult Payment()
        {
            var bookingJson = HttpContext.Session.GetString("TemporaryBooking");
            if (string.IsNullOrEmpty(bookingJson))
            {
                return RedirectToAction("Index", "Home");
            }
            var booking = JsonSerializer.Deserialize<Booking>(bookingJson);
            ViewBag.TotalCost = booking.TotalCost;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment()
        {
            var bookingJson = HttpContext.Session.GetString("TemporaryBooking");
            if (string.IsNullOrEmpty(bookingJson))
            {
                return RedirectToAction("Index", "Home");
            }

            var booking = JsonSerializer.Deserialize<Booking>(bookingJson);

            // --- FIX ---
            // We need the same fix here for the "Pay Now" path.
            _context.Entry(booking.Car).State = EntityState.Unchanged;
            // --- END FIX ---

            booking.BookingStatus = "Confirmed";
            booking.PaymentStatus = "Paid";
            booking.TransactionId = "FAKE_TRAN_" + Guid.NewGuid().ToString();
            booking.BookingDate = DateTime.Now;

            _context.Add(booking);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("TemporaryBooking");
            TempData["BookingId"] = booking.BookingID;
            return RedirectToAction(nameof(BookingConfirmation));
        }

        public async Task<IActionResult> BookingConfirmation()
        {
            if (TempData["BookingId"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var bookingId = (int)TempData["BookingId"];
            var booking = await _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }
    }
}