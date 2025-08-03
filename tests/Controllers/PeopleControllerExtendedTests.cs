using Xunit;
using Moq;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SqlAPI.Controllers;
using SqlAPI.Services;
using SqlAPI.DTOs;
using SqlAPI.Data;
using SqlAPI.Models;
using SqlAPI.Profiles;

namespace SqlAPI.Tests.Controllers
{
    public class PeopleControllerExtendedTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<PeopleController>> _mockLogger;
        private readonly Mock<IPersonService> _mockPersonService;
        private readonly PeopleController _controller;

        public PeopleControllerExtendedTests()
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

            // Configure Mock Logger and Service
            _mockLogger = new Mock<ILogger<PeopleController>>();
            _mockPersonService = new Mock<IPersonService>();

            // Create controller
            _controller = new PeopleController(_context, _mapper, _mockLogger.Object, _mockPersonService.Object);
        }

        [Fact]
        public async Task SearchPeople_ValidRequest_ShouldReturnOkResult()
        {
            // Arrange
            var searchDto = new PersonSearchDto { Name = "Alice" };
            var expectedResult = new PagedResultDto<PersonDto>
            {
                Items = new List<PersonDto> 
                { 
                    new PersonDto { Id = 1, Name = "Alice Johnson", Age = 25, Email = "alice@test.com" } 
                },
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 10
            };

            _mockPersonService
                .Setup(s => s.SearchPersonsAsync(It.IsAny<PersonSearchDto>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.SearchPeople(searchDto);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedData = okResult.Value.Should().BeOfType<PagedResultDto<PersonDto>>().Subject;
            returnedData.Items.Should().HaveCount(1);
            returnedData.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task SearchPeople_ServiceThrowsException_ShouldReturnServerError()
        {
            // Arrange
            var searchDto = new PersonSearchDto { Name = "Alice" };
            _mockPersonService
                .Setup(s => s.SearchPersonsAsync(It.IsAny<PersonSearchDto>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.SearchPeople(searchDto);

            // Assert
            result.Should().NotBeNull();
            var serverErrorResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            serverErrorResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetPeopleByAgeRange_ValidRange_ShouldReturnOkResult()
        {
            // Arrange
            var expectedPersons = new List<PersonDto>
            {
                new PersonDto { Id = 1, Name = "Alice", Age = 25, Email = "alice@test.com" },
                new PersonDto { Id = 2, Name = "Bob", Age = 30, Email = "bob@test.com" }
            };

            _mockPersonService
                .Setup(s => s.GetPersonsByAgeRangeAsync(25, 30))
                .ReturnsAsync(expectedPersons);

            // Act
            var result = await _controller.GetPeopleByAgeRange(25, 30);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedData = okResult.Value.Should().BeAssignableTo<IEnumerable<PersonDto>>().Subject;
            returnedData.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetPeopleByAgeRange_InvalidRange_ShouldReturnBadRequest()
        {
            // Act
            var result = await _controller.GetPeopleByAgeRange(30, 25); // Invalid: min > max

            // Assert
            result.Should().NotBeNull();
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeOfType<string>();
        }

        [Fact]
        public async Task GetPeopleByAgeRange_NegativeAges_ShouldReturnBadRequest()
        {
            // Act
            var result = await _controller.GetPeopleByAgeRange(-5, 25);

            // Assert
            result.Should().NotBeNull();
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeOfType<string>();
        }

        [Fact]
        public async Task GetCountByEmailDomain_ValidDomain_ShouldReturnCount()
        {
            // Arrange
            _mockPersonService
                .Setup(s => s.GetCountByEmailDomainAsync("@gmail.com"))
                .ReturnsAsync(5);

            // Act
            var result = await _controller.GetCountByEmailDomain("@gmail.com");

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(5);
        }

        [Fact]
        public async Task GetCountByEmailDomain_EmptyDomain_ShouldReturnBadRequest()
        {
            // Act
            var result = await _controller.GetCountByEmailDomain("");

            // Assert
            result.Should().NotBeNull();
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeOfType<string>();
        }

        [Fact]
        public async Task GetStatistics_ValidRequest_ShouldReturnStatistics()
        {
            // Arrange
            var testPeople = new List<Person>
            {
                new Person { Id = 1, Name = "Alice", Age = 25, Email = "alice@gmail.com" },
                new Person { Id = 2, Name = "Bob", Age = 30, Email = "bob@yahoo.com" },
                new Person { Id = 3, Name = "Charlie", Age = 35, Email = "charlie@gmail.com" }
            };

            _context.People.AddRange(testPeople);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetStatistics();

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var statistics = okResult.Value;
            statistics.Should().NotBeNull();

            // Use reflection to check the anonymous object properties
            var statisticsType = statistics!.GetType();
            var totalPeopleProperty = statisticsType.GetProperty("TotalPeople");
            var totalPeople = totalPeopleProperty?.GetValue(statistics);
            totalPeople.Should().Be(3);

            var averageAgeProperty = statisticsType.GetProperty("AverageAge");
            var averageAge = averageAgeProperty?.GetValue(statistics);
            averageAge.Should().Be(30.0);
        }

        [Theory]
        [InlineData(0, 100)]
        [InlineData(18, 65)]
        [InlineData(25, 25)]
        public async Task GetPeopleByAgeRange_VariousValidRanges_ShouldReturnOk(int minAge, int maxAge)
        {
            // Arrange
            _mockPersonService
                .Setup(s => s.GetPersonsByAgeRangeAsync(minAge, maxAge))
                .ReturnsAsync(new List<PersonDto>());

            // Act
            var result = await _controller.GetPeopleByAgeRange(minAge, maxAge);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Theory]
        [InlineData("@gmail.com")]
        [InlineData("@yahoo.com")]
        [InlineData("@outlook.com")]
        public async Task GetCountByEmailDomain_VariousValidDomains_ShouldReturnOk(string domain)
        {
            // Arrange
            _mockPersonService
                .Setup(s => s.GetCountByEmailDomainAsync(domain))
                .ReturnsAsync(It.IsAny<int>());

            // Act
            var result = await _controller.GetCountByEmailDomain(domain);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<OkObjectResult>();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
