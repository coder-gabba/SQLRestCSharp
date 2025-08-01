using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace SqlAPI.Models
{
    public class Person
    {
        [Key]
        public int Id { get; set; }
        [Required] // NOT NULL
        [StringLength(100)] // VARCHAR(100)
        public string Name { get; set; }

        [Required] // NOT NULL
        [EmailAddress] // E-Mail-Validierung
        [StringLength(100)] // VARCHAR(100)
        public string Email { get; set; }
    }
}
