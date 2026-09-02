using ClyvoVet.Api.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ClyvoVet.Api.Swagger;

/// <summary>
/// Só mostra o cadeado do Swagger nos endpoints que realmente usam
/// [TypeFilter(typeof(ApiKeyFilterAttribute), Arguments = new object[] { "Api:ApiKey" })] —
/// evita sugerir que o Widget/WhatsApp/Telegram (que ou não exigem chave, ou
/// exigem uma chave diferente) aceitam a mesma "Api:ApiKey" do botão Authorize.
///
/// Precisa ser um DocumentFilter (não um OperationFilter): a referência ao security
/// scheme só serializa corretamente (vira {"ApiKey": []} em vez de um objeto vazio "{}")
/// quando é construída com acesso ao OpenApiDocument já montado — e só o DocumentFilter
/// recebe esse documento. Um OperationFilter roda cedo demais para isso e produz uma
/// referência "solta" que o Swagger UI não consegue resolver, então o botão "Authorize"
/// nunca anexa o header X-Api-Key nas chamadas de teste.
/// </summary>
public sealed class ApiKeySecurityDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        foreach (var apiDescription in context.ApiDescriptions)
        {
            var protegido = apiDescription.ActionDescriptor.FilterDescriptors.Any(descriptor =>
                descriptor.Filter is TypeFilterAttribute typeFilter &&
                typeFilter.ImplementationType == typeof(ApiKeyFilterAttribute) &&
                typeFilter.Arguments is [string configKey, ..] &&
                configKey == "Api:ApiKey");

            if (!protegido)
                continue;

            var path = "/" + apiDescription.RelativePath?.TrimStart('/');
            if (!document.Paths.TryGetValue(path, out var pathItem))
                continue;

            var method = new HttpMethod(apiDescription.HttpMethod!);
            if (!pathItem.Operations.TryGetValue(method, out var operation))
                continue;

            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    { new OpenApiSecuritySchemeReference("ApiKey", document), new List<string>() }
                }
            ];
        }
    }
}
