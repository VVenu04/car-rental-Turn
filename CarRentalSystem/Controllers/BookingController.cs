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

            // --- FIX: ADDING THE OVERLAP CHECK BACK FOR IMMEDIATE FEEDBACK ---
            var isOverlapping = await _context.Bookings
                .AnyAsync(b => b.CarID == booking.CarID &&
                               b.BookingStatus != "Cancelled" &&
                               booking.PickupDate <= b.ReturnDate &&
                               booking.ReturnDate >= b.PickupDate);

            if (isOverlapping)
            {
                ModelState.AddModelError("", "This car is not available for the selected dates. Please choose a different date range.");
            }
            // --- END OF FIX ---

            if (booking.ReturnDate < booking.PickupDate)
            {
                ModelState.AddModelError("ReturnDate", "Return date cannot be before the pickup date.");
            }

            if (ModelState.IsValid)
            {
                booking.TotalCost = booking.TotalDays * car.DailyRate;
                var bookingJson = JsonSerializer.Serialize(booking);
                HttpContext.Session.SetString("TemporaryBooking", bookingJson);
                return RedirectToAction(nameof(BookingSummary));
            }

            // If validation fails, we need to repopulate the dropdowns before returning the view
            var pickupLocations = await _context.CarLocations
                .Where(l => l.CarId == car.CarID && l.LocationType == "Pickup")
                .Select(l => l.LocationName)
                .ToListAsync();
            var dropoffLocations = await _context.CarLocations
                .Where(l => l.CarId == car.CarID && l.LocationType == "Dropoff")
                .Select(l => l.LocationName)
                .ToListAsync();
            ViewBag.PickupLocations = new SelectList(pickupLocations);
            ViewBag.DropoffLocations = new SelectList(dropoffLocations);

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitSummary(string paymentMethod)
        {
            var bookingJson = HttpContext.Session.GetString("TemporaryBooking");
            if (string.IsNullOrEmpty(bookingJson))
            {
                return RedirectToAction("Index", "Home");
            }

            var booking = JsonSerializer.Deserialize<Booking>(bookingJson);
            booking.PaymentMethod = paymentMethod;

            // --- CRITICAL FIX: Perform the overlap check right before saving ---
            var isOverlapping = await _context.Bookings
                .AnyAsync(b => b.CarID == booking.CarID &&
                               b.BookingStatus != "Cancelled" &&
                               booking.PickupDate <= b.ReturnDate && // Correct logic: New start is before or same as existing end
                               booking.ReturnDate >= b.PickupDate); // Correct logic: New end is after or same as existing start

            if (isOverlapping)
            {
                TempData["ErrorMessage"] = "Sorry, this car has just been booked for the selected dates by another user. Please try different dates.";
                return RedirectToAction("Index", "Cars");
            }
            // --- END OF FIX ---

            if (paymentMethod == "PayOnPickup")
            {
                _context.Entry(booking.Car).State = EntityState.Unchanged;
                booking.BookingStatus = "Confirmed";
                booking.PaymentStatus = "Due at Pickup";
                booking.BookingDate = DateTime.Now;

                _context.Add(booking);
                await _context.SaveChangesAsync(); // Changed to async

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

            // --- CRITICAL FIX: Perform the overlap check right before saving ---
            var isOverlapping = await _context.Bookings
                .AnyAsync(b => b.CarID == booking.CarID &&
                               b.BookingStatus != "Cancelled" &&
                               booking.PickupDate <= b.ReturnDate &&
                               booking.ReturnDate >= b.PickupDate);

            if (isOverlapping)
            {
                TempData["ErrorMessage"] = "Sorry, this car has just been booked for the selected dates by another user. Please try different dates.";
                return RedirectToAction("Index", "Cars");
            }
            // --- END OF FIX ---

            _context.Entry(booking.Car).State = EntityState.Unchanged;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var userRole = HttpContext.Session.GetString("Role");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account"); // Not logged in
            }

            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null)
            {
                return NotFound();
            }

            // Security Check: Ensure the user is an admin OR owns the booking
            if (userRole != "Admin" && booking.CustomerID != userId)
            {
                return Unauthorized();
            }

            // Business Rule: Can only cancel if the pickup date is in the future
            if (booking.PickupDate <= DateTime.Today)
            {
                TempData["Error"] = "This booking cannot be cancelled as the pickup date is today or in the past.";
            }
            else if (booking.BookingStatus != "Confirmed")
            {
                TempData["Error"] = "Only 'Confirmed' bookings can be cancelled.";
            }
            else
            {
                booking.BookingStatus = "Cancelled";
                _context.Update(booking);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Booking has been successfully cancelled.";
            }

            // Redirect back to the appropriate page
            if (userRole == "Admin")
            {
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction("Profile", "Account");
        }
    }
}