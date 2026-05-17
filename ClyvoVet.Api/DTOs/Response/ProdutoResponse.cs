using ClyvoVet.Api.Enums;

namespace ClyvoVet.Api.DTOs.Response;

public class ProdutoResponse
{
    public string Id { get; set; } = null!;
    public string Nome { get; set; } = null!;
    public string? Descricao { get; set; }
    public CategoriaEnum Categoria { get; set; }
    public decimal? Preco { get; set; }
    public EspecieEnum EspecieIndicada { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}
