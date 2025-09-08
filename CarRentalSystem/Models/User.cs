
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace CarRentalSystem.Models
    {
        public class User
        {
            [Key]
            public int UserID { get; set; }

            [Required]
            [StringLength(50)]
            public string Username { get; set; }

            [Required]
            [StringLength(100)]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [StringLength(100, MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Required]
            public string Role { get; set; } = "Customer"; // Default role

            [StringLength(100)]
            public string FullName { get; set; }

            [Phone]
            public string PhoneNumber { get; set; }

            public DateTime DateJoined { get; set; } = DateTime.Now;

            public bool IsActive { get; set; } = true;

            public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        }
    }    
