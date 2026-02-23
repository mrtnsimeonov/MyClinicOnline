namespace MyClinicOnline.Models
{
    public class MyAppointmentViewModel
    {
        public int AppointmentId { get; set; }
        public int SlotId { get; set; }
        public string DoctorName { get; set; }
        public DateTime DateTime { get; set; }
        public bool IsPast { get; set; }
    }
}