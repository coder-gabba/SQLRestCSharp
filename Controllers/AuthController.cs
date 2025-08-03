using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SqlAPI.Data;
using SqlAPI.Models;
using SqlAPI.Services;
using SqlAPI.DTOs;

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
        private readonly ILogger<AuthController> _logger;

        public AuthController(ApplicationDbContext context, JwtService jwtService, ILogger<AuthController> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token
        /// </summary>
        /// <param name="loginDto">Login credentials</param>
        /// <returns>JWT token if authentication successful</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid login request for username {Username}", loginDto.Username);
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Login attempt for username {Username}", loginDto.Username);

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

                if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Failed login attempt for username {Username}", loginDto.Username);
                    return Unauthorized("Invalid credentials");
                }

                var token = _jwtService.GenerateToken(user.Username);
                _logger.LogInformation("Successful login for username {Username}", loginDto.Username);
                return Ok(new { Token = token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login attempt for username {Username}", loginDto.Username);
                return StatusCode(500, "An error occurred while processing the login request");
            }
        }

        /// <summary>
        /// Registers a new user account
        /// </summary>
        /// <param name="registerDto">Registration details</param>
        /// <returns>Success message if registration successful</returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid registration request for username {Username}", registerDto.Username);
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Registration attempt for username {Username}", registerDto.Username);

                // Check if user already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == registerDto.Username);

                if (existingUser != null)
                {
                    _logger.LogWarning("Registration failed: Username {Username} already exists", registerDto.Username);
                    return Conflict("Username already exists");
                }

                // Create new user
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);
                var user = new User
                {
                    Username = registerDto.Username,
                    PasswordHash = hashedPassword,
                    Role = registerDto.Role
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully registered user {Username} with role {Role}", registerDto.Username, registerDto.Role);
                return Ok(new { message = "User registered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during registration for username {Username}", registerDto.Username);
                return StatusCode(500, "An error occurred while processing the registration request");
            }
        }

        /// <summary>
        /// Legacy login endpoint for backward compatibility
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>JWT token if authentication successful</returns>
        [HttpPost("legacy-login")]
        public async Task<IActionResult> LegacyLogin([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid legacy login request for username {Username}", request.Username);
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Legacy login attempt for username {Username}", request.Username);

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username);

                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Failed legacy login attempt for username {Username}", request.Username);
                    return Unauthorized("Invalid credentials");
                }

                var token = _jwtService.GenerateToken(user.Username);
                _logger.LogInformation("Successful legacy login for username {Username}", request.Username);
                return Ok(new { Token = token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during legacy login attempt for username {Username}", request.Username);
                return StatusCode(500, "An error occurred while processing the login request");
            }
        }
    }
}
