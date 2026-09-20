using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ClyvoVet.Api.Swagger;

/// <summary>
/// Põe o cadeado do <c>Bearer</c> nas ações com <c>[Authorize]</c> (na ação ou no controller).
/// Precisa ser um DocumentFilter pelo mesmo motivo do <see cref="ApiKeySecurityDocumentFilter"/>:
/// só ele recebe o documento já montado, e sem isso o Swagger UI não resolve a referência.
///
/// <para>
/// O esquema entra no <b>mesmo</b> requisito que a <c>X-Api-Key</c> (registre este filtro
/// <b>depois</b> dele): num requisito só os dois esquemas valem juntos (E), que é o que a API
/// exige. Dois requisitos separados significariam OU, e o botão Authorize mandaria só um dos dois.
/// </para>
/// </summary>
public sealed class BearerSecurityDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        foreach (var apiDescription in context.ApiDescriptions)
        {
            var exigeToken = (apiDescription.ActionDescriptor.EndpointMetadata ?? [])
                .OfType<IAuthorizeData>().Any();
            if (!exigeToken)
                continue;

            var path = "/" + apiDescription.RelativePath?.TrimStart('/');
            if (!document.Paths.TryGetValue(path, out var pathItem))
                continue;

            var method = new HttpMethod(apiDescription.HttpMethod!);
            if (pathItem.Operations is null || !pathItem.Operations.TryGetValue(method, out var operation))
                continue;

            var requisito = operation.Security?.FirstOrDefault() ?? new OpenApiSecurityRequirement();
            requisito[new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>();
            operation.Security = [requisito];
        }
    }
}
