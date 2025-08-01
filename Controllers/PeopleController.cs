using SqlAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SqlAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/[controller]")]
public class PeopleController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PeopleController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/people
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Person>>> GetPeople()
    {
        return await _context.People.ToListAsync();
    }

    // GET: api/people/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Person>> GetPerson(int id)
    {
        var person = await _context.People.FindAsync(id);

        if (person == null)
            return NotFound();

        return person;
    }

    // POST: api/people
    [HttpPost]
    public async Task<ActionResult<Person>> PostPerson(Person person)
    {
        _context.People.Add(person);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetPerson", new { id = person.Id }, person);
    }

    // PUT: api/people/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutPerson(int id, Person person)
    {
        if (id != person.Id)
            return BadRequest();

        _context.Entry(person).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/people/5
    // Controllers/PeopleController.cs
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminPolicy")] // Nur Admins dürfen Personen löschen
    public async Task<IActionResult> DeletePerson(int id)
    {
        var person = await _context.People.FindAsync(id);
        if (person == null) return NotFound();
        _context.People.Remove(person);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
