using MyClinicOnline.Models;

namespace MyClinicOnline.ViewModels
{
    public class MyAppointmentViewModel
    {
        public int AppointmentId { get; set; }
        public int SlotId { get; set; }
        public string DoctorName { get; set; }
        public DateTime DateTime { get; set; }
        public bool IsPast { get; set; }
        public ConsultationType ConsultationType { get; set; }
        public string? MeetingCode { get; set; }
    }
}
