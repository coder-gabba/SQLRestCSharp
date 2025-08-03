using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SqlAPI.Data;
using SqlAPI.DTOs;
using SqlAPI.Models;

namespace SqlAPI.Services
{
    /// <summary>
    /// Service for advanced person operations
    /// </summary>
    public interface IPersonService
    {
        Task<PagedResultDto<PersonDto>> SearchPersonsAsync(PersonSearchDto searchDto);
        Task<PersonDto?> GetPersonByIdAsync(int id);
        Task<PersonDto> CreatePersonAsync(PersonDto personDto);
        Task<PersonDto?> UpdatePersonAsync(int id, PersonDto personDto);
        Task<bool> DeletePersonAsync(int id);
        Task<List<PersonDto>> GetPersonsByAgeRangeAsync(int minAge, int maxAge);
        Task<bool> ExistsAsync(int id);
        Task<int> GetCountByEmailDomainAsync(string domain);
    }

    /// <summary>
    /// Implementation of person service with advanced features
    /// </summary>
    public class PersonService : IPersonService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<PersonService> _logger;

        public PersonService(ApplicationDbContext context, IMapper mapper, ILogger<PersonService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResultDto<PersonDto>> SearchPersonsAsync(PersonSearchDto searchDto)
        {
            _logger.LogInformation("Searching persons with filters: {@SearchDto}", searchDto);

            var query = _context.People.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchDto.Name))
            {
                query = query.Where(p => p.Name.ToLower().Contains(searchDto.Name.ToLower()));
            }

            if (searchDto.MinAge.HasValue)
            {
                query = query.Where(p => p.Age >= searchDto.MinAge.Value);
            }

            if (searchDto.MaxAge.HasValue)
            {
                query = query.Where(p => p.Age <= searchDto.MaxAge.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchDto.EmailDomain))
            {
                query = query.Where(p => p.Email.ToLower().Contains(searchDto.EmailDomain.ToLower()));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = searchDto.SortBy.ToLower() switch
            {
                "age" => searchDto.SortDirection.ToLower() == "desc" 
                    ? query.OrderByDescending(p => p.Age)
                    : query.OrderBy(p => p.Age),
                "email" => searchDto.SortDirection.ToLower() == "desc" 
                    ? query.OrderByDescending(p => p.Email)
                    : query.OrderBy(p => p.Email),
                _ => searchDto.SortDirection.ToLower() == "desc" 
                    ? query.OrderByDescending(p => p.Name)
                    : query.OrderBy(p => p.Name)
            };

            // Apply pagination
            var persons = await query
                .Skip((searchDto.PageNumber - 1) * searchDto.PageSize)
                .Take(searchDto.PageSize)
                .ToListAsync();

            var personDtos = _mapper.Map<List<PersonDto>>(persons);

            return new PagedResultDto<PersonDto>
            {
                Items = personDtos,
                TotalCount = totalCount,
                PageNumber = searchDto.PageNumber,
                PageSize = searchDto.PageSize
            };
        }

        public async Task<PersonDto?> GetPersonByIdAsync(int id)
        {
            _logger.LogInformation("Getting person by ID: {Id}", id);
            var person = await _context.People.FindAsync(id);
            return person != null ? _mapper.Map<PersonDto>(person) : null;
        }

        public async Task<PersonDto> CreatePersonAsync(PersonDto personDto)
        {
            _logger.LogInformation("Creating new person: {@PersonDto}", personDto);
            var person = _mapper.Map<Person>(personDto);
            _context.People.Add(person);
            await _context.SaveChangesAsync();
            return _mapper.Map<PersonDto>(person);
        }

        public async Task<PersonDto?> UpdatePersonAsync(int id, PersonDto personDto)
        {
            _logger.LogInformation("Updating person {Id}: {@PersonDto}", id, personDto);
            var person = await _context.People.FindAsync(id);
            if (person == null) return null;

            _mapper.Map(personDto, person);
            person.Id = id; // Ensure ID doesn't change
            await _context.SaveChangesAsync();
            return _mapper.Map<PersonDto>(person);
        }

        public async Task<bool> DeletePersonAsync(int id)
        {
            _logger.LogInformation("Deleting person with ID: {Id}", id);
            var person = await _context.People.FindAsync(id);
            if (person == null) return false;

            _context.People.Remove(person);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<PersonDto>> GetPersonsByAgeRangeAsync(int minAge, int maxAge)
        {
            _logger.LogInformation("Getting persons by age range: {MinAge}-{MaxAge}", minAge, maxAge);
            var persons = await _context.People
                .Where(p => p.Age >= minAge && p.Age <= maxAge)
                .OrderBy(p => p.Age)
                .ToListAsync();
            
            return _mapper.Map<List<PersonDto>>(persons);
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.People.AnyAsync(p => p.Id == id);
        }

        public async Task<int> GetCountByEmailDomainAsync(string domain)
        {
            _logger.LogInformation("Getting count by email domain: {Domain}", domain);
            return await _context.People
                .Where(p => p.Email.ToLower().Contains(domain.ToLower()))
                .CountAsync();
        }
    }
}
