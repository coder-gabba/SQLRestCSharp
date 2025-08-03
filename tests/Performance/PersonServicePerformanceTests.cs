using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SqlAPI.Services;
using SqlAPI.Data;
using SqlAPI.DTOs;
using SqlAPI.Models;
using AutoMapper;
using SqlAPI.Profiles;
using System.Diagnostics;

namespace SqlAPI.Tests.Performance
{
    public class PersonServicePerformanceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly PersonService _personService;

        public PersonServicePerformanceTests()
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

            // Configure Logger
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<PersonService>();

            // Create service
            _personService = new PersonService(_context, _mapper, logger);

            // Seed large amount of test data
            SeedLargeTestData();
        }

        private void SeedLargeTestData()
        {
            var random = new Random(42); // Fixed seed for reproducible tests
            var persons = new List<Person>();
            var domains = new[] { "@gmail.com", "@yahoo.com", "@outlook.com", "@hotmail.com", "@test.com" };
            var firstNames = new[] { "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Henry", "Ivy", "Jack" };
            var lastNames = new[] { "Johnson", "Smith", "Brown", "Davis", "Wilson", "Moore", "Taylor", "Anderson", "Thomas", "Jackson" };

            for (int i = 1; i <= 1000; i++)
            {
                var firstName = firstNames[random.Next(firstNames.Length)];
                var lastName = lastNames[random.Next(lastNames.Length)];
                var domain = domains[random.Next(domains.Length)];

                persons.Add(new Person
                {
                    Id = i,
                    Name = $"{firstName} {lastName}",
                    Age = random.Next(18, 80),
                    Email = $"{firstName.ToLower()}.{lastName.ToLower()}{i}{domain}"
                });
            }

            _context.People.AddRange(persons);
            _context.SaveChanges();
        }

        [Fact]
        public async Task SearchPersonsAsync_LargeDataset_ShouldCompleteWithinTimeLimit()
        {
            // Arrange
            var searchDto = new PersonSearchDto
            {
                Name = "Alice",
                PageNumber = 1,
                PageSize = 50
            };

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await _personService.SearchPersonsAsync(searchDto);

            // Assert
            stopwatch.Stop();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Search should complete within 1 second");
            result.Should().NotBeNull();
            result.Items.Should().NotBeEmpty();
        }

        [Fact]
        public async Task SearchPersonsAsync_PaginationPerformance_ShouldBeConsistent()
        {
            // Arrange & Act
            var times = new List<long>();

            for (int page = 1; page <= 10; page++)
            {
                var searchDto = new PersonSearchDto
                {
                    PageNumber = page,
                    PageSize = 20
                };

                var stopwatch = Stopwatch.StartNew();
                var result = await _personService.SearchPersonsAsync(searchDto);
                stopwatch.Stop();

                times.Add(stopwatch.ElapsedMilliseconds);
                result.Should().NotBeNull();
            }

            // Assert
            var averageTime = times.Average();
            var maxTime = times.Max();
            var minTime = times.Min();

            averageTime.Should().BeLessThan(2000, "Average pagination time should be under 2000ms");
            maxTime.Should().BeLessThan(5000, "Max pagination time should be under 5 seconds");
            
            // Time variance should not be too high (performance should be consistent)
            var variance = maxTime - minTime;
            variance.Should().BeLessThan((long)(averageTime * 5), "Performance should be consistent across pages");
        }

        [Fact]
        public async Task GetPersonsByAgeRangeAsync_LargeRange_ShouldCompleteQuickly()
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await _personService.GetPersonsByAgeRangeAsync(20, 70);

            // Assert
            stopwatch.Stop();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "Age range query should complete within 500ms");
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
            result.All(p => p.Age >= 20 && p.Age <= 70).Should().BeTrue();
        }

        [Fact]
        public async Task GetCountByEmailDomainAsync_ShouldCompleteQuickly()
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await _personService.GetCountByEmailDomainAsync("@gmail.com");

            // Assert
            stopwatch.Stop();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(200, "Count query should complete within 200ms");
            result.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task CreatePersonAsync_BulkOperations_ShouldCompleteWithinTimeLimit()
        {
            // Arrange
            var personsToCreate = new List<PersonDto>();
            for (int i = 0; i < 100; i++)
            {
                personsToCreate.Add(new PersonDto
                {
                    Name = $"Test User {i}",
                    Age = 25 + (i % 40),
                    Email = $"testuser{i}@performance.test"
                });
            }

            var stopwatch = Stopwatch.StartNew();

            // Act
            var tasks = personsToCreate.Select(p => _personService.CreatePersonAsync(p));
            var results = await Task.WhenAll(tasks);

            // Assert
            stopwatch.Stop();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "100 create operations should complete within 5 seconds");
            results.Should().HaveCount(100);
            results.All(r => r.Id > 0).Should().BeTrue();
        }

        [Theory]
        [InlineData(10)]
        [InlineData(50)]
        [InlineData(100)]
        public async Task SearchPersonsAsync_VariousPageSizes_ShouldScaleLinearly(int pageSize)
        {
            // Arrange
            var searchDto = new PersonSearchDto
            {
                PageNumber = 1,
                PageSize = pageSize,
                SortBy = "Name"
            };

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await _personService.SearchPersonsAsync(searchDto);

            // Assert
            stopwatch.Stop();
            
            // Rough performance expectations (adjust based on your requirements)
            var expectedMaxTime = pageSize * 100; // 100ms per item is reasonable for integration tests
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(expectedMaxTime, 
                $"Search with page size {pageSize} should complete within {expectedMaxTime}ms");
            
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(Math.Min(pageSize, result.TotalCount));
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
