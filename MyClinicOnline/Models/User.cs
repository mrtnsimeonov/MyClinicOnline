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

        public List<Appointment> Appointments { get; set; } = new();

    }
}