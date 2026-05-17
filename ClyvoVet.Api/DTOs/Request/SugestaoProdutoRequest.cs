using System.ComponentModel.DataAnnotations;

namespace ClyvoVet.Api.DTOs.Request;

public class SugestaoProdutoRequest
{
    [Required]
    public string AnimalId { get; set; } = null!;

    [Required]
    public string ProdutoId { get; set; } = null!;

    [MaxLength(1000)]
    public string? Justificativa { get; set; }

    public DateOnly? DataSugestao { get; set; }

    public bool Ativo { get; set; } = true;
}
