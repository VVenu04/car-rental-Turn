using CarRentalSystem.Data;
using CarRentalSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CarRentalSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly CarRentalDbContext _context;

        public AccountController(CarRentalDbContext context)
        {
            _context = context;
        }

        //  Register
        public IActionResult Register()
        {
            return View();
        }

        //  Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "An account with this email already exists.");
                    return View(user);
                }

                user.Password = Convert.ToBase64String(Encoding.UTF8.GetBytes(user.Password));
                user.Role = "Customer"; // Ensure role is set to Customer
                user.DateJoined = DateTime.Now;

                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction("Login");
            }
            return View(user);
        }

        // GET: Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Email and password are required.";
                return View();
            }

            // Encode the provided password to match the stored one
            var encodedPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Password == encodedPassword);

            if (user != null && user.IsActive)
            {
                // Set session variables
                HttpContext.Session.SetInt32("UserID", user.UserID);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role);

                if (user.Role == "Admin")
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                return RedirectToAction("Profile");
            }

            ViewBag.Error = "Invalid login attempt.";
            return View();
        }

        // POST: Logout
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }


        // GET: Profile
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login"); // Not logged in
            }

            var user = await _context.Users
                .Include(u => u.Bookings)
                .ThenInclude(b => b.Car)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: EditProfile
        public async Task<IActionResult> EditProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            // We don't want to send the hashed password to the view
            user.Password = "";
            return View(user);
        }

        // POST: EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile([Bind("UserID,FullName,PhoneNumber")] User userModel, string newPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login"); // Not logged in, redirect.
            }

            if (userModel.UserID != userId)
            {
                return Unauthorized();
            }

            // Find the user in the database to update.
            var userToUpdate = await _context.Users.FindAsync(userModel.UserID);
            if (userToUpdate == null)
            {
                return NotFound();
            }

            
            userToUpdate.FullName = userModel.FullName;
            userToUpdate.PhoneNumber = userModel.PhoneNumber;

            // Update password only if a new one is provided
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                if (newPassword.Length < 6)
                {
                    ModelState.AddModelError("Password", "Password must be at least 6 characters long.");
                    
                    return View(userModel);
                }
                userToUpdate.Password = Convert.ToBase64String(Encoding.UTF8.GetBytes(newPassword));
            }

            try
            {
                _context.Update(userToUpdate);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.UserID == userModel.UserID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Profile));
        }

        // GET: /Account/Notifications
        public async Task<IActionResult> Notifications()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserID == userId)
                .OrderByDescending(n => n.DateSent)
                .ToListAsync();

            // Mark all fetched notifications as read
            foreach (var notification in notifications.Where(n => !n.IsRead))
            {
                notification.IsRead = true;
            }
            await _context.SaveChangesAsync();

            return View(notifications);
        }



    }






}