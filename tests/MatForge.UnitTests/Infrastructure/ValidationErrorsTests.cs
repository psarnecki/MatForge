using FluentAssertions;
using MatForge.Api.Infrastructure;

namespace MatForge.UnitTests.Infrastructure;

public class ValidationErrorsTests
{
    [Theory]
    [InlineData("FirstName", "firstName")]
    [InlineData("Rounds[0].TechniqueId", "rounds[0].techniqueId")]
    [InlineData("Fights[2].DurationSeconds", "fights[2].durationSeconds")]
    [InlineData("$.durationMinutes", "$.durationMinutes")]
    [InlineData("", "")]
    public void Key_is_converted_to_camel_case_in_every_segment(string key, string expected)
    {
        ValidationErrors.ToCamelCaseKey(key).Should().Be(expected);
    }
}
