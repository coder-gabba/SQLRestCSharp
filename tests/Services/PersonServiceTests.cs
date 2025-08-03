using Xunit;
using Moq;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SqlAPI.Services;
using SqlAPI.Data;
using SqlAPI.DTOs;
using SqlAPI.Models;
using SqlAPI.Profiles;

namespace SqlAPI.Tests.Services
{
    public class PersonServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<PersonService>> _mockLogger;
        private readonly PersonService _personService;

        public PersonServiceTests()
        {
            // Configure InMemory Database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            // Configure AutoMapper
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new PersonProfile());
            });
            _mapper = mapperConfig.CreateMapper();

            // Configure Mock Logger
            _mockLogger = new Mock<ILogger<PersonService>>();

            // Create service
            _personService = new PersonService(_context, _mapper, _mockLogger.Object);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            var persons = new List<Person>
            {
                new Person { Id = 1, Name = "Alice Johnson", Age = 25, Email = "alice@gmail.com" },
                new Person { Id = 2, Name = "Bob Smith", Age = 30, Email = "bob@yahoo.com" },
                new Person { Id = 3, Name = "Charlie Brown", Age = 35, Email = "charlie@gmail.com" },
                new Person { Id = 4, Name = "Diana Prince", Age = 28, Email = "diana@outlook.com" },
                new Person { Id = 5, Name = "Eve Wilson", Age = 42, Email = "eve@gmail.com" }
            };

            _context.People.AddRange(persons);
            _context.SaveChanges();
        }

        [Fact]
        public async Task SearchPersonsAsync_WithNameFilter_ShouldReturnMatchingPersons()
        {
            // Arrange
            var searchDto = new PersonSearchDto
            {
                Name = "alice",
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _personService.SearchPersonsAsync(searchDto);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.Items.First().Name.Should().Be("Alice Johnson");
            result.TotalCount.Should().Be(1);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task SearchPersonsAsync_WithAgeRange_ShouldReturnCorrectPersons()
        {
            // Arrange
            var searchDto = new PersonSearchDto
            {
                MinAge = 25,
                MaxAge = 30,
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _personService.SearchPersonsAsync(searchDto);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(3); // Alice (25), Bob (30), Diana (28)
            result.TotalCount.Should().Be(3);
            result.Items.All(p => p.Age >= 25 && p.Age <= 30).Should().BeTrue();
        }

        [Fact]
        public async Task SearchPersonsAsync_WithEmailDomain_ShouldReturnMatchingPersons()
        {
            // Arrange
            var searchDto = new PersonSearchDto
            {
                EmailDomain = "@gmail.com",
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _personService.SearchPersonsAsync(searchDto);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(3); // Alice, Charlie, Eve
            result.TotalCount.Should().Be(3);
            result.Items.All(p => p.Email.Contains("@gmail.com")).Should().BeTrue();
        }

        [Fact]
        public async Task SearchPersonsAsync_WithSortingByAge_ShouldReturnSortedResults()
        {
            // Arrange
            var searchDto = new PersonSearchDto
            {
                SortBy = "Age",
                SortDirection = "asc",
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _personService.SearchPersonsAsync(searchDto);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(5);
            result.Items.Should().BeInAscendingOrder(p => p.Age);
        }

        [Fact]
        public async Task SearchPersonsAsync_WithPagination_ShouldReturnCorrectPage()
        {
            // Arrange
            var searchDto = new PersonSearchDto
            {
                PageNumber = 2,
                PageSize = 2,
                SortBy = "Name"
            };

            // Act
            var result = await _personService.SearchPersonsAsync(searchDto);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(5);
            result.PageNumber.Should().Be(2);
            result.PageSize.Should().Be(2);
            result.TotalPages.Should().Be(3);
            result.HasPreviousPage.Should().BeTrue();
            result.HasNextPage.Should().BeTrue();
        }

        [Fact]
        public async Task GetPersonByIdAsync_ExistingPerson_ShouldReturnPerson()
        {
            // Act
            var result = await _personService.GetPersonByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("Alice Johnson");
            result.Email.Should().Be("alice@gmail.com");
        }

        [Fact]
        public async Task GetPersonByIdAsync_NonExistingPerson_ShouldReturnNull()
        {
            // Act
            var result = await _personService.GetPersonByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreatePersonAsync_ValidPerson_ShouldCreateAndReturnPerson()
        {
            // Arrange
            var personDto = new PersonDto
            {
                Name = "New Person",
                Age = 30,
                Email = "new@test.com"
            };

            // Act
            var result = await _personService.CreatePersonAsync(personDto);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("New Person");
            result.Id.Should().BeGreaterThan(0);

            // Verify in database
            var personInDb = await _context.People.FindAsync(result.Id);
            personInDb.Should().NotBeNull();
            personInDb!.Name.Should().Be("New Person");
        }

        [Fact]
        public async Task UpdatePersonAsync_ExistingPerson_ShouldUpdateAndReturnPerson()
        {
            // Arrange
            var updateDto = new PersonDto
            {
                Name = "Updated Alice",
                Age = 26,
                Email = "updated.alice@gmail.com"
            };

            // Act
            var result = await _personService.UpdatePersonAsync(1, updateDto);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("Updated Alice");
            result.Age.Should().Be(26);
            result.Email.Should().Be("updated.alice@gmail.com");

            // Verify in database
            var personInDb = await _context.People.FindAsync(1);
            personInDb!.Name.Should().Be("Updated Alice");
        }

        [Fact]
        public async Task UpdatePersonAsync_NonExistingPerson_ShouldReturnNull()
        {
            // Arrange
            var updateDto = new PersonDto
            {
                Name = "Non Existing",
                Age = 30,
                Email = "nonexisting@test.com"
            };

            // Act
            var result = await _personService.UpdatePersonAsync(999, updateDto);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task DeletePersonAsync_ExistingPerson_ShouldDeleteAndReturnTrue()
        {
            // Act
            var result = await _personService.DeletePersonAsync(1);

            // Assert
            result.Should().BeTrue();

            // Verify deletion
            var personInDb = await _context.People.FindAsync(1);
            personInDb.Should().BeNull();
        }

        [Fact]
        public async Task DeletePersonAsync_NonExistingPerson_ShouldReturnFalse()
        {
            // Act
            var result = await _personService.DeletePersonAsync(999);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetPersonsByAgeRangeAsync_ValidRange_ShouldReturnPersonsInRange()
        {
            // Act
            var result = await _personService.GetPersonsByAgeRangeAsync(25, 35);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(4); // Alice, Bob, Charlie, Diana
            result.All(p => p.Age >= 25 && p.Age <= 35).Should().BeTrue();
            result.Should().BeInAscendingOrder(p => p.Age);
        }

        [Fact]
        public async Task ExistsAsync_ExistingPerson_ShouldReturnTrue()
        {
            // Act
            var result = await _personService.ExistsAsync(1);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_NonExistingPerson_ShouldReturnFalse()
        {
            // Act
            var result = await _personService.ExistsAsync(999);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetCountByEmailDomainAsync_ExistingDomain_ShouldReturnCorrectCount()
        {
            // Act
            var result = await _personService.GetCountByEmailDomainAsync("@gmail.com");

            // Assert
            result.Should().Be(3); // Alice, Charlie, Eve
        }

        [Fact]
        public async Task GetCountByEmailDomainAsync_NonExistingDomain_ShouldReturnZero()
        {
            // Act
            var result = await _personService.GetCountByEmailDomainAsync("@nonexisting.com");

            // Assert
            result.Should().Be(0);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
