namespace ClyvoVet.Api.Swagger;

/// <summary>
/// Marca uma ação de listagem para o Swagger documentar também o envelope
/// (<c>application/vnd.clyvovet.hateoas+json</c>). <paramref name="tipoDoItem"/> é o DTO de cada item.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ListagemHateoasAttribute(Type tipoDoItem) : Attribute
{
    public Type TipoDoItem { get; } = tipoDoItem;
}
