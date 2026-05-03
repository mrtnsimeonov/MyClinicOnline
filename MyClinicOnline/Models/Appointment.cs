namespace MyClinicOnline.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int TimeSlotId { get; set; }
        public TimeSlot TimeSlot { get; set; }

        public AppointmentStatus Status { get; set; }
        public ConsultationType ConsultationType { get; set; }

        public string? MeetingCode { get; set; }
    }
}
