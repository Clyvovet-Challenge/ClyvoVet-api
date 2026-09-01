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
/// </summary>
public sealed class ApiKeySecurityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var protegido = context.ApiDescription.ActionDescriptor.FilterDescriptors.Any(descriptor =>
            descriptor.Filter is TypeFilterAttribute typeFilter &&
            typeFilter.ImplementationType == typeof(ApiKeyFilterAttribute) &&
            typeFilter.Arguments is [string configKey, ..] &&
            configKey == "Api:ApiKey");

        if (!protegido)
            return;

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            { new OpenApiSecuritySchemeReference("ApiKey"), new List<string>() }
        });
    }
}
