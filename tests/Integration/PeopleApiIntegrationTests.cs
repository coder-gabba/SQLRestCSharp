using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using SqlAPI.Data;
using SqlAPI.DTOs;
using SqlAPI.Models;
using System.Text;
using System.Text.Json;
using System.Net;

namespace SqlAPI.Tests.Integration
{
    public class PeopleApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly ApplicationDbContext _context;

        public PeopleApiIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the real database context
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Add InMemory database for testing
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb");
                    });
                });
            });

            _client = _factory.CreateClient();
            
            // Get the test database context
            using var scope = _factory.Services.CreateScope();
            _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            if (!_context.People.Any())
            {
                var testPeople = new List<Person>
                {
                    new Person { Name = "Alice Johnson", Age = 25, Email = "alice@gmail.com" },
                    new Person { Name = "Bob Smith", Age = 30, Email = "bob@yahoo.com" },
                    new Person { Name = "Charlie Brown", Age = 35, Email = "charlie@outlook.com" }
                };

                _context.People.AddRange(testPeople);
                _context.SaveChanges();
            }
        }

        [Fact]
        public async Task GET_People_ShouldReturnAllPeople()
        {
            // Act
            var response = await _client.GetAsync("/api/people");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var people = JsonSerializer.Deserialize<List<PersonDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            people.Should().NotBeNull();
            people!.Should().HaveCount(c => c >= 3);
        }

        [Fact]
        public async Task GET_PeopleById_ExistingId_ShouldReturnPerson()
        {
            // Arrange
            var existingPerson = _context.People.First();

            // Act
            var response = await _client.GetAsync($"/api/people/{existingPerson.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var person = JsonSerializer.Deserialize<PersonDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            person.Should().NotBeNull();
            person!.Id.Should().Be(existingPerson.Id);
            person.Name.Should().Be(existingPerson.Name);
        }

        [Fact]
        public async Task GET_PeopleById_NonExistingId_ShouldReturnNotFound()
        {
            // Act
            var response = await _client.GetAsync("/api/people/999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task POST_People_ValidPerson_ShouldCreatePerson()
        {
            // Arrange
            var newPerson = new PersonDto
            {
                Name = "Diana Prince",
                Age = 28,
                Email = "diana@test.com"
            };

            var json = JsonSerializer.Serialize(newPerson);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/people", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var createdPerson = JsonSerializer.Deserialize<PersonDto>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            createdPerson.Should().NotBeNull();
            createdPerson!.Name.Should().Be(newPerson.Name);
            createdPerson.Id.Should().BeGreaterThan(0);

            // Verify in database
            var personInDb = await _context.People.FindAsync(createdPerson.Id);
            personInDb.Should().NotBeNull();
        }

        [Fact]
        public async Task POST_People_InvalidPerson_ShouldReturnBadRequest()
        {
            // Arrange
            var invalidPerson = new PersonDto
            {
                Name = "", // Invalid: empty name
                Age = -5,  // Invalid: negative age
                Email = "invalid-email" // Invalid: not a valid email
            };

            var json = JsonSerializer.Serialize(invalidPerson);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/people", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task PUT_People_ValidUpdate_ShouldUpdatePerson()
        {
            // Arrange
            var existingPerson = _context.People.First();
            var updatedPerson = new PersonDto
            {
                Id = existingPerson.Id,
                Name = "Updated Name",
                Age = existingPerson.Age + 1,
                Email = "updated@test.com"
            };

            var json = JsonSerializer.Serialize(updatedPerson);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync($"/api/people/{existingPerson.Id}", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify in database
            var personInDb = await _context.People.FindAsync(existingPerson.Id);
            personInDb.Should().NotBeNull();
            personInDb!.Name.Should().Be("Updated Name");
            personInDb.Email.Should().Be("updated@test.com");
        }

        [Fact]
        public async Task DELETE_People_ExistingPerson_ShouldDeletePerson()
        {
            // Arrange
            var personToDelete = _context.People.First();
            var originalId = personToDelete.Id;

            // Act
            var response = await _client.DeleteAsync($"/api/people/{originalId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify deletion
            var deletedPerson = await _context.People.FindAsync(originalId);
            deletedPerson.Should().BeNull();
        }

        [Fact]
        public async Task POST_PeopleSearch_ValidSearch_ShouldReturnResults()
        {
            // Arrange
            var searchDto = new PersonSearchDto
            {
                Name = "Alice",
                PageNumber = 1,
                PageSize = 10
            };

            var json = JsonSerializer.Serialize(searchDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/people/search", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PagedResultDto<PersonDto>>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            result.Should().NotBeNull();
            result!.Items.Should().NotBeEmpty();
            result.Items.Should().Contain(p => p.Name.Contains("Alice"));
        }

        [Fact]
        public async Task GET_PeopleAgeRange_ValidRange_ShouldReturnResults()
        {
            // Act
            var response = await _client.GetAsync("/api/people/age-range?minAge=20&maxAge=40");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            var people = JsonSerializer.Deserialize<List<PersonDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            people.Should().NotBeNull();
            people!.Should().NotBeEmpty();
            people.All(p => p.Age >= 20 && p.Age <= 40).Should().BeTrue();
        }

        [Fact]
        public async Task GET_PeopleAgeRange_InvalidRange_ShouldReturnBadRequest()
        {
            // Act
            var response = await _client.GetAsync("/api/people/age-range?minAge=40&maxAge=20");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GET_CountByDomain_ValidDomain_ShouldReturnCount()
        {
            // Act
            var response = await _client.GetAsync("/api/people/count-by-domain?domain=@gmail.com");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            var count = JsonSerializer.Deserialize<int>(content);

            count.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task GET_Statistics_ShouldReturnStatistics()
        {
            // Act
            var response = await _client.GetAsync("/api/people/statistics");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            var statistics = JsonSerializer.Deserialize<JsonElement>(content);

            statistics.TryGetProperty("totalPeople", out var totalPeople).Should().BeTrue();
            statistics.TryGetProperty("averageAge", out var averageAge).Should().BeTrue();
            statistics.TryGetProperty("minAge", out var minAge).Should().BeTrue();
            statistics.TryGetProperty("maxAge", out var maxAge).Should().BeTrue();

            totalPeople.GetInt32().Should().BeGreaterThanOrEqualTo(3);
        }

        public void Dispose()
        {
            _context.Dispose();
            _client.Dispose();
        }
    }
}
