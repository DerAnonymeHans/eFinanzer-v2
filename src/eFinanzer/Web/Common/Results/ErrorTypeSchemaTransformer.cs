using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace eFinanzer.Common.Results;

/// <summary>
/// Narrows <see cref="Error.Type"/> in the schema of each error to its class name, so generated clients get a literal
/// type to tell the errors apart
/// </summary>
public class ErrorTypeSchemaTransformer : IOpenApiSchemaTransformer
{
    private const string TypeProperty = "type";

    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        if (!type.IsAssignableTo(typeof(Error)) || schema.Properties?.ContainsKey(TypeProperty) != true)
            return Task.CompletedTask;

        // The base class is only used for parent errors, which can be any error
        var typeSchema = new OpenApiSchema { Type = JsonSchemaType.String };
        if (!type.IsAbstract)
            typeSchema.Enum = [JsonValue.Create(type.Name)];

        schema.Properties[TypeProperty] = typeSchema;
        schema.Required ??= new HashSet<string>();
        schema.Required.Add(TypeProperty);
        return Task.CompletedTask;
    }
}
