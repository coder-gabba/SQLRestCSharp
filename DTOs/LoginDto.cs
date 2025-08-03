using System.ComponentModel.DataAnnotations;

namespace SqlAPI.DTOs
{
    /// <summary>
    /// Data transfer object for user login
    /// </summary>
    public class LoginDto
    {
        /// <summary>
        /// The username for authentication
        /// </summary>
        [Required]
        public required string Username { get; set; }

        /// <summary>
        /// The password for authentication
        /// </summary>
        [Required]
        public required string Password { get; set; }
    }
}