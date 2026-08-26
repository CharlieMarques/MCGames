using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
                return Ok("Usuario creado con éxito");
            }
            return BadRequest(result.Errors);
     
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(UserLoginDto dto)
        {
            var result = await _userService.LoginAsync(dto);

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
    }
}
