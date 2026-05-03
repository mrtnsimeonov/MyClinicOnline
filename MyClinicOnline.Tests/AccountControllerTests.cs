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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace MyClinicOnline.Tests
{
    public class AccountControllerTests
    {
        private static MyClinicOnlineContext CreateContext() =>
            new MyClinicOnlineContext(
                new DbContextOptionsBuilder<MyClinicOnlineContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

        private static AccountController CreateController(MyClinicOnlineContext context)
        {
            var mockEmail = new Mock<IEmailService>();
            var controller = new AccountController(context, mockEmail.Object);

            var authService = new Mock<IAuthenticationService>();
            authService
                .Setup(s => s.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(),
                    It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties?>()))
                .Returns(Task.CompletedTask);

            var urlHelperFactory = new Mock<IUrlHelperFactory>();
            urlHelperFactory
                .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(new Mock<IUrlHelper>().Object);

            var tempDataFactory = new Mock<ITempDataDictionaryFactory>();
            tempDataFactory
                .Setup(f => f.GetTempData(It.IsAny<HttpContext>()))
                .Returns(new Mock<ITempDataDictionary>().Object);

            var services = new Mock<IServiceProvider>();
            services.Setup(s => s.GetService(typeof(IAuthenticationService))).Returns(authService.Object);
            services.Setup(s => s.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactory.Object);
            services.Setup(s => s.GetService(typeof(ITempDataDictionaryFactory))).Returns(tempDataFactory.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = services.Object }
            };

            return controller;
        }

        private static User MakeUser(string email, string password, bool isAdmin = false) => new User
        {
            FirstName = "Test", LastName = "User", Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            Phone = "0000", Region = "Sofia",
            DateOfBirth = new DateTime(1990, 1, 1), Gender = "Male",
            IsAdmin = isAdmin
        };

        [Fact]
        public async Task Login_ReturnsError_WhenEmailNotFound()
        {
            using var context = CreateContext();
            var controller = CreateController(context);

            var result = await controller.Login("nobody@test.com", "pass");

            Assert.IsType<ViewResult>(result);
            Assert.Equal("Грешен имейл или парола", controller.ViewBag.Error);
        }

        [Fact]
        public async Task Login_ReturnsError_WhenPasswordIsWrong()
        {
            using var context = CreateContext();
            context.Users.Add(MakeUser("user@test.com", "correctpass"));
            await context.SaveChangesAsync();

            var controller = CreateController(context);
            var result = await controller.Login("user@test.com", "wrongpass");

            Assert.IsType<ViewResult>(result);
            Assert.Equal("Грешен имейл или парола", controller.ViewBag.Error);
        }

        [Fact]
        public async Task Login_RedirectsToAdminDashboard_WhenAdminCredentialsCorrect()
        {
            using var context = CreateContext();
            context.Users.Add(MakeUser("admin@test.com", "Admin123!", isAdmin: true));
            await context.SaveChangesAsync();

            var controller = CreateController(context);
            var result = await controller.Login("admin@test.com", "Admin123!");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
        }

        [Fact]
        public async Task Login_RedirectsToHome_WhenPatientCredentialsCorrect()
        {
            using var context = CreateContext();
            context.Users.Add(MakeUser("patient@test.com", "pass123"));
            await context.SaveChangesAsync();

            var controller = CreateController(context);
            var result = await controller.Login("patient@test.com", "pass123");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Home", redirect.ControllerName);
        }

        [Fact]
        public async Task Login_ReturnsError_WhenDoctorAccountPendingApproval()
        {
            using var context = CreateContext();
            var city = new City { Name = "Sofia" };
            context.Cities.Add(city);
            await context.SaveChangesAsync();

            context.Doctors.Add(new Doctor
            {
                FullName = "Dr. Pending", Email = "pending@test.com",
                Password = BCrypt.Net.BCrypt.HashPassword("docpass"),
                IsApproved = false, CityId = city.Id
            });
            await context.SaveChangesAsync();

            var controller = CreateController(context);
            var result = await controller.Login("pending@test.com", "docpass");

            Assert.IsType<ViewResult>(result);
            Assert.Equal("Your account is pending admin approval. You will be notified by email.", controller.ViewBag.Error);
        }

        [Fact]
        public async Task Login_RedirectsToHome_WhenApprovedDoctorLogsIn()
        {
            using var context = CreateContext();
            var city = new City { Name = "Sofia" };
            context.Cities.Add(city);
            await context.SaveChangesAsync();

            context.Doctors.Add(new Doctor
            {
                FullName = "Dr. Approved", Email = "approved@test.com",
                Password = BCrypt.Net.BCrypt.HashPassword("docpass"),
                IsApproved = true, CityId = city.Id
            });
            await context.SaveChangesAsync();

            var controller = CreateController(context);
            var result = await controller.Login("approved@test.com", "docpass");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Home", redirect.ControllerName);
        }

        [Fact]
        public async Task Login_RehashesPlainTextPassword_OnSuccessfulLogin()
        {
            using var context = CreateContext();
            context.Users.Add(new User
            {
                FirstName = "Legacy", LastName = "User", Email = "legacy@test.com",
                Password = "plaintext123",
                Phone = "0000", Region = "Sofia",
                DateOfBirth = new DateTime(1990, 1, 1), Gender = "Male"
            });
            await context.SaveChangesAsync();

            var controller = CreateController(context);
            await controller.Login("legacy@test.com", "plaintext123");

            var user = context.Users.First(u => u.Email == "legacy@test.com");
            Assert.StartsWith("$2", user.Password);
        }
    }
}
