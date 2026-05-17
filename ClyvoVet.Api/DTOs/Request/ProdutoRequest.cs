using ClyvoVet.Api.Enums;
using System.ComponentModel.DataAnnotations;

namespace ClyvoVet.Api.DTOs.Request;

public class ProdutoRequest
{
    [Required]
    [MaxLength(200)]
    public string Nome { get; set; } = null!;

    [MaxLength(1000)]
    public string? Descricao { get; set; }

    [Required]
    public CategoriaEnum Categoria { get; set; }

    public decimal? Preco { get; set; }

    [Required]
    public EspecieEnum EspecieIndicada { get; set; }

    public bool Ativo { get; set; } = true;
}
