using Moq;
using Xunit;
using MyClinicOnline.Controllers;
using Microsoft.AspNetCore.Mvc;
using MyClinicOnline.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using MyClinicOnline.Data;
using MyClinicOnline.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Routing;

namespace MyClinicOnline.Tests
{
    public class CalendarControllerTests
    {
        private static MyClinicOnlineContext CreateContext() =>
            new MyClinicOnlineContext(
                new DbContextOptionsBuilder<MyClinicOnlineContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

        private static CalendarController CreateController(MyClinicOnlineContext context, int? userId = null)
        {
            var mockEmail = new Mock<IEmailService>();
            var controller = new CalendarController(context, mockEmail.Object);

            var urlHelperFactory = new Mock<IUrlHelperFactory>();
            urlHelperFactory
                .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(new Mock<IUrlHelper>().Object);

            var services = new Mock<IServiceProvider>();
            services.Setup(s => s.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactory.Object);

            var claims = new List<Claim>();
            if (userId.HasValue)
                claims.Add(new Claim("UserId", userId.Value.ToString()));

            var principal = new ClaimsPrincipal(
                new ClaimsIdentity(claims, userId.HasValue ? "MyCookieAuth" : ""));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal,
                    RequestServices = services.Object
                }
            };
            controller.TempData = new Mock<ITempDataDictionary>().Object;

            return controller;
        }

        private static (Doctor doctor, TimeSlot slot, User user) SeedBookingData(MyClinicOnlineContext context)
        {
            var city = new City { Name = "Sofia" };
            context.Cities.Add(city);
            context.SaveChanges();

            var doctor = new Doctor
            {
                FullName = "Dr. Test", Email = "doc@test.com",
                Password = "hash", CityId = city.Id, IsApproved = true
            };
            context.Doctors.Add(doctor);
            context.SaveChanges();

            var slot = new TimeSlot
            {
                DoctorId = doctor.Id,
                StartTime = DateTime.Now.AddDays(1),
                EndTime = DateTime.Now.AddDays(1).AddHours(1),
                IsBooked = false
            };
            context.TimeSlots.Add(slot);

            var user = new User
            {
                FirstName = "John", LastName = "Doe", Email = "john@test.com",
                Password = "hash", Phone = "0000", Region = "Sofia",
                DateOfBirth = new DateTime(1990, 1, 1), Gender = "Male"
            };
            context.Users.Add(user);
            context.SaveChanges();

            return (doctor, slot, user);
        }

        [Fact]
        public async Task Book_CreatesAppointmentWithMeetingCode_WhenOnline()
        {
            using var context = CreateContext();
            var (_, slot, user) = SeedBookingData(context);
            var controller = CreateController(context, userId: user.Id);

            await controller.Book(slot.Id, "Online");

            var appointment = context.Appointments.Single();
            Assert.Equal(ConsultationType.Online, appointment.ConsultationType);
            Assert.NotNull(appointment.MeetingCode);
            Assert.Equal(8, appointment.MeetingCode!.Length);
        }

        [Fact]
        public async Task Book_CreatesAppointmentWithNoMeetingCode_WhenInPerson()
        {
            using var context = CreateContext();
            var (_, slot, user) = SeedBookingData(context);
            var controller = CreateController(context, userId: user.Id);

            await controller.Book(slot.Id, "InPerson");

            var appointment = context.Appointments.Single();
            Assert.Equal(ConsultationType.InPerson, appointment.ConsultationType);
            Assert.Null(appointment.MeetingCode);
        }

        [Fact]
        public async Task Book_MarksSlotAsBooked_AfterSuccessfulBooking()
        {
            using var context = CreateContext();
            var (_, slot, user) = SeedBookingData(context);
            var controller = CreateController(context, userId: user.Id);

            await controller.Book(slot.Id, "InPerson");

            Assert.True(context.TimeSlots.Find(slot.Id)!.IsBooked);
        }

        [Fact]
        public async Task Book_RedirectsToLogin_WhenUserNotAuthenticated()
        {
            using var context = CreateContext();
            var (_, slot, _) = SeedBookingData(context);
            var controller = CreateController(context, userId: null);

            var result = await controller.Book(slot.Id, "InPerson");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Account", redirect.ControllerName);
        }

        [Fact]
        public async Task Book_RedirectsWithError_WhenSlotAlreadyBooked()
        {
            using var context = CreateContext();
            var (_, slot, user) = SeedBookingData(context);
            slot.IsBooked = true;
            await context.SaveChangesAsync();

            var controller = CreateController(context, userId: user.Id);
            var result = await controller.Book(slot.Id, "InPerson");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }
    }
}
