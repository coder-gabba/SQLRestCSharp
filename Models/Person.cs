using System.ComponentModel.DataAnnotations;

namespace SqlAPI.Models
{
    /// <summary>
    /// Represents a person entity in the system
    /// </summary>
    public class Person
    {
        /// <summary>
        /// Unique identifier for the person
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The person's full name (required, max 100 characters)
        /// </summary>
        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        /// <summary>
        /// The person's age (required, must be between 0 and 150)
        /// </summary>
        [Required]
        [Range(0, 150)]
        public int Age { get; set; }

        /// <summary>
        /// The person's email address (required, valid email format, max 100 characters)
        /// </summary>
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public required string Email { get; set; }
    }
}
