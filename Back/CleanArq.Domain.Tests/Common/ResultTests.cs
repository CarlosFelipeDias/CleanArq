using CleanArq.Domain.Common;

namespace CleanArq.Domain.Tests.Common;

public sealed class ResultTests
{
    [Fact]
    public void Success_ShouldReturnFailure_WhenValueIsNull()
    {
        var result = Result.Success<string>(null!);

        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().Contain(error => error.PropertyName == "Value");
    }
}
