using System.ComponentModel.DataAnnotations;

namespace MyClinicOnline.ViewModels
{
    public class RegisterDoctorViewModel
    {
        [Required(ErrorMessage = "Full Name is required")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool WorksWithNhif { get; set; }

        [Required(ErrorMessage = "Please select a city")]
        public int CityId { get; set; }

        [Required(ErrorMessage = "Select at least one specialty")]
        public List<int> SelectedSpecialtyIds { get; set; } = new();
    }
}
