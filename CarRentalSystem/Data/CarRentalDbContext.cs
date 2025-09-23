using CarRentalSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CarRentalSystem.Data
{
    public class CarRentalDbContext : DbContext
    {
        public CarRentalDbContext(DbContextOptions<CarRentalDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Car> Cars { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<SiteSetting> SiteSettings { get; set; }

        public DbSet<CarLocation> CarLocations { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Define a static date to avoid the pending model changes error
            var seedDate = new DateTime(2025, 1, 1);

            // Configure relationships
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Customer)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.CustomerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Car)
                .WithMany(c => c.Bookings)
                .HasForeignKey(b => b.CarID)
                .OnDelete(DeleteBehavior.Restrict);
            // Seed Site Settings
            modelBuilder.Entity<SiteSetting>().HasData(
                new SiteSetting
                {
                    SettingID = 1, // We will only ever have one row with ID = 1
                    ContactEmail = "Vvenujan04@gmail.com",
                    ContactPhone = "0741514769",
                    Address = "mvc building jaffna"
                }
            );

            // Seed Admin User
            // Simple Base64 encoding for the password "Admin@123". NOT FOR PRODUCTION!
            var adminPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes("Admin@123"));
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserID = 1,
                    Username = "admin",
                    Email = "admin@example.com",
                    Password = adminPassword, // Password: Admin@123
                    Role = "Admin",
                    FullName = "Administrator",
                    PhoneNumber = "1234567890",
                    DateJoined = seedDate, // <-- CORRECTED
                    IsActive = true,

                    IdentificationNumber = "ADMIN001", // 1. Add a placeholder ID
                    IsEmailVerified = true,           // 2. Mark the admin as verified
                }

            );

            // Seed Cars
            //modelBuilder.Entity<Car>().HasData(
            //    new Car
            //    {
            //        CarID = 1,
            //        CarName = "Toyota Camry",
            //        CarModel = "2023",
            //        ImageUrl = "/images/cars/placeholder.png",
            //        IsAvailable = true,
            //        DailyRate = 50.00m,
            //        CarType = "Sedan",
            //        FuelType = "Petrol",
            //        SeatingCapacity = 5,
            //        Transmission = "Automatic",
            //        Description = "A reliable and comfortable sedan for city and highway driving.",
            //        Mileage = 15000,
            //        DateAdded = seedDate // <-- CORRECTED
            //    },
            //    new Car
            //    {
            //        CarID = 2,
            //        CarName = "Ford Explorer",
            //        CarModel = "2022",
            //        ImageUrl = "/images/cars/placeholder.png",
            //        IsAvailable = true,
            //        DailyRate = 85.00m,
            //        CarType = "SUV",
            //        FuelType = "Petrol",
            //        SeatingCapacity = 7,
            //        Transmission = "Automatic",
            //        Description = "A spacious SUV perfect for family trips and adventures.",
            //        Mileage = 25000,
            //        DateAdded = seedDate // <-- CORRECTED
            //    },
            //    new Car
            //    {
            //        CarID = 3,
            //        CarName = "Honda Civic",
            //        CarModel = "2024",
            //        ImageUrl = "/images/cars/placeholder.png",
            //        IsAvailable = false,
            //        DailyRate = 45.00m,
            //        CarType = "Hatchback",
            //        FuelType = "Petrol",
            //        SeatingCapacity = 5,
            //        Transmission = "Manual",
            //        Description = "A sporty and fuel-efficient hatchback, great for city driving.",
            //        Mileage = 5000,
            //        DateAdded = seedDate // <-- CORRECTED
            //    }
            //);
        }
    }
    }
