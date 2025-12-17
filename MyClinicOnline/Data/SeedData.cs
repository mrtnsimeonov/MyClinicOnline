using MyClinicOnline.Models;

namespace MyClinicOnline.Data
{
    public static class SeedData
    {
        public static void Initialize(MyClinicOnlineContext context)
        {
            // Prevent duplicate seeding
            if (context.Doctors.Any())
                return;

            // 1️⃣ Create specialties
            var cardiology = new Specialty { Name = "Cardiology" };
            var dermatology = new Specialty { Name = "Dermatology" };
            var pediatrics = new Specialty { Name = "Pediatrics" };
            var orthopedics = new Specialty { Name = "Orthopedics" };
            var neurology = new Specialty { Name = "Neurology" };

            context.Specialties.AddRange(
                cardiology,
                dermatology,
                pediatrics,
                orthopedics,
                neurology
            );

            context.SaveChanges();

            // 2️⃣ Create doctors
            var doctors = new List<Doctor>
            {
                new Doctor
                {
                    FullName = "Dr. Maria Petrova",
                    WorksWithNhif = true
                },
                new Doctor
                {
                    FullName = "Dr. Ivan Dimitrov",
                    WorksWithNhif = false
                },
                new Doctor
                {
                    FullName = "Dr. Georgi Stoyanov",
                    WorksWithNhif = true
                },
                new Doctor
                {
                    FullName = "Dr. Elena Nikolova",
                    WorksWithNhif = true
                },
                new Doctor
                {
                    FullName = "Dr. Petar Kolev",
                    WorksWithNhif = false
                }
            };

            context.Doctors.AddRange(doctors);
            context.SaveChanges();

            // 3️⃣ Assign ONE specialty per doctor
            var doctorSpecialties = new List<DoctorSpecialty>
            {
                new DoctorSpecialty { DoctorId = doctors[0].Id, SpecialtyId = cardiology.Id },
                new DoctorSpecialty { DoctorId = doctors[1].Id, SpecialtyId = dermatology.Id },
                new DoctorSpecialty { DoctorId = doctors[2].Id, SpecialtyId = pediatrics.Id },
                new DoctorSpecialty { DoctorId = doctors[3].Id, SpecialtyId = orthopedics.Id },
                new DoctorSpecialty { DoctorId = doctors[4].Id, SpecialtyId = neurology.Id }
            };

            context.DoctorSpecialties.AddRange(doctorSpecialties);
            context.SaveChanges();
        }
    }
}
