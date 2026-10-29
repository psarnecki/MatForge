namespace MatForge.Application.Common;

public class BadRequestException(IDictionary<string, string[]> errors) : Exception("One or more validation errors occurred.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}
