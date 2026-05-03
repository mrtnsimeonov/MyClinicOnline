# MyClinicOnline

A full-stack web application for booking medical appointments online, built with ASP.NET Core 8 MVC. Patients can search for doctors and book in-person or video consultations. Doctors manage their schedule and join video calls directly from the platform. Admins oversee all users, appointments, and platform data.

**Live site:** https://mycliniconline-ercfbcbrgdcsgjcw.westeurope-01.azurewebsites.net

---

## Features

### Patients
- Register and log in securely
- Search for doctors by specialty and city
- Book in-person or online (video) appointments
- Fill in a symptom form before each appointment
- View upcoming and past appointments
- Join video consultations via a unique meeting code
- Message doctors through the appointment chat
- Cancel appointments

### Doctors
- Register and await admin approval before logging in
- View their time slot calendar and booked appointments
- Add medical notes, diagnosis, and prescription after each consultation
- Join online appointments with in-browser video (Jitsi Meet)
- Receive toast notifications 5 minutes before an online appointment starts
- Cancel bookings (patient is notified by email)

### Admin
- Dashboard with platform statistics (patients, doctors, appointments, online vs in-person breakdown)
- Approve or reject doctor registrations (doctor notified by email)
- Delete patients and doctors
- Manage specialties and cities
- View all appointments system-wide

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core 8.0 MVC |
| ORM | Entity Framework Core 8.0 |
| Database | Azure SQL Server |
| Authentication | Cookie-based auth (custom, BCrypt password hashing) |
| Video calls | Jitsi Meet (browser-based, no install required) |
| Frontend | Bootstrap 5, Bootstrap Icons |
| Email | SMTP via custom `IEmailService` |
| Testing | xUnit, Moq, EF Core InMemory |
| Hosting | Azure App Service (West Europe) |

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (local) or an Azure SQL database
- A valid SMTP account for email sending (optional for local dev)

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/mrtnsimeonov/MyClinicOnline.git
   cd MyClinicOnline
   ```

2. Update the connection string in `MyClinicOnline/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER;Database=MyClinicDB;User ID=...;Password=...;"
   }
   ```

3. Apply migrations and seed the database — this happens automatically on first startup via `context.Database.Migrate()`.

4. Run the application:
   ```bash
   cd MyClinicOnline
   dotnet run
   ```

The app seeds an admin account and sample doctors automatically on first launch.

---

## Default Admin Account

| Field | Value |
|-------|-------|
| Email | `admin@myclinic.com` |
| Password | `Admin123!` |

> The password is stored as a BCrypt hash. On first login the hash is generated automatically by the seed process.

---

## User Roles

| Role | How to get it |
|------|--------------|
| **Patient** | Self-register via the Register page |
| **Doctor** | Register via Register as Doctor — requires admin approval |
| **Admin** | Seeded automatically; only one admin account |

---

## Online Appointments

When a patient books an **Online** consultation:
- An 8-character cryptographically secure meeting code is generated
- The code is emailed to the patient with a direct join link
- Both the patient and doctor see a **Join Meeting** button in their appointment list, active from 10 minutes before until 60 minutes after the start time
- The video room is powered by Jitsi Meet — no plugin or account needed

---

## Project Structure

```
MyClinicOnline/
├── Controllers/         # MVC controllers (Account, Admin, Booking, Calendar, Doctor, Video, ...)
├── Data/
│   ├── MyClinicOnlineContext.cs
│   └── SeedData.cs
├── Migrations/          # EF Core migrations
├── Models/              # Entity and view models
├── Services/            # IEmailService
├── Views/               # Razor views per controller
└── wwwroot/             # Static assets

MyClinicOnline.Tests/
├── AccountControllerTests.cs
├── AdminControllerTests.cs
├── BookingTests.cs
├── CalendarControllerTests.cs
└── VideoControllerTests.cs
```

---

## Running the Tests

```bash
cd MyClinicOnline.Tests
dotnet test
```

25 tests covering login routing, BCrypt migration, online/in-person booking logic, video call access control, and admin operations.

---

## Deployment

The app is deployed to **Azure App Service** using Visual Studio's Web Deploy publish profile.

On every startup:
1. `context.Database.Migrate()` applies any pending EF Core migrations to Azure SQL
2. `SeedData.Initialize()` ensures the admin account and sample doctors exist

To redeploy after changes: right-click the project in Visual Studio → **Publish** → **Publish**.

---

## Security Notes

- Passwords are hashed with **BCrypt** (work factor 11). Plain-text legacy passwords are auto-migrated to BCrypt on the user's next login.
- Login is rate-limited to **10 attempts per minute** per IP.
- Video meeting rooms are validated — only the patient and doctor of a given appointment can join.
- All admin routes are protected with `[Authorize(Roles = "Admin")]`.
- Doctor routes are protected with `[Authorize(Roles = "Doctor")]`.

---

## License

This project is for educational purposes.
