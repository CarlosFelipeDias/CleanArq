using CleanArq.Domain.Entities;

namespace CleanArq.Domain.Tests.Entities;

public sealed class CategoryTests
{
    [Fact]
    public void Create_ShouldFail_WhenIdIsNegative()
    {
        var result = Category.Create(
            id: -1,
            name: "Electronics");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(Category.Id));
    }

    [Fact]
    public void Create_ShouldSucceed_WhenIdAndNameAreValid()
    {
        var result = Category.Create(
            id: 1,
            name: "Electronics");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(1);
        result.Value.Name.Should().Be("Electronics");
    }

    [Fact]
    public void UpdateName_ShouldFail_WhenNameIsWhitespace()
    {
        var categoryResult = Category.Create("Electronics");
        categoryResult.IsSuccess.Should().BeTrue();

        var category = categoryResult.Value!;
        var result = category.UpdateName("   ");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(Category.Name));
    }

    [Fact]
    public void UpdateName_ShouldSucceed_WhenNameIsValid()
    {
        var categoryResult = Category.Create("Electronics");
        categoryResult.IsSuccess.Should().BeTrue();

        var category = categoryResult.Value!;
        var result = category.UpdateName("Computers");

        result.IsSuccess.Should().BeTrue();
        category.Name.Should().Be("Computers");
    }
}
