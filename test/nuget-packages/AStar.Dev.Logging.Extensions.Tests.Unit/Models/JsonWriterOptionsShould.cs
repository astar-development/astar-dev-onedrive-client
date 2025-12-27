// C:\repos\M\astar-dev-logging-extensions\tests\AStar.Dev.Logging.Extensions.Tests.Unit\Models\JsonWriterOptionsTest.cs

using AStar.Dev.Logging.Extensions.Models;

namespace AStar.Dev.Logging.Extensions.Tests.Unit.Models;

[TestSubject(typeof(JsonWriterOptions))]
public class JsonWriterOptionsShould
{
    [Fact]
    public void Default_Indented_Should_Be_False()
    {
        var options = new JsonWriterOptions();

        var isIndented = options.Indented;

        isIndented.ShouldBeFalse();
    }

    [Fact]
    public void Indented_Set_To_True_Should_Return_True()
    {
        var options = new JsonWriterOptions { Indented = true };

        options.Indented.ShouldBeTrue();
    }

    [Fact]
    public void Indented_Set_To_False_Should_Return_False()
    {
        var options = new JsonWriterOptions { Indented = false };

        options.Indented.ShouldBeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Indented_Can_Be_Set_And_Retrieved(bool expectedValue)
    {
        var options = new JsonWriterOptions { Indented = expectedValue };

        options.Indented.ShouldBe(expectedValue);
    }
}
