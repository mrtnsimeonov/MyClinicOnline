using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyClinicOnline.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Phone { get; set; }

        [Required]
        public string Region { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Gender { get; set; }

        public bool IsAdmin { get; set; } = false;

        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutUntil { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetExpiry { get; set; }

        public List<Appointment> Appointments { get; set; } = new();

    }
}