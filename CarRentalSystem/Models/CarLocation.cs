using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarRentalSystem.Models
{
    public class CarLocation
    {
        [Key]
        public int CarLocationId { get; set; }

        [Required]
        [StringLength(150)]
        public string LocationName { get; set; }

       
        [Required]
        [StringLength(20)]
        public string LocationType { get; set; } // Will be "Pickup" or "Dropoff"

        // Foreign key for Car
        public int CarId { get; set; }
        [ForeignKey("CarId")]
        public virtual Car Car { get; set; }
    }
}