using ClyvoVet.Api.Enums;

namespace ClyvoVet.Api.Models;

public class Produto
{
    public string Id { get; set; } = null!;
    public string Nome { get; set; } = null!;
    public string? Descricao { get; set; }
    public CategoriaEnum Categoria { get; set; }
    public decimal? Preco { get; set; }
    public EspecieEnum EspecieIndicada { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }

    public ICollection<SugestaoProduto> Sugestoes { get; set; } = new List<SugestaoProduto>();
}
