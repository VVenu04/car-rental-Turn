
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarRentalSystem.Models
{
    public class Booking
    {
        [Key]
        public int BookingID { get; set; }

        [Required]
        public int CustomerID { get; set; }

        [Required]
        public int CarID { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime PickupDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ReturnDate { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal TotalCost { get; set; }

        [StringLength(20)]
        public string BookingStatus { get; set; } = "Pending"; 

        [StringLength(20)]
        public string PaymentStatus { get; set; } = "Pending"; 

        [StringLength(100)]
        public string? TransactionId { get; set; }

        [StringLength(20)]
        public string? PaymentMethod { get; set; }

        public DateTime BookingDate { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? SpecialRequirements { get; set; }

        [StringLength(200)]
        public string PickupLocation { get; set; }

        [StringLength(200)]
        public string ReturnLocation { get; set; }

        // Navigation properties
        [ForeignKey("CustomerID")]
        public virtual User Customer { get; set; }

        [ForeignKey("CarID")]
        public virtual Car Car { get; set; }

        [NotMapped]
        public int TotalDays => (ReturnDate - PickupDate).Days + 1;
    }
}
