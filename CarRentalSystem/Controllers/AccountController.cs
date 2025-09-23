﻿using CarRentalSystem.Data;
using CarRentalSystem.Models;
using CarRentalSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace CarRentalSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly CarRentalDbContext _context;
        private readonly IEmailService _emailService;

        public AccountController(CarRentalDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // --- REGISTRATION & OTP VERIFICATION ---
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "An account with this email already exists.");
                    return View(user);
                }

                var otp = new Random().Next(100000, 999999).ToString();
                user.VerificationOtp = otp;
                user.VerificationOtpExpires = DateTime.Now.AddMinutes(10);

                var emailBody = $"<h3>Welcome to DriveEase!</h3><p>Your One-Time Password (OTP) is: <strong>{otp}</strong></p>";
                await _emailService.SendEmailAsync(user.Email, "Verify Your Account with OTP", emailBody);

                HttpContext.Session.SetString("TempUser", JsonSerializer.Serialize(user));
                TempData["VerificationEmail"] = user.Email;
                return RedirectToAction("VerifyRegistrationOtp");
            }
            return View(user);
        }

        public IActionResult VerifyRegistrationOtp()
        {
            ViewBag.Email = TempData["VerificationEmail"];
            if (ViewBag.Email == null) return RedirectToAction("Register");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyRegistrationOtp(string email, string otp)
        {
            var tempUserJson = HttpContext.Session.GetString("TempUser");
            if (string.IsNullOrEmpty(tempUserJson))
            {
                ViewBag.Error = "Your session has expired. Please register again.";
                return View();
            }

            var user = JsonSerializer.Deserialize<User>(tempUserJson);

            if (user.Email != email || user.VerificationOtp != otp)
            {
                ViewBag.Error = "The OTP you entered is incorrect.";
                ViewBag.Email = email;
                return View();
            }

            if (user.VerificationOtpExpires < DateTime.Now)
            {
                ViewBag.Error = "The OTP has expired. Please register again.";
                ViewBag.Email = email;
                return View();
            }

            user.Password = Convert.ToBase64String(Encoding.UTF8.GetBytes(user.Password));
            user.IsEmailVerified = true;
            user.VerificationOtp = null;
            user.VerificationOtpExpires = null;
            user.DateJoined = DateTime.Now;
            user.Role = "Customer";

            _context.Add(user);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("TempUser");

            TempData["SuccessMessage"] = "Registration successful! You can now log in.";
            return RedirectToAction("Login");
        }


        // --- LOGIN (CUSTOMER & ADMIN) ---
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string loginType)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Email and password are required.";
                return loginType == "Admin" ? View("AdminLogin") : View();
            }

            var encodedPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Password == encodedPassword);

            if (user != null && user.IsActive)
            {
                if (!user.IsEmailVerified && !user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    ViewBag.Error = "Your account is not verified. Please register again to get a new OTP.";
                    return View();
                }
                if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) && loginType != "Admin")
                {
                    ViewBag.Error = "Administrators must use the dedicated admin login page.";
                    return View();
                }
                if (!user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) && loginType == "Admin")
                {
                    ViewBag.Error = "This login page is for administrators only.";
                    return View("AdminLogin");
                }

                HttpContext.Session.SetInt32("UserID", user.UserID);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role);

                if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                return RedirectToAction("Index", "Cars");
            }

            ViewBag.Error = "Invalid login attempt.";
            if (loginType == "Admin")
            {
                return View("AdminLogin");
            }
            return View();
        }

        [HttpGet("Account/admin/Login")]
        public IActionResult AdminLogin()
        {
            return View();
        }


        // --- FORGOT PASSWORD FLOW ---
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                var otp = new Random().Next(100000, 999999).ToString();
                user.PasswordResetOtp = otp;
                user.PasswordResetOtpExpires = DateTime.Now.AddMinutes(10);
                await _context.SaveChangesAsync();

                var emailBody = $"<h3>Password Reset Request</h3><p>Your One-Time Password (OTP) is: <strong>{otp}</strong></p>";
                await _emailService.SendEmailAsync(user.Email, "Your Password Reset OTP", emailBody);

                TempData["ResetEmail"] = user.Email;
                return RedirectToAction("VerifyOtp");
            }

            ViewBag.Message = "If an account with that email exists, an OTP has been sent.";
            return View();
        }

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            ViewBag.Email = TempData["ResetEmail"];
            TempData.Keep("ResetEmail");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(string email, string otp)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.PasswordResetOtp == otp);

            if (user != null && user.PasswordResetOtpExpires > DateTime.Now)
            {
                HttpContext.Session.SetString("VerifiedResetEmail", email);
                return RedirectToAction("ResetPassword");
            }

            ViewBag.Error = "Invalid or expired OTP. Please try again.";
            ViewBag.Email = email;
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            var email = HttpContext.Session.GetString("VerifiedResetEmail");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }
            return View(new User { Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string password, string confirmPassword)
        {
            var authorizedEmail = HttpContext.Session.GetString("VerifiedResetEmail");
            if (string.IsNullOrEmpty(authorizedEmail) || email != authorizedEmail)
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(password) || password.Length < 6)
            {
                ModelState.AddModelError("password", "Password must be at least 6 characters long.");
            }
            if (password != confirmPassword)
            {
                ModelState.AddModelError("confirmPassword", "The passwords do not match.");
            }

            if (!ModelState.IsValid)
            {
                return View(new User { Email = email });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                user.Password = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));
                user.PasswordResetOtp = null;
                user.PasswordResetOtpExpires = null;
                await _context.SaveChangesAsync();

                HttpContext.Session.Remove("VerifiedResetEmail");
                TempData["SuccessMessage"] = "Your password has been reset successfully. You can now log in.";
                return RedirectToAction("Login");
            }

            TempData["ErrorMessage"] = "An unexpected error occurred.";
            return RedirectToAction("ForgotPassword");
        }


        // --- PROFILE & NOTIFICATIONS ---
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToAction("Login");

            var user = await _context.Users
                .Include(u => u.Bookings).ThenInclude(b => b.Car)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null) return NotFound();
            return View(user);
        }

        public async Task<IActionResult> EditProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.Password = "";
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile([Bind("UserID,FullName,PhoneNumber")] User userModel, string newPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToAction("Login");
            if (userModel.UserID != userId) return Unauthorized();

            var userToUpdate = await _context.Users.FindAsync(userModel.UserID);
            if (userToUpdate == null) return NotFound();

            userToUpdate.FullName = userModel.FullName;
            userToUpdate.PhoneNumber = userModel.PhoneNumber;

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                if (newPassword.Length < 6)
                {
                    ModelState.AddModelError("Password", "Password must be at least 6 characters long.");
                    return View(userModel);
                }
                userToUpdate.Password = Convert.ToBase64String(Encoding.UTF8.GetBytes(newPassword));
            }

            _context.Update(userToUpdate);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Profile));
        }

        public async Task<IActionResult> Notifications()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToAction("Login");

            var notifications = await _context.Notifications
                .Where(n => n.UserID == userId)
                .OrderByDescending(n => n.DateSent)
                .ToListAsync();

            foreach (var notification in notifications.Where(n => !n.IsRead))
            {
                notification.IsRead = true;
            }
            await _context.SaveChangesAsync();

            return View(notifications);
        }
    }
}