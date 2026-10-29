using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace MatForge.Api.Infrastructure;

// Validation error keys in camelCase, like the JSON fields (rounds[0].techniqueId)
public static class ValidationErrors
{
    public static IActionResult CreateResponse(ActionContext context)
    {
        var problemDetailsFactory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = problemDetailsFactory.CreateValidationProblemDetails(context.HttpContext, context.ModelState);
        problem.Errors = ToCamelCaseKeys(problem.Errors);
        return new BadRequestObjectResult(problem);
    }

    public static IDictionary<string, string[]> ToCamelCaseKeys(IDictionary<string, string[]> errors) =>
        errors.ToDictionary(e => ToCamelCaseKey(e.Key), e => e.Value);

    public static string ToCamelCaseKey(string key) =>
        string.Join('.', key.Split('.').Select(ToCamelCaseSegment));

    private static string ToCamelCaseSegment(string segment)
    {
        var indexStart = segment.IndexOf('[');
        var name = indexStart < 0 ? segment : segment[..indexStart];
        var index = indexStart < 0 ? string.Empty : segment[indexStart..];
        return JsonNamingPolicy.CamelCase.ConvertName(name) + index;
    }
}
