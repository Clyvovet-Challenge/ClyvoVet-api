using ClyvoVet.Api.Enums;
using System.ComponentModel.DataAnnotations;

namespace ClyvoVet.Api.DTOs.Request;

public class LembreteRequest
{
    [Required]
    public string AnimalId { get; set; } = null!;

    [Required]
    [MinLength(3)]
    [MaxLength(200)]
    public string Titulo { get; set; } = null!;

    [MaxLength(1000)]
    public string? Descricao { get; set; }

    [Required]
    public TipoLembreteEnum Tipo { get; set; }

    [Required]
    public DateTime AgendadoEm { get; set; }

    /// <summary>
    /// Aceito por compatibilidade e IGNORADO no calculo: quem manda e
    /// <see cref="IntervaloDias"/>. O service deriva este valor.
    /// </summary>
    public bool Recorrente { get; set; } = false;

    /// <summary>
    /// "A cada quantos dias". NULO = nao repete.
    /// </summary>
    /// <remarks>
    /// O teto de 365 nao e capricho: acima de um ano, "a cada N dias" deixa de
    /// ser lembrete e passa a ser agendamento — e o intervalo em dias erraria a
    /// data por causa do ano bissexto.
    /// </remarks>
    [Range(1, 365, ErrorMessage = "O intervalo deve ficar entre 1 e 365 dias.")]
    public int? IntervaloDias { get; set; }

    /// <summary>Fim da serie. NULO = repete sem fim previsto.</summary>
    public DateTime? RepetirAte { get; set; }

    public StatusLembreteEnum Status { get; set; } = StatusLembreteEnum.Pendente;
}
