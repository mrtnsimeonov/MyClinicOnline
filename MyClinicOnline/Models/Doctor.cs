using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace MyClinicOnline.Models
{
    public class Doctor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; }

        public string Password { get; set; }

        public bool WorksWithNhif { get; set; }

        // --- City Relationship ---
        // Keeping CityId nullable for the initial migration, 
        // but adding [Required] for the form logic.
        [Required(ErrorMessage = "City selection is required.")]
        public int? CityId { get; set; }

        public City? City { get; set; }

        public bool IsApproved { get; set; } = false;

        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutUntil { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetExpiry { get; set; }

        // --- Navigation Properties (Relationships) ---

        // Many-to-Many via DoctorSpecialty join table
        public List<DoctorSpecialty> Specialties { get; set; } = new();

        // One-to-Many relationships
        public List<TimeSlot> TimeSlots { get; set; } = new();
        public List<Appointment> Appointments { get; set; } = new();
    }
}