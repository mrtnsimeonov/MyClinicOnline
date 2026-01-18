using System.ComponentModel.DataAnnotations;

namespace MyClinicOnline.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Името е задължително")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Фамилията е задължителна")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Телефонът е задължителен")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Изберете област")]
        public string Region { get; set; }

        [Required(ErrorMessage = "Изберете пол")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Датата на раждане е задължителна")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Имейлът е задължителен")]
        [EmailAddress(ErrorMessage = "Невалиден имейл")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Паролата е задължителна")]
        [MinLength(8, ErrorMessage = "Паролата трябва да е поне 8 символа")]
        public string Password { get; set; }
    }
}