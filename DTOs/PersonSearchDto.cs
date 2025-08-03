using System.ComponentModel.DataAnnotations;

namespace SqlAPI.DTOs
{
    /// <summary>
    /// DTO for searching persons with various filters
    /// </summary>
    public class PersonSearchDto
    {
        /// <summary>
        /// Search by name (partial match)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Minimum age filter
        /// </summary>
        [Range(0, 150)]
        public int? MinAge { get; set; }

        /// <summary>
        /// Maximum age filter
        /// </summary>
        [Range(0, 150)]
        public int? MaxAge { get; set; }

        /// <summary>
        /// Search by email domain (e.g., "@gmail.com")
        /// </summary>
        public string? EmailDomain { get; set; }

        /// <summary>
        /// Page number for pagination (starts from 1)
        /// </summary>
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Number of items per page
        /// </summary>
        [Range(1, 100)]
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Sort field (Name, Age, Email)
        /// </summary>
        public string SortBy { get; set; } = "Name";

        /// <summary>
        /// Sort direction (asc, desc)
        /// </summary>
        public string SortDirection { get; set; } = "asc";
    }
}
