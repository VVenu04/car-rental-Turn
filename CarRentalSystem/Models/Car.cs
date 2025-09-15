using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarRentalSystem.Models
{
    public class Car
    {
        [Key]
        public int CarID { get; set; }

        [Required]
        [StringLength(100)]
        public string CarName { get; set; }

        [Required]
        [Display(Name = "Model Year")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "Please enter a valid 4-digit year.")]
        public string CarModel { get; set; }

        [StringLength(200)]
        public string? ImageUrl { get; set; } // This is for the saved path, NOT the file itself

        public bool IsAvailable { get; set; } = true;

        [Required]
        [Range(0.01, 9999.99)]
        [Column(TypeName = "decimal(8, 2)")]
        public decimal DailyRate { get; set; }

        [StringLength(50)]
        public string CarType { get; set; }

        [StringLength(20)]
        public string FuelType { get; set; }

        [Range(2, 12)]
        public int SeatingCapacity { get; set; }

        [StringLength(20)]
        public string Transmission { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public double? Mileage { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.Now;

        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}