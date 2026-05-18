using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Unisonmusic.Api.Extensions
{
    public static class ControllerBaseExtensions
    {
        public static int? GetCurrentAccountId(this ControllerBase apiController)
        {
            var identifier = apiController.User.FindFirst(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(identifier?.Value))
            {
                return null;
            }

            if (int.TryParse(identifier.Value, out var userId))
            {
                return userId;
            }

            return null;
        }
    }
}
