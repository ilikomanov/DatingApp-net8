using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.Tests.TestHelpers
{
    public static class ControllerTestExtensions
    {
        public static void SetupUserId(this ControllerBase controller, int userId)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };
        }
    }
}
