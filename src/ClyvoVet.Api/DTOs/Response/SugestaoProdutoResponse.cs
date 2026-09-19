namespace ClyvoVet.Api.DTOs.Response;

public class SugestaoProdutoResponse
{
    public string Id { get; set; } = null!;
    public string AnimalId { get; set; } = null!;
    public string NomeAnimal { get; set; } = null!;
    public string ProdutoId { get; set; } = null!;
    public string NomeProduto { get; set; } = null!;
    public string? Justificativa { get; set; }
    public DateOnly DataSugestao { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}
