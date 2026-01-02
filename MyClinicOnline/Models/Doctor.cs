using System.ComponentModel.DataAnnotations;

namespace MyClinicOnline.Models
{
    public class Doctor
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; }

        public bool WorksWithNhif { get; set; }

        // ✅ NEW: City relation
        public int CityId { get; set; }
        public City City { get; set; } = null!;

        public List<DoctorSpecialty> Specialties { get; set; } = new();
        public List<TimeSlot> TimeSlots { get; set; } = new();
        public List<Appointment> Appointments { get; set; } = new();
    }
}
