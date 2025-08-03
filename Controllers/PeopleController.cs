using AutoMapper;
using SqlAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SqlAPI.Models;
using Microsoft.AspNetCore.Authorization;
using SqlAPI.DTOs;
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

        public PeopleController(ApplicationDbContext context, IMapper mapper, ILogger<PeopleController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
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
