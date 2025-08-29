using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;

namespace DatingApp.Tests.TestHelpers
{
    public static class TestUserHelpers
    {
        public static ClaimsPrincipal CreateTestUserClaimsPrincipal(int userId)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            return new ClaimsPrincipal(identity);
        }
    }
}
