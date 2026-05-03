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
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

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

            var urlHelperFactory = new Mock<IUrlHelperFactory>();
            urlHelperFactory
                .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(new Mock<IUrlHelper>().Object);

            var tempDataFactory = new Mock<ITempDataDictionaryFactory>();
            tempDataFactory
                .Setup(f => f.GetTempData(It.IsAny<HttpContext>()))
                .Returns(new Mock<ITempDataDictionary>().Object);

            var services = new Mock<IServiceProvider>();
            services.Setup(s => s.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactory.Object);
            services.Setup(s => s.GetService(typeof(ITempDataDictionaryFactory))).Returns(tempDataFactory.Object);

            var claims = new[] { new Claim(ClaimTypes.Role, "Admin") };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "MyCookieAuth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal,
                    RequestServices = services.Object
                }
            };
            return controller;
        }

        [Fact]
        public async Task ApproveDoctor_SetsIsApprovedTrue_AndSendsEmail()
        {
            using var context = CreateContext();
            var mockEmail = new Mock<IEmailService>();

            var city = new City { Name = "Sofia" };
            context.Cities.Add(city);
            context.Doctors.Add(new Doctor
            {
                FullName = "Dr. Pending", Email = "pending@test.com",
                Password = "hash", IsApproved = false, CityId = city.Id
            });
            await context.SaveChangesAsync();

            var doctorId = context.Doctors.First().Id;
            await CreateController(context, mockEmail.Object).ApproveDoctor(doctorId);

            Assert.True(context.Doctors.Find(doctorId)!.IsApproved);
            mockEmail.Verify(e => e.SendEmailAsync(
                "pending@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task RejectDoctor_DeletesDoctorRecord_AndSendsEmail()
        {
            using var context = CreateContext();
            var mockEmail = new Mock<IEmailService>();

            var city = new City { Name = "Sofia" };
            context.Cities.Add(city);
            context.Doctors.Add(new Doctor
            {
                FullName = "Dr. Rejected", Email = "rejected@test.com",
                Password = "hash", IsApproved = false, CityId = city.Id
            });
            await context.SaveChangesAsync();

            var doctorId = context.Doctors.First().Id;
            await CreateController(context, mockEmail.Object).RejectDoctor(doctorId);

            Assert.False(context.Doctors.Any(d => d.Id == doctorId));
            mockEmail.Verify(e => e.SendEmailAsync(
                "rejected@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task AddSpecialty_AddsNewSpecialty_WhenNameDoesNotExist()
        {
            using var context = CreateContext();

            await CreateController(context).AddSpecialty("Ophthalmology");

            Assert.True(context.Specialties.Any(s => s.Name == "Ophthalmology"));
        }

        [Fact]
        public async Task AddSpecialty_DoesNotAddDuplicate_WhenNameAlreadyExists()
        {
            using var context = CreateContext();
            context.Specialties.Add(new Specialty { Name = "Cardiology" });
            await context.SaveChangesAsync();

            await CreateController(context).AddSpecialty("Cardiology");

            Assert.Equal(1, context.Specialties.Count(s => s.Name == "Cardiology"));
        }

        [Fact]
        public async Task AddCity_AddsNewCity_WhenNameDoesNotExist()
        {
            using var context = CreateContext();

            await CreateController(context).AddCity("Ruse");

            Assert.True(context.Cities.Any(c => c.Name == "Ruse"));
        }

        [Fact]
        public async Task AddCity_DoesNotAddDuplicate_WhenNameAlreadyExists()
        {
            using var context = CreateContext();
            context.Cities.Add(new City { Name = "Sofia" });
            await context.SaveChangesAsync();

            await CreateController(context).AddCity("Sofia");

            Assert.Equal(1, context.Cities.Count(c => c.Name == "Sofia"));
        }
    }
}
