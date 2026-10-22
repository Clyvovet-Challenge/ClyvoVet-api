using System.Reflection;
using ClyvoVet.Api.Listagem;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ClyvoVet.Api.Swagger;

/// <summary>
/// Acrescenta ao 200 das listagens a segunda mídia (o envelope), com o schema dele. É um filtro, e não
/// um segundo <c>[ProducesResponseType]</c>, porque dois deles para o mesmo status não se combinam: o
/// Swashbuckle mostrava só <c>application/json</c>, com o schema do envelope no lugar do array. Aqui o
/// <c>[ProducesResponseType]</c> descreve o array (<c>application/json</c>) e o filtro soma o resto.
/// </summary>
public sealed class EnvelopeHateoasOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var marca = context.MethodInfo.GetCustomAttribute<ListagemHateoasAttribute>();
        if (marca is null)
            return;

        if (operation.Responses is null
            || !operation.Responses.TryGetValue("200", out var resposta)
            || resposta is not OpenApiResponse ok)
            return;

        var envelope = context.SchemaGenerator.GenerateSchema(
            typeof(EnvelopeHateoas<>).MakeGenericType(marca.TipoDoItem), context.SchemaRepository);

        ok.Content ??= new Dictionary<string, OpenApiMediaType>();
        ok.Content[PreferenciaDeEnvelope.TipoDeMidia] = new OpenApiMediaType { Schema = envelope };
    }
}
