using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Newsletter.Data;
using Newsletter.DTOs.Users;
using Newsletter.Models;
using Newsletter.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Newsletter.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(UserRegisterDto dto)
        {
            var result = await _userService.RegisterAsync(dto);
            if (result.success)
            {
                return Ok("Usuario creado. Revisá tu correo para confirmar la cuenta.");
            }
            return BadRequest(result.Errors);
     
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(UserLoginDto dto)
        {
            var result = await _userService.LoginAsync(dto);

            if (result.EmailNotConfirmed)
            {
                return StatusCode(StatusCodes.Status403Forbidden, result.ErrorMessage);
            }
            if(!result.success)
            {
                return BadRequest(result.ErrorMessage);
            }
            return Ok(new
            {
                mensaje = "Login exitoso",
                token = result.Token
            });
        }

        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery]string token)
        {
           var result = await _userService.ConfirmEmailAsync(userId, token);
            if (result.success)
            {
                return Ok( result.message);
            }
            return BadRequest( result.message);
        }

        [HttpPost("ResendConfirmation")]
        [EnableRateLimiting("EmailSending")]
        public async Task<IActionResult> ResendConfirmation(ResendConfirmationDto dto)
        {
            await _userService.ResendConfirmationEmailAsync(dto.Email);
            return Ok("Si el email está registrado y sin confirmar, te enviamos un nuevo enlace.");
        }

        
       [Authorize(Roles = "Admin")]
        [HttpPost("AssignRole")]
        public async Task<IActionResult> AssignRole(UserRoleDto dto)
        {
            var result = await _userService.AssignRoleAsycn(dto);

            if (result.success)
            {
                return Ok("Rol asignado correctamente");
            }
            return BadRequest(result.Errors);
        }       

        [HttpPost("ForgotPassword")]
        [EnableRateLimiting("EmailSending")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            await _userService.ForgotPasswordAsync(dto.Email);
            return Ok("Si el email está registrado, te enviamos un enlace para restablecer tu contraseña.");
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            var result = await _userService.ResetPasswordAsync(dto);
            if (result.success)
                return Ok("Tu contraseña se cambió correctamente. Ya podés iniciar sesión.");
            return BadRequest(new { invalidLink = result.InvalidLink, errors = result.Errors });
        }
    }
}
