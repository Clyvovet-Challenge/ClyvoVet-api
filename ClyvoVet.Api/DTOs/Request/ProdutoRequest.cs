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

    [Range(0, double.MaxValue, ErrorMessage = "O preço não pode ser negativo.")]
    public decimal? Preco { get; set; }

    [Required]
    public EspecieEnum EspecieIndicada { get; set; }

    /// <summary>
    /// Porte atendido: <c>Pequeno | Medio | Grande | Todos</c>. Omitido, vale
    /// <c>Todos</c> -- o produto passa a servir a qualquer porte, que era o
    /// comportamento antes desta coluna existir.
    /// </summary>
    public PorteEnum PorteIndicado { get; set; } = PorteEnum.Todos;

    public bool Ativo { get; set; } = true;
}
