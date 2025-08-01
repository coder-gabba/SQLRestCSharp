using System.ComponentModel.DataAnnotations;

namespace SqlAPI.Models
{
    /// <summary>
    /// Request model for user login authentication
    /// </summary>
    public class LoginRequest
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