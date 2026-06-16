using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newsletter.DTOs.Users;
using Newsletter.Models;
using Newsletter.Services;
using System.Security.Claims;

namespace Newsletter.Controllers
{
    [Authorize]
    [Route("[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {         
        private readonly IUserService _userSErvice;
        public UserController(IUserService userService)
        {
            _userSErvice = userService;
        }

        [HttpPatch("Change-Password")]
        public async Task<IActionResult> ChangePassword([FromBody] UserChangePasswordDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Sesion Invalida o expirada" });

            var (success, errorMessage) = await _userSErvice.ChangePasswordAsync(userId, dto);
            if(!success)
            {
                return BadRequest(new { message = errorMessage });
            }
            return Ok(new { message = "Contraseña actualizada correctamente" });

        }

    }
}
