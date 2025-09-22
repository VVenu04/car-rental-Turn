using CarRentalSystem.Data;
using CarRentalSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem.Controllers
{
    public class CarsController : Controller
    {
        private readonly CarRentalDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public CarsController(CarRentalDbContext context, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        private void PopulateDropdowns()
        {
            ViewBag.TransmissionOptions = new List<string> { "Automatic", "Manual" };
            ViewBag.FuelTypeOptions = new List<string> { "Petrol", "Diesel", "Super Petrol", "Super Diesel" };
        }

        public async Task<IActionResult> Index(string searchString, string carType, string transmission, string fuelType, int? seatingCapacity)
        {
            ViewBag.CarTypes = await _context.Cars.Select(c => c.CarType).Distinct().OrderBy(t => t).ToListAsync();
            ViewBag.Transmissions = await _context.Cars.Select(c => c.Transmission).Distinct().OrderBy(t => t).ToListAsync();
            ViewBag.FuelTypes = await _context.Cars.Select(c => c.FuelType).Distinct().OrderBy(t => t).ToListAsync();
            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentCarType"] = carType;
            ViewData["CurrentTransmission"] = transmission;
            ViewData["CurrentFuelType"] = fuelType;
            ViewData["CurrentSeatingCapacity"] = seatingCapacity;

            var cars = _context.Cars.AsQueryable();
            if (!String.IsNullOrEmpty(searchString))
            {
                cars = cars.Where(c => c.CarName.Contains(searchString) || c.CarModel.Contains(searchString));
            }
            if (!String.IsNullOrEmpty(carType))
            {
                cars = cars.Where(c => c.CarType == carType);
            }
            if (!String.IsNullOrEmpty(transmission))
            {
                cars = cars.Where(c => c.Transmission == transmission);
            }
            if (!String.IsNullOrEmpty(fuelType))
            {
                cars = cars.Where(c => c.FuelType == fuelType);
            }
            if (seatingCapacity.HasValue)
            {
                cars = cars.Where(c => c.SeatingCapacity >= seatingCapacity.Value);
            }
            return View(await cars.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var car = await _context.Cars.FirstOrDefaultAsync(m => m.CarID == id);
            if (car == null) return NotFound();
            return View(car);
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("Role") == "Admin";
        }

        public IActionResult Create()
        {
            if (!IsAdmin()) return Unauthorized();
            PopulateDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CarName,CarModel,RegistrationNumber,IsAvailable,DailyRate,CarType,FuelType,SeatingCapacity,Transmission,Description,Mileage,PickupLocationsString,DropoffLocationsString")] Car car, IFormFile? imageFile)
        {
            if (!IsAdmin()) return Unauthorized();
            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError("imageFile", "Please select an image file to upload.");
            }
            if (ModelState.IsValid)
            {
                var uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "images", "cars");
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                await imageFile.CopyToAsync(new FileStream(filePath, FileMode.Create));
                car.ImageUrl = "/images/cars/" + uniqueFileName;
                car.DateAdded = DateTime.Now;
                if (!string.IsNullOrEmpty(car.PickupLocationsString))
                {
                    var locations = car.PickupLocationsString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var loc in locations)
                    {
                        car.AvailableLocations.Add(new CarLocation { LocationName = loc.Trim(), LocationType = "Pickup" });
                    }
                }
                if (!string.IsNullOrEmpty(car.DropoffLocationsString))
                {
                    var locations = car.DropoffLocationsString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var loc in locations)
                    {
                        car.AvailableLocations.Add(new CarLocation { LocationName = loc.Trim(), LocationType = "Dropoff" });
                    }
                }
                _context.Add(car);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Cars");
            }
            PopulateDropdowns();
            return View(car);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdmin()) return Unauthorized();
            if (id == null) return NotFound();
            var car = await _context.Cars.Include(c => c.AvailableLocations).FirstOrDefaultAsync(c => c.CarID == id);
            if (car == null) return NotFound();
            car.PickupLocationsString = string.Join("\n", car.AvailableLocations.Where(l => l.LocationType == "Pickup").Select(l => l.LocationName));
            car.DropoffLocationsString = string.Join("\n", car.AvailableLocations.Where(l => l.LocationType == "Dropoff").Select(l => l.LocationName));
            PopulateDropdowns();
            return View(car);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CarID,CarName,CarModel,RegistrationNumber,IsAvailable,DailyRate,CarType,FuelType,SeatingCapacity,Transmission,Description,Mileage,DateAdded,PickupLocationsString,DropoffLocationsString")] Car car, IFormFile? imageFile)
        {
            if (!IsAdmin()) return Unauthorized();
            if (id != car.CarID) return NotFound();
            ModelState.Remove("ImageUrl");
            ModelState.Remove("imageFile");
            if (ModelState.IsValid)
            {
                var carToUpdate = await _context.Cars.Include(c => c.AvailableLocations).FirstOrDefaultAsync(c => c.CarID == id);
                if (carToUpdate == null) return NotFound();
                carToUpdate.CarName = car.CarName;
                carToUpdate.CarModel = car.CarModel;
                carToUpdate.RegistrationNumber = car.RegistrationNumber; // Update Registration Number
                carToUpdate.IsAvailable = car.IsAvailable;
                carToUpdate.DailyRate = car.DailyRate;
                carToUpdate.CarType = car.CarType;
                carToUpdate.FuelType = car.FuelType;
                carToUpdate.SeatingCapacity = car.SeatingCapacity;
                carToUpdate.Transmission = car.Transmission;
                carToUpdate.Description = car.Description;
                carToUpdate.Mileage = car.Mileage;
                carToUpdate.AvailableLocations.Clear();
                if (!string.IsNullOrEmpty(car.PickupLocationsString))
                {
                    var locations = car.PickupLocationsString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var loc in locations)
                    {
                        carToUpdate.AvailableLocations.Add(new CarLocation { LocationName = loc.Trim(), LocationType = "Pickup" });
                    }
                }
                if (!string.IsNullOrEmpty(car.DropoffLocationsString))
                {
                    var locations = car.DropoffLocationsString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var loc in locations)
                    {
                        carToUpdate.AvailableLocations.Add(new CarLocation { LocationName = loc.Trim(), LocationType = "Dropoff" });
                    }
                }
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "images", "cars");
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    await imageFile.CopyToAsync(new FileStream(filePath, FileMode.Create));
                    carToUpdate.ImageUrl = "/images/cars/" + uniqueFileName;
                }
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CarExists(car.CarID)) { return NotFound(); } else { throw; }
                }
                return RedirectToAction(nameof(Index));
            }
            PopulateDropdowns();
            return View(car);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdmin()) return Unauthorized();
            if (id == null) return NotFound();
            var car = await _context.Cars.FirstOrDefaultAsync(m => m.CarID == id);
            if (car == null) return NotFound();
            return View(car);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return Unauthorized();
            var hasBookings = await _context.Bookings.AnyAsync(b => b.CarID == id);
            if (hasBookings)
            {
                TempData["DeleteError"] = "This car cannot be deleted because it is associated with existing bookings. Consider marking it as 'Unavailable' instead.";
                return RedirectToAction(nameof(Index));
            }
            var car = await _context.Cars.FindAsync(id);
            if (car != null)
            {
                string oldImagePath = car.ImageUrl;
                _context.Cars.Remove(car);
                await _context.SaveChangesAsync();
                if (!string.IsNullOrEmpty(oldImagePath) && oldImagePath != "/images/cars/placeholder.png")
                {
                    string webRootPath = _hostingEnvironment.WebRootPath;
                    string fullPath = Path.Combine(webRootPath, oldImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CarExists(int id)
        {
            return _context.Cars.Any(e => e.CarID == id);
        }
    }
}