using CarRentalSystem.Data;
using CarRentalSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CarRentalSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly CarRentalDbContext _context;

        public HomeController(CarRentalDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Fetch distinct options for the filter dropdowns on the home page
            ViewBag.CarTypes = await _context.Cars.Select(c => c.CarType).Distinct().OrderBy(t => t).ToListAsync();
            ViewBag.Transmissions = await _context.Cars.Select(c => c.Transmission).Distinct().OrderBy(t => t).ToListAsync();
            ViewBag.FuelTypes = await _context.Cars.Select(c => c.FuelType).Distinct().OrderBy(t => t).ToListAsync();

            // Fetch featured cars to display
            var featuredCars = await _context.Cars
                .Where(c => c.IsAvailable)
                .OrderByDescending(c => c.DateAdded)
                .Take(3)
                .ToListAsync();

            return View(featuredCars);
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult WhyChooseUs()
        {
            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}