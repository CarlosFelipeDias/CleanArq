using CleanArq.Domain.Entities;

namespace CleanArq.Domain.Tests.Entities;

public sealed class ProductTests
{
    [Fact]
    public void Create_ShouldSucceed_WhenStockIsGreaterThanZero()
    {
        var result = Product.Create(
            name: "Notebook",
            description: "Powerful notebook for development",
            price: 3500m,
            stock: 5,
            imageUrl: "https://example.com/notebook.jpg",
            categoryId: 1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Stock.Should().Be(5);
    }

    [Fact]
    public void Create_ShouldFail_WhenStockIsNegative()
    {
        var result = Product.Create(
            name: "Notebook",
            description: "Powerful notebook for development",
            price: 3500m,
            stock: -1,
            imageUrl: "https://example.com/notebook.jpg",
            categoryId: 1);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(Product.Stock));
    }

    [Fact]
    public void Create_ShouldSucceed_WhenImageUrlHasMaximumAllowedLength()
    {
        var imageUrl = $"https://example.com/{new string('a', 196)}.jpg";

        var result = Product.Create(
            name: "Notebook",
            description: "Powerful notebook for development",
            price: 3500m,
            stock: 5,
            imageUrl: imageUrl,
            categoryId: 1);

        imageUrl.Should().HaveLength(220);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ImageUrl.Should().Be(imageUrl);
    }

    [Fact]
    public void Create_ShouldFail_WhenImageUrlExceedsMaximumLength()
    {
        var imageUrl = $"https://example.com/{new string('a', 197)}.jpg";

        var result = Product.Create(
            name: "Notebook",
            description: "Powerful notebook for development",
            price: 3500m,
            stock: 5,
            imageUrl: imageUrl,
            categoryId: 1);

        imageUrl.Should().HaveLength(221);
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(Product.ImageUrl));
    }

    [Fact]
    public void Create_ShouldFail_WhenCategoryIdIsInvalid()
    {
        var result = Product.Create(
            name: "Notebook",
            description: "Powerful notebook for development",
            price: 3500m,
            stock: 5,
            imageUrl: "https://example.com/notebook.jpg",
            categoryId: 0);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(Product.CategoryId));
    }

    [Fact]
    public void Update_ShouldFail_WhenCategoryIdIsInvalid()
    {
        var productResult = Product.Create(
            name: "Notebook",
            description: "Powerful notebook for development",
            price: 3500m,
            stock: 5,
            imageUrl: "https://example.com/notebook.jpg",
            categoryId: 1);

        productResult.IsSuccess.Should().BeTrue();

        var result = productResult.Value!.Update(
            name: "Notebook",
            description: "Powerful notebook for development",
            price: 3500m,
            stock: 5,
            imageUrl: "https://example.com/notebook.jpg",
            categoryId: 0);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(Product.CategoryId));
    }
}
