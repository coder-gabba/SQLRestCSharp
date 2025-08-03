using AutoMapper;
using SqlAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SqlAPI.Models;
using Microsoft.AspNetCore.Authorization;
using SqlAPI.DTOs;
using SqlAPI.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SqlAPI.Controllers
{
    /// <summary>
    /// Controller for managing Person entities
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PeopleController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<PeopleController> _logger;
        private readonly IPersonService _personService;

        public PeopleController(ApplicationDbContext context, IMapper mapper, ILogger<PeopleController> logger, IPersonService personService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _personService = personService;
        }

        /// <summary>
        /// Retrieves all people from the database
        /// </summary>
        /// <returns>A list of all people</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PersonDto>>> GetPeople()
        {
            try
            {
                _logger.LogInformation("Retrieving all people");
                var people = await _context.People.ToListAsync();
                var result = _mapper.Map<IEnumerable<PersonDto>>(people);
                _logger.LogInformation("Successfully retrieved {Count} people", people.Count);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all people");
                return StatusCode(500, "An error occurred while retrieving people");
            }
        }

        /// <summary>
        /// Retrieves a specific person by their ID
        /// </summary>
        /// <param name="id">The ID of the person to retrieve</param>
        /// <returns>The person with the specified ID</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<PersonDto>> GetPerson(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving person with ID {PersonId}", id);
                var person = await _context.People.FindAsync(id);

                if (person == null)
                {
                    _logger.LogWarning("Person with ID {PersonId} not found", id);
                    return NotFound($"Person with ID {id} not found");
                }

                var result = _mapper.Map<PersonDto>(person);
                _logger.LogInformation("Successfully retrieved person with ID {PersonId}", id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving person with ID {PersonId}", id);
                return StatusCode(500, "An error occurred while retrieving the person");
            }
        }

        /// <summary>
        /// Creates a new person
        /// </summary>
        /// <param name="personDto">The person data to create</param>
        /// <returns>The created person</returns>
        [HttpPost]
        public async Task<ActionResult<PersonDto>> PostPerson(PersonDto personDto)
        {
            try
            {
                _logger.LogInformation("Creating new person with name {PersonName}", personDto.Name);
                var person = _mapper.Map<Person>(personDto);

                _context.People.Add(person);
                await _context.SaveChangesAsync();

                var createdPersonDto = _mapper.Map<PersonDto>(person);
                _logger.LogInformation("Successfully created person with ID {PersonId}", createdPersonDto.Id);
                return CreatedAtAction(nameof(GetPerson), new { id = createdPersonDto.Id }, createdPersonDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating person with name {PersonName}", personDto.Name);
                return StatusCode(500, "An error occurred while creating the person");
            }
        }

        /// <summary>
        /// Updates an existing person
        /// </summary>
        /// <param name="id">The ID of the person to update</param>
        /// <param name="personDto">The updated person data</param>
        /// <returns>No content if successful</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPerson(int id, PersonDto personDto)
        {
            try
            {
                if (id != personDto.Id)
                {
                    _logger.LogWarning("ID mismatch: URL ID {UrlId} does not match body ID {BodyId}", id, personDto.Id);
                    return BadRequest("ID mismatch between URL and request body");
                }

                _logger.LogInformation("Updating person with ID {PersonId}", id);
                var personInDb = await _context.People.FindAsync(id);
                if (personInDb == null)
                {
                    _logger.LogWarning("Person with ID {PersonId} not found for update", id);
                    return NotFound($"Person with ID {id} not found");
                }

                _mapper.Map(personDto, personInDb);

                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully updated person with ID {PersonId}", id);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning(ex, "Concurrency conflict while updating person with ID {PersonId}", id);
                    if (!PersonExists(id))
                    {
                        _logger.LogWarning("Person with ID {PersonId} no longer exists after concurrency conflict", id);
                        return NotFound($"Person with ID {id} not found");
                    }
                    throw;
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating person with ID {PersonId}", id);
                return StatusCode(500, "An error occurred while updating the person");
            }
        }

        /// <summary>
        /// Deletes a person (Admin only)
        /// </summary>
        /// <param name="id">The ID of the person to delete</param>
        /// <returns>No content if successful</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> DeletePerson(int id)
        {
            try
            {
                _logger.LogInformation("Deleting person with ID {PersonId}", id);
                var person = await _context.People.FindAsync(id);
                if (person == null)
                {
                    _logger.LogWarning("Person with ID {PersonId} not found for deletion", id);
                    return NotFound($"Person with ID {id} not found");
                }

                _context.People.Remove(person);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted person with ID {PersonId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting person with ID {PersonId}", id);
                return StatusCode(500, "An error occurred while deleting the person");
            }
        }

        /// <summary>
        /// Advanced search for people with filtering, sorting, and pagination
        /// </summary>
        /// <param name="searchDto">Search criteria</param>
        /// <returns>Paginated list of people matching the criteria</returns>
        [HttpPost("search")]
        public async Task<ActionResult<PagedResultDto<PersonDto>>> SearchPeople([FromBody] PersonSearchDto searchDto)
        {
            try
            {
                _logger.LogInformation("Searching people with criteria: {@SearchDto}", searchDto);
                var result = await _personService.SearchPersonsAsync(searchDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching people");
                return StatusCode(500, "An error occurred while searching people");
            }
        }

        /// <summary>
        /// Get people within a specific age range
        /// </summary>
        /// <param name="minAge">Minimum age</param>
        /// <param name="maxAge">Maximum age</param>
        /// <returns>List of people within the age range</returns>
        [HttpGet("age-range")]
        public async Task<ActionResult<IEnumerable<PersonDto>>> GetPeopleByAgeRange([FromQuery] int minAge, [FromQuery] int maxAge)
        {
            try
            {
                if (minAge < 0 || maxAge < 0 || minAge > maxAge)
                {
                    return BadRequest("Invalid age range. MinAge and MaxAge must be non-negative and MinAge must be <= MaxAge");
                }

                _logger.LogInformation("Getting people by age range: {MinAge}-{MaxAge}", minAge, maxAge);
                var result = await _personService.GetPersonsByAgeRangeAsync(minAge, maxAge);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting people by age range");
                return StatusCode(500, "An error occurred while retrieving people by age range");
            }
        }

        /// <summary>
        /// Get count of people by email domain
        /// </summary>
        /// <param name="domain">Email domain to search for (e.g., "@gmail.com")</param>
        /// <returns>Count of people with the specified email domain</returns>
        [HttpGet("count-by-domain")]
        public async Task<ActionResult<int>> GetCountByEmailDomain([FromQuery] string domain)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(domain))
                {
                    return BadRequest("Domain parameter is required");
                }

                _logger.LogInformation("Getting count by email domain: {Domain}", domain);
                var count = await _personService.GetCountByEmailDomainAsync(domain);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting count by email domain");
                return StatusCode(500, "An error occurred while counting people by email domain");
            }
        }

        /// <summary>
        /// Get statistics about all people
        /// </summary>
        /// <returns>Statistical information about people</returns>
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetStatistics()
        {
            try
            {
                _logger.LogInformation("Getting people statistics");
                
                var totalCount = await _context.People.CountAsync();
                var averageAge = totalCount > 0 ? await _context.People.AverageAsync(p => p.Age) : 0;
                var minAge = totalCount > 0 ? await _context.People.MinAsync(p => p.Age) : 0;
                var maxAge = totalCount > 0 ? await _context.People.MaxAsync(p => p.Age) : 0;
                
                var emailDomains = await _context.People
                    .Select(p => p.Email.Substring(p.Email.IndexOf('@')))
                    .GroupBy(domain => domain)
                    .Select(g => new { Domain = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToListAsync();

                var statistics = new
                {
                    TotalPeople = totalCount,
                    AverageAge = Math.Round(averageAge, 2),
                    MinAge = minAge,
                    MaxAge = maxAge,
                    TopEmailDomains = emailDomains
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting statistics");
                return StatusCode(500, "An error occurred while retrieving statistics");
            }
        }

        /// <summary>
        /// Checks if a person exists in the database
        /// </summary>
        /// <param name="id">The ID to check</param>
        /// <returns>True if the person exists, false otherwise</returns>
        private bool PersonExists(int id)
        {
            return _context.People.Any(e => e.Id == id);
        }
    }
}
