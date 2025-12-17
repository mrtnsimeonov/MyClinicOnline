namespace MyClinicOnline.Models
{
    public class Specialty
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public List<DoctorSpecialty> Doctors { get; set; } = new();
    }
}
