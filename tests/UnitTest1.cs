using Xunit;
using FluentAssertions;
using SqlAPI.DTOs;

namespace SqlAPI.Tests;

/// <summary>
/// Basic integration tests for the application
/// </summary>
public class BasicIntegrationTests
{
    [Fact]
    public void PersonDto_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var personDto = new PersonDto();

        // Assert
        personDto.Name.Should().BeEmpty();
        personDto.Email.Should().BeEmpty();
        personDto.Age.Should().Be(0);
    }

    [Fact]
    public void PersonSearchDto_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var searchDto = new PersonSearchDto();

        // Assert
        searchDto.PageNumber.Should().Be(1);
        searchDto.PageSize.Should().Be(10);
        searchDto.SortBy.Should().Be("Name");
        searchDto.SortDirection.Should().Be("asc");
    }

    [Fact]
    public void PagedResultDto_TotalPages_ShouldCalculateCorrectly()
    {
        // Arrange
        var pagedResult = new PagedResultDto<PersonDto>
        {
            TotalCount = 25,
            PageSize = 10,
            PageNumber = 2
        };

        // Act & Assert
        pagedResult.TotalPages.Should().Be(3);
        pagedResult.HasPreviousPage.Should().BeTrue();
        pagedResult.HasNextPage.Should().BeTrue();
    }
}
