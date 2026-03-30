namespace MyClinicOnline.Models
{
    public class AppointmentNote
    {
        public int Id { get; set; }

        public int AppointmentId { get; set; }
        public Appointment Appointment { get; set; }

        // Patient fills this right after booking
        public string? MainComplaint { get; set; }
        public string? Symptoms { get; set; }
        public string? SymptomDuration { get; set; }
        public string? AdditionalPatientNotes { get; set; }

        // Doctor fills this after the consultation
        public string? Diagnosis { get; set; }
        public string? Prescription { get; set; }
        public string? DoctorNotes { get; set; }
        public string? NextSteps { get; set; }
        public DateTime? DoctorNotesAddedAt { get; set; }
    }
}