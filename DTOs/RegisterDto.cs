using System.ComponentModel.DataAnnotations;

namespace SqlAPI.DTOs
{
    /// <summary>
    /// Data transfer object for user registration
    /// </summary>
    public class RegisterDto
    {
        /// <summary>
        /// The username for the new user account
        /// </summary>
        [Required]
        public required string Username { get; set; }

        /// <summary>
        /// The password for the new user account
        /// </summary>
        [Required]
        public required string Password { get; set; }

        /// <summary>
        /// Password confirmation for validation
        /// </summary>
        [Required]
        public required string ConfirmPassword { get; set; }

        /// <summary>
        /// The role for the new user
        /// </summary>
        [Required]
        public required string Role { get; set; }
    }
}