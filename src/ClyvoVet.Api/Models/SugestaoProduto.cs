namespace ClyvoVet.Api.Models;

public class SugestaoProduto
{
    public string Id { get; set; } = null!;
    public string AnimalId { get; set; } = null!;
    public Animal Animal { get; set; } = null!;
    public string ProdutoId { get; set; } = null!;
    public Produto Produto { get; set; } = null!;
    public string? Justificativa { get; set; }
    public DateOnly DataSugestao { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}
