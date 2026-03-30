namespace MyClinicOnline.Models
{
    public class Message
    {
        public int Id { get; set; }

        public int AppointmentId { get; set; }
        public Appointment Appointment { get; set; }

        public string Content { get; set; }
        public bool SentByDoctor { get; set; }  // true = doctor, false = patient
        public DateTime SentAt { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
    }
}