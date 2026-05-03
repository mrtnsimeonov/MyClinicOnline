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

namespace MyClinicOnline.Tests
{
    public class AdminControllerTests
    {
        private static MyClinicOnlineContext CreateContext() =>
            new MyClinicOnlineContext(
                new DbContextOptionsBuilder<MyClinicOnlineContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

        private static AdminController CreateController(MyClinicOnlineContext context, IEmailService? emailService = null)
        {
            var mockEmail = emailService ?? new Mock<IEmailService>().Object;
            var controller = new AdminController(context, mockEmail);

            var claims = new[] { new Claim(ClaimTypes.Role, "Admin") };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "MyCookieAuth"));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
            return controller;
        }

        [Fact]
        public async Task ApproveDoctor_SetsIsApprovedTrue_AndSendsEmail()
        {
            using var context = CreateContext();
            var mockEmail = new Mock<IEmailService>();
            context.Doctors.Add(new Doctor
            {
                FullName = "Dr. Pending", Email = "pending@test.com",
                IsApproved = false
            });
            await context.SaveChangesAsync();

            var doctorId = context.Doctors.First().Id;
            var controller = CreateController(context, mockEmail.Object);

            await controller.ApproveDoctor(doctorId);

            Assert.True(context.Doctors.Find(doctorId)!.IsApproved);
            mockEmail.Verify(e => e.SendEmailAsync(
                "pending@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task RejectDoctor_DeletesDoctorRecord_AndSendsEmail()
        {
            using var context = CreateContext();
            var mockEmail = new Mock<IEmailService>();
            context.Doctors.Add(new Doctor
            {
                FullName = "Dr. Rejected", Email = "rejected@test.com",
                IsApproved = false
            });
            await context.SaveChangesAsync();

            var doctorId = context.Doctors.First().Id;
            var controller = CreateController(context, mockEmail.Object);

            await controller.RejectDoctor(doctorId);

            Assert.False(context.Doctors.Any(d => d.Id == doctorId));
            mockEmail.Verify(e => e.SendEmailAsync(
                "rejected@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task AddSpecialty_AddsNewSpecialty_WhenNameDoesNotExist()
        {
            using var context = CreateContext();
            var controller = CreateController(context);

            await controller.AddSpecialty("Ophthalmology");

            Assert.True(context.Specialties.Any(s => s.Name == "Ophthalmology"));
        }

        [Fact]
        public async Task AddSpecialty_DoesNotAddDuplicate_WhenNameAlreadyExists()
        {
            using var context = CreateContext();
            context.Specialties.Add(new Specialty { Name = "Cardiology" });
            await context.SaveChangesAsync();

            var controller = CreateController(context);
            await controller.AddSpecialty("Cardiology");

            Assert.Equal(1, context.Specialties.Count(s => s.Name == "Cardiology"));
        }

        [Fact]
        public async Task AddCity_AddsNewCity_WhenNameDoesNotExist()
        {
            using var context = CreateContext();
            var controller = CreateController(context);

            await controller.AddCity("Ruse");

            Assert.True(context.Cities.Any(c => c.Name == "Ruse"));
        }

        [Fact]
        public async Task AddCity_DoesNotAddDuplicate_WhenNameAlreadyExists()
        {
            using var context = CreateContext();
            context.Cities.Add(new City { Name = "Sofia" });
            await context.SaveChangesAsync();

            var controller = CreateController(context);
            await controller.AddCity("Sofia");

            Assert.Equal(1, context.Cities.Count(c => c.Name == "Sofia"));
        }
    }
}
