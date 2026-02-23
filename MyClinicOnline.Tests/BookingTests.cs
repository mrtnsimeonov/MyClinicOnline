using Moq;
using Xunit;
using MyClinicOnline.Controllers;
using Microsoft.AspNetCore.Mvc;
using MyClinicOnline.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using MyClinicOnline.Data;
using MyClinicOnline.Services;

namespace MyClinicOnline.Tests
{
    public class BookingTests
    {
        [Fact]
        public async Task FinalizeBooking_ReturnsJsonFalse_WhenUserNotAuthenticated()
        {
            // 1. Arrange
            // We use Mock to "fake" the database context and the email service
            var mockContext = new Mock<MyClinicOnlineContext>(new Microsoft.EntityFrameworkCore.DbContextOptions<MyClinicOnlineContext>());
            var mockEmail = new Mock<IEmailService>();
            var controller = new BookingController(mockContext.Object, mockEmail.Object);

            // We simulate a user who is NOT logged in
            var user = new ClaimsPrincipal(new ClaimsIdentity());
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // 2. Act
            // Note: We are calling FinalizeBooking now, not Book
            var result = await controller.FinalizeBooking(slotId: 1);

            // 3. Assert
            // Your controller returns Json(new { success = false... }) when not authenticated
            var jsonResult = Assert.IsType<JsonResult>(result);

            // This checks that the anonymous object has success = false
            var successProperty = jsonResult.Value.GetType().GetProperty("success");
            var successValue = (bool)successProperty.GetValue(jsonResult.Value);

            Assert.False(successValue);
        }
    }
}