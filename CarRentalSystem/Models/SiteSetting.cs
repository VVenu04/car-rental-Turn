using System.ComponentModel.DataAnnotations;

namespace CarRentalSystem.Models
{
    public class SiteSetting
    {
        [Key]
        public int SettingID { get; set; }

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string ContactEmail { get; set; }

        [Required]
        [StringLength(20)]
        public string ContactPhone { get; set; }

        [Required]
        [StringLength(250)]
        public string Address { get; set; }
    }
}