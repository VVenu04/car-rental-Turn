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

        // A private helper method to avoid repeating code
        private void PopulateDropdowns()
        {
            ViewBag.TransmissionOptions = new List<string> { "Automatic", "Manual" };
            ViewBag.FuelTypeOptions = new List<string> { "Petrol", "Diesel", "Super Petrol", "Super Diesel" };
        }

        // GET: Cars (Customer Browse Page)
        public async Task<IActionResult> Index()
        {
            return View(await _context.Cars.ToListAsync());
        }

        // GET: Cars/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var car = await _context.Cars
                .FirstOrDefaultAsync(m => m.CarID == id);
            if (car == null)
            {
                return NotFound();
            }

            return View(car);
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("Role") == "Admin";
        }

        // GET: Cars/Create
        public IActionResult Create()
        {
            if (!IsAdmin()) return Unauthorized();

            // --- FIX ---
            // Populate dropdowns when the page first loads
            PopulateDropdowns();

            return View();
        }

        // POST: Cars/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CarName,CarModel,IsAvailable,DailyRate,CarType,FuelType,SeatingCapacity,Transmission,Description,Mileage")] Car car, IFormFile? imageFile)
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
                _context.Add(car);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Cars");
            }

            // --- FIX ---
            // If validation fails, repopulate dropdowns before returning to the view
            PopulateDropdowns();

            return View(car);
        }

        // GET: Cars/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdmin()) return Unauthorized();
            if (id == null) return NotFound();

            var car = await _context.Cars.FindAsync(id);
            if (car == null) return NotFound();

            // --- FIX ---
            // Populate dropdowns when the page first loads
            PopulateDropdowns();

            return View(car);
        }

        // POST: Cars/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CarID,CarName,CarModel,IsAvailable,DailyRate,CarType,FuelType,SeatingCapacity,Transmission,Description,Mileage,DateAdded")] Car car, IFormFile? imageFile)
        {
            if (!IsAdmin()) return Unauthorized();
            if (id != car.CarID) return NotFound();

            ModelState.Remove("ImageUrl");
            ModelState.Remove("imageFile");

            if (ModelState.IsValid)
            {
                var carToUpdate = await _context.Cars.FindAsync(id);
                if (carToUpdate == null)
                {
                    return NotFound();
                }

                carToUpdate.CarName = car.CarName;
                carToUpdate.CarModel = car.CarModel;
                carToUpdate.IsAvailable = car.IsAvailable;
                carToUpdate.DailyRate = car.DailyRate;
                carToUpdate.CarType = car.CarType;
                carToUpdate.FuelType = car.FuelType;
                carToUpdate.SeatingCapacity = car.SeatingCapacity;
                carToUpdate.Transmission = car.Transmission;
                carToUpdate.Description = car.Description;
                carToUpdate.Mileage = car.Mileage;

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
                    _context.Update(carToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CarExists(car.CarID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            // --- FIX ---
            // If validation fails, repopulate dropdowns before returning to the view
            PopulateDropdowns();

            return View(car);
        }

        // GET: Cars/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdmin()) return Unauthorized();
            if (id == null) return NotFound();

            var car = await _context.Cars.FirstOrDefaultAsync(m => m.CarID == id);
            if (car == null) return NotFound();

            return View(car);
        }

        // POST: Cars/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return Unauthorized();

            var car = await _context.Cars.FindAsync(id);
            if (car != null)
            {
                _context.Cars.Remove(car);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CarExists(int id)
        {
            return _context.Cars.Any(e => e.CarID == id);
        }
    }
}