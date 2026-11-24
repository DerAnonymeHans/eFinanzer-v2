using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace eFinanzer.Common.Results;

/// <summary>
/// Adds the schema of every <see cref="Error"/> to the document. A response lists only one schema per status code, so
/// errors sharing a status code with another one would be missing otherwise, and generated clients could not tell them
/// apart by their <see cref="Error.Type"/>.
/// </summary>
public class ErrorSchemasDocumentTransformer : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var errorTypes = typeof(Error).Assembly.DefinedTypes
            .Where(type => type is { IsAbstract: false, IsGenericType: false } && type.IsAssignableTo(typeof(Error)))
            .OrderBy(type => type.Name);

        foreach (var type in errorTypes)
        {
            if (document.Components?.Schemas?.ContainsKey(type.Name) == true)
                continue;

            var schema = await context.GetOrCreateSchemaAsync(type, cancellationToken: cancellationToken);
            document.AddComponent(type.Name, schema);
        }
    }
}
