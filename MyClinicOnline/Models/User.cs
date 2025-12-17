using System.ComponentModel.DataAnnotations;

namespace MyClinicOnline.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; }

        public string Email { get; set; }

        public List<Appointment> Appointments { get; set; } = new();
    }
}
