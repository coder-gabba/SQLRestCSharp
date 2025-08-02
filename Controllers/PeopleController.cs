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

        public PeopleController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        /// <summary>
        /// Retrieves all people from the database
        /// </summary>
        /// <returns>A list of all people</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PersonDto>>> GetPeople()
        {
            var people = await _context.People.ToListAsync();
            return Ok(_mapper.Map<IEnumerable<PersonDto>>(people));
        }

        /// <summary>
        /// Retrieves a specific person by their ID
        /// </summary>
        /// <param name="id">The ID of the person to retrieve</param>
        /// <returns>The person with the specified ID</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<PersonDto>> GetPerson(int id)
        {
            var person = await _context.People.FindAsync(id);

            if (person == null)
                return NotFound($"Person with ID {id} not found");

            return Ok(_mapper.Map<PersonDto>(person));
        }

        /// <summary>
        /// Creates a new person
        /// </summary>
        /// <param name="personDto">The person data to create</param>
        /// <returns>The created person</returns>
        [HttpPost]
        public async Task<ActionResult<PersonDto>> PostPerson(PersonDto personDto)
        {
            var person = _mapper.Map<Person>(personDto);

            _context.People.Add(person);
            await _context.SaveChangesAsync();

            var createdPersonDto = _mapper.Map<PersonDto>(person);
            return CreatedAtAction(nameof(GetPerson), new { id = createdPersonDto.Id }, createdPersonDto);
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
            if (id != personDto.Id)
                return BadRequest("ID mismatch between URL and request body");

            var personInDb = await _context.People.FindAsync(id);
            if (personInDb == null)
            {
                return NotFound($"Person with ID {id} not found");
            }

            _mapper.Map(personDto, personInDb);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PersonExists(id))
                {
                    return NotFound($"Person with ID {id} not found");
                }
                throw;
            }

            return NoContent();
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
            var person = await _context.People.FindAsync(id);
            if (person == null)
            {
                return NotFound($"Person with ID {id} not found");
            }

            _context.People.Remove(person);
            await _context.SaveChangesAsync();

            return NoContent();
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
