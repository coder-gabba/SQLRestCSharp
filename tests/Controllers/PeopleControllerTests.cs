using Xunit;
using Moq;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Microsoft.Extensions.Logging;
using SqlAPI.Controllers;
using SqlAPI.Data;
using SqlAPI.DTOs;
using SqlAPI.Models;
using SqlAPI.Profiles;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SqlAPI.Tests.Controllers
{
    public class PeopleControllerTests
    {
        private readonly IMapper _mapper;
        private readonly DbContextOptions<ApplicationDbContext> _dbOptions;
        private readonly Mock<ILogger<PeopleController>> _mockLogger;

        public PeopleControllerTests()
        {
            // Configure AutoMapper
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new PersonProfile());
            });
            _mapper = mapperConfig.CreateMapper();

            // Configure InMemory Database
            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "SqlApiTestDb")
                .Options;

            // Configure Mock Logger
            _mockLogger = new Mock<ILogger<PeopleController>>();
        }

        private async Task<ApplicationDbContext> GetDatabaseContext()
        {
            var context = new ApplicationDbContext(_dbOptions);
            await context.Database.EnsureDeletedAsync(); // Clean slate for each test
            await context.Database.EnsureCreatedAsync();
            if (!await context.People.AnyAsync())
            {
                context.People.AddRange(
                    new Person { Id = 1, Name = "Alice", Age = 30, Email = "alice@example.com" },
                    new Person { Id = 2, Name = "Bob", Age = 25, Email = "bob@example.com" }
                );
                await context.SaveChangesAsync();
            }
            return context;
        }

        [Fact]
        public async Task GetPeople_ShouldReturnAllPeople()
        {
            // Arrange
            using var context = await GetDatabaseContext();
            var controller = new PeopleController(context, _mapper, _mockLogger.Object);

            // Act
            var result = await controller.GetPeople();

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedDtos = Assert.IsAssignableFrom<IEnumerable<PersonDto>>(actionResult.Value);
            returnedDtos.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetPerson_WithValidId_ShouldReturnPerson()
        {
            // Arrange
            using var context = await GetDatabaseContext();
            var controller = new PeopleController(context, _mapper, _mockLogger.Object);

            // Act
            var result = await controller.GetPerson(1);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedDto = Assert.IsType<PersonDto>(actionResult.Value);
            returnedDto.Id.Should().Be(1);
            returnedDto.Name.Should().Be("Alice");
        }

        [Fact]
        public async Task GetPerson_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            using var context = await GetDatabaseContext();
            var controller = new PeopleController(context, _mapper, _mockLogger.Object);

            // Act
            var result = await controller.GetPerson(99);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostPerson_WithValidDto_ShouldCreatePerson()
        {
            // Arrange
            using var context = await GetDatabaseContext();
            var controller = new PeopleController(context, _mapper, _mockLogger.Object);
            var newPersonDto = new PersonDto { Name = "Charlie", Age = 40, Email = "charlie@example.com" };

            // Act
            var result = await controller.PostPerson(newPersonDto);

            // Assert
            var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdDto = Assert.IsType<PersonDto>(actionResult.Value);
            createdDto.Name.Should().Be("Charlie");
            (await context.People.CountAsync()).Should().Be(3);
        }

        [Fact]
        public async Task PutPerson_WithValidDto_ShouldUpdatePerson()
        {
            // Arrange
            using var context = await GetDatabaseContext();
            var controller = new PeopleController(context, _mapper, _mockLogger.Object);
            var updatedDto = new PersonDto { Id = 1, Name = "Alice Smith", Age = 31, Email = "alice.smith@example.com" };

            // Act
            var result = await controller.PutPerson(1, updatedDto);

            // Assert
            Assert.IsType<NoContentResult>(result);
            var personInDb = await context.People.FindAsync(1);
            personInDb!.Name.Should().Be("Alice Smith");
            personInDb.Age.Should().Be(31);
        }

        [Fact]
        public async Task DeletePerson_WithValidId_ShouldRemovePerson()
        {
            // Arrange
            using var context = await GetDatabaseContext();
            var controller = new PeopleController(context, _mapper, _mockLogger.Object);

            // Act
            var result = await controller.DeletePerson(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
            (await context.People.CountAsync()).Should().Be(1);
            (await context.People.FindAsync(1)).Should().BeNull();
        }
    }
}
