using Microsoft.EntityFrameworkCore;
using MyClinicOnline.Models;

namespace MyClinicOnline.Data
{
    public class MyClinicOnlineContext : DbContext
    {
        public MyClinicOnlineContext(DbContextOptions<MyClinicOnlineContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Clinic> Clinics { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Specialty> Specialties { get; set; }
        public DbSet<DoctorSpecialty> DoctorSpecialties { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Many-to-many Doctor ↔ Specialty
            modelBuilder.Entity<DoctorSpecialty>()
                .HasKey(ds => new { ds.DoctorId, ds.SpecialtyId });

            // Enum → string
            modelBuilder.Entity<Appointment>()
                .Property(a => a.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Appointment>()
                .Property(a => a.ConsultationType)
                .HasConversion<string>();

            // IMPORTANT: avoid cascade delete cycle
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.TimeSlot)
                .WithMany()
                .HasForeignKey(a => a.TimeSlotId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
