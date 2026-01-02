using MyClinicOnline.Models;

namespace MyClinicOnline.Data
{
    public static class SeedData
    {
        public static void Initialize(MyClinicOnlineContext context)
        {
            // 1) Seed Cities (if missing)
            if (!context.Cities.Any())
            {
                context.Cities.AddRange(
                    new City { Name = "Sofia" },
                    new City { Name = "Plovdiv" },
                    new City { Name = "Varna" },
                    new City { Name = "Burgas" }
                );
                context.SaveChanges();
            }

            // 2) Seed Specialties (if missing)
            if (!context.Specialties.Any())
            {
                context.Specialties.AddRange(
                    new Specialty { Name = "Cardiology" },
                    new Specialty { Name = "Dermatology" },
                    new Specialty { Name = "Pediatrics" },
                    new Specialty { Name = "Orthopedics" },
                    new Specialty { Name = "Neurology" }
                );
                context.SaveChanges();
            }

            // Helpers
            var sofia = context.Cities.First(c => c.Name == "Sofia");
            var plovdiv = context.Cities.First(c => c.Name == "Plovdiv");
            var varna = context.Cities.First(c => c.Name == "Varna");
            var burgas = context.Cities.First(c => c.Name == "Burgas");

            var cardiology = context.Specialties.First(s => s.Name == "Cardiology");
            var dermatology = context.Specialties.First(s => s.Name == "Dermatology");
            var pediatrics = context.Specialties.First(s => s.Name == "Pediatrics");
            var orthopedics = context.Specialties.First(s => s.Name == "Orthopedics");
            var neurology = context.Specialties.First(s => s.Name == "Neurology");

            // 3) Backfill CityId for existing doctors (if null)
            var existingWithoutCity = context.Doctors.Where(d => d.CityId == null).ToList();
            if (existingWithoutCity.Any())
            {
                foreach (var d in existingWithoutCity)
                    d.CityId = sofia.Id; // default city

                context.SaveChanges();
            }

            // 4) Add new doctors only if they don't already exist (by FullName)
            var toAdd = new List<Doctor>();

            void AddDoctorIfMissing(string fullName, bool worksWithNhif, int cityId)
            {
                bool exists = context.Doctors.Any(d => d.FullName == fullName);
                if (!exists)
                {
                    toAdd.Add(new Doctor
                    {
                        FullName = fullName,
                        WorksWithNhif = worksWithNhif,
                        CityId = cityId
                    });
                }
            }

            AddDoctorIfMissing("Dr. Maria Petrova", true, sofia.Id);
            AddDoctorIfMissing("Dr. Ivan Dimitrov", false, sofia.Id);
            AddDoctorIfMissing("Dr. Georgi Stoyanov", true, plovdiv.Id);
            AddDoctorIfMissing("Dr. Elena Nikolova", true, varna.Id);
            AddDoctorIfMissing("Dr. Petar Kolev", false, plovdiv.Id);

            // extra doctors (make DB bigger)
            AddDoctorIfMissing("Dr. Daniela Hristova", true, burgas.Id);
            AddDoctorIfMissing("Dr. Stefan Marinov", false, varna.Id);
            AddDoctorIfMissing("Dr. Nikolay Andreev", true, sofia.Id);
            AddDoctorIfMissing("Dr. Ralitsa Georgieva", true, plovdiv.Id);
            AddDoctorIfMissing("Dr. Hristo Petkov", false, burgas.Id);

            if (toAdd.Any())
            {
                context.Doctors.AddRange(toAdd);
                context.SaveChanges();
            }

            // 5) Ensure DoctorSpecialties exist (no duplicates)
            void EnsureDoctorSpecialty(string doctorName, int specialtyId)
            {
                var doctor = context.Doctors.FirstOrDefault(d => d.FullName == doctorName);
                if (doctor == null) return;

                bool linkExists = context.DoctorSpecialties.Any(ds =>
                    ds.DoctorId == doctor.Id && ds.SpecialtyId == specialtyId);

                if (!linkExists)
                {
                    context.DoctorSpecialties.Add(new DoctorSpecialty
                    {
                        DoctorId = doctor.Id,
                        SpecialtyId = specialtyId
                    });
                }
            }

            // existing 5
            EnsureDoctorSpecialty("Dr. Maria Petrova", cardiology.Id);
            EnsureDoctorSpecialty("Dr. Ivan Dimitrov", dermatology.Id);
            EnsureDoctorSpecialty("Dr. Georgi Stoyanov", pediatrics.Id);
            EnsureDoctorSpecialty("Dr. Elena Nikolova", orthopedics.Id);
            EnsureDoctorSpecialty("Dr. Petar Kolev", neurology.Id);

            // new ones
            EnsureDoctorSpecialty("Dr. Daniela Hristova", dermatology.Id);
            EnsureDoctorSpecialty("Dr. Stefan Marinov", cardiology.Id);
            EnsureDoctorSpecialty("Dr. Nikolay Andreev", neurology.Id);
            EnsureDoctorSpecialty("Dr. Ralitsa Georgieva", pediatrics.Id);
            EnsureDoctorSpecialty("Dr. Hristo Petkov", orthopedics.Id);

            context.SaveChanges();
        }
    }
}
