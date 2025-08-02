using System.ComponentModel.DataAnnotations;

namespace SqlAPI.Models
{
    /// <summary>
    /// Represents a user in the system for authentication purposes
    /// </summary>
    public class User
    {
        /// <summary>
        /// Unique identifier for the user
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The username for authentication (required, max 50 characters)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// The hashed password for authentication (required)
        /// </summary>
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// The role assigned to the user (required, max 20 characters)
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Role { get; set; } = string.Empty;
    }
}