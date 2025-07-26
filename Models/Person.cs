// Models/Person.cs
using System.ComponentModel.DataAnnotations;

namespace SqlAPI.Models
{
    public class Person
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }
}
