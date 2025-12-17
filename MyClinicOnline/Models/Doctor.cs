using System.ComponentModel.DataAnnotations;

namespace MyClinicOnline.Models
{
    public class Doctor
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; }

        public bool WorksWithNhif { get; set; }

        public List<DoctorSpecialty> Specialties { get; set; } = new();
        public List<TimeSlot> TimeSlots { get; set; } = new();
        public List<Appointment> Appointments { get; set; } = new();
    }
}
