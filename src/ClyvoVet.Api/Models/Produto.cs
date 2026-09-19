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

    /// <summary>
    /// O porte que o produto atende. <c>Todos</c> por padrao -- e o que
    /// preserva o comportamento de antes da coluna existir, quando nenhum
    /// produto declarava porte e todos apareciam para qualquer animal.
    /// </summary>
    public PorteEnum PorteIndicado { get; set; } = PorteEnum.Todos;
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }

    public ICollection<SugestaoProduto> Sugestoes { get; set; } = new List<SugestaoProduto>();
}
