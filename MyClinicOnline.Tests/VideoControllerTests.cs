using Moq;
using Xunit;
using MyClinicOnline.Controllers;
using Microsoft.AspNetCore.Mvc;
using MyClinicOnline.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using MyClinicOnline.Data;
using Microsoft.EntityFrameworkCore;

namespace MyClinicOnline.Tests
{
    public class VideoControllerTests
    {
        private static MyClinicOnlineContext CreateContext() =>
            new MyClinicOnlineContext(
                new DbContextOptionsBuilder<MyClinicOnlineContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

        private static VideoController CreateController(MyClinicOnlineContext context, int userId, string role = "Patient")
        {
            var claims = new List<Claim>
            {
                new Claim("UserId", userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "MyCookieAuth"));

            var controller = new VideoController(context);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
            return controller;
        }

        private static (Appointment appointment, User user, Doctor doctor) SeedAppointment(
            MyClinicOnlineContext context, DateTime startTime, string meetingCode)
        {
            var city = new City { Name = "Sofia" };
            context.Cities.Add(city);

            var doctor = new Doctor { FullName = "Dr. Test", Email = "doc@test.com", CityId = city.Id };
            context.Doctors.Add(doctor);

            var user = new User
            {
                FirstName = "John", LastName = "Doe", Email = "john@test.com",
                Password = "hash", Phone = "0000", Region = "Sofia",
                DateOfBirth = new DateTime(1990, 1, 1), Gender = "Male"
            };
            context.Users.Add(user);
            context.SaveChanges();

            var slot = new TimeSlot
            {
                DoctorId = doctor.Id, StartTime = startTime,
                EndTime = startTime.AddHours(1), IsBooked = true
            };
            context.TimeSlots.Add(slot);
            context.SaveChanges();

            var appointment = new Appointment
            {
                DoctorId = doctor.Id, UserId = user.Id, TimeSlotId = slot.Id,
                MeetingCode = meetingCode, ConsultationType = ConsultationType.Online,
                Status = AppointmentStatus.Confirmed
            };
            context.Appointments.Add(appointment);
            context.SaveChanges();

            return (appointment, user, doctor);
        }

        [Fact]
        public async Task Join_ReturnsEnterCodeView_WhenCodeIsNull()
        {
            using var context = CreateContext();
            var controller = CreateController(context, userId: 1);

            var result = await controller.Join(null);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("EnterCode", view.ViewName);
        }

        [Fact]
        public async Task Join_ReturnsErrorOnEnterCode_WhenCodeNotFound()
        {
            using var context = CreateContext();
            var controller = CreateController(context, userId: 1);

            var result = await controller.Join("BADCODE1");

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("EnterCode", view.ViewName);
            Assert.NotNull(controller.ViewBag.Error);
        }

        [Fact]
        public async Task Join_ReturnsError_WhenPatientTriesToJoinOtherAppointment()
        {
            using var context = CreateContext();
            SeedAppointment(context, DateTime.Now, "VALIDCOD");
            var controller = CreateController(context, userId: 9999, role: "Patient");

            var result = await controller.Join("VALIDCOD");

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("EnterCode", view.ViewName);
            Assert.NotNull(controller.ViewBag.Error);
        }

        [Fact]
        public async Task Join_ReturnsError_WhenTooEarlyToJoin()
        {
            using var context = CreateContext();
            // Start is 30 min away — earlier than the allowed -10 min window
            var (_, user, _) = SeedAppointment(context, DateTime.Now.AddMinutes(30), "EARLY001");
            var controller = CreateController(context, userId: user.Id, role: "Patient");

            var result = await controller.Join("EARLY001");

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("EnterCode", view.ViewName);
            Assert.NotNull(controller.ViewBag.Error);
        }

        [Fact]
        public async Task Join_ReturnsError_WhenAppointmentExpired()
        {
            using var context = CreateContext();
            // Start was 90 min ago — past the 60 min post-start window
            var (_, user, _) = SeedAppointment(context, DateTime.Now.AddMinutes(-90), "EXPIRD01");
            var controller = CreateController(context, userId: user.Id, role: "Patient");

            var result = await controller.Join("EXPIRD01");

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("EnterCode", view.ViewName);
            Assert.NotNull(controller.ViewBag.Error);
        }

        [Fact]
        public async Task Join_ReturnsVideoCall_WhenCodeValidAndOwner()
        {
            using var context = CreateContext();
            // Start = now, well within the -10/+60 window
            var (_, user, _) = SeedAppointment(context, DateTime.Now, "VALID001");
            var controller = CreateController(context, userId: user.Id, role: "Patient");

            var result = await controller.Join("VALID001");

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("VideoCall", view.ViewName);
        }
    }
}
