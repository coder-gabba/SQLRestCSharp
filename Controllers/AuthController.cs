using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SqlAPI.Data;
using SqlAPI.Models;
using SqlAPI.Services;

namespace SqlAPI.Controllers
{
    /// <summary>
    /// Controller for handling authentication operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;

        public AuthController(ApplicationDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>JWT token if authentication successful</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid credentials");
            }

            var token = _jwtService.GenerateToken(user.Username);
            return Ok(new { Token = token });
        }
    }
}
