
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

        [RegularExpression(@"^\d{9,10}$", ErrorMessage = "Phone number must be a valid number between 9 and 10 digits.")]
        public string PhoneNumber { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "NIC or Driving License No.")]
        public string IdentificationNumber { get; set; }

        public bool IsEmailVerified { get; set; } = false;

        public string? VerificationOtp { get; set; }
        public DateTime? VerificationOtpExpires { get; set; }
        public string? PasswordResetOtp { get; set; }

        public DateTime? PasswordResetOtpExpires { get; set; }

        public DateTime DateJoined { get; set; } = DateTime.Now;

            public bool IsActive { get; set; } = true;

            public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        }
    }    
