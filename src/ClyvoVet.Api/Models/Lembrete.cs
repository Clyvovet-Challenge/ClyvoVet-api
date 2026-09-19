using ClyvoVet.Api.Enums;

namespace ClyvoVet.Api.Models;

public class Lembrete
{
    public string Id { get; set; } = null!;
    public string AnimalId { get; set; } = null!;
    public Animal Animal { get; set; } = null!;
    public string Titulo { get; set; } = null!;
    public string? Descricao { get; set; }
    public TipoLembreteEnum Tipo { get; set; }
    public DateTime AgendadoEm { get; set; }
    /// <summary>
    /// DERIVADO de <see cref="IntervaloDias"/>, e mantido por compatibilidade: o
    /// app le este campo hoje. Quem decide e o intervalo — ver LembreteService.
    /// </summary>
    public bool Recorrente { get; set; }

    /// <summary>
    /// De quantos em quantos dias o lembrete volta. NULO = nao repete.
    /// </summary>
    public int? IntervaloDias { get; set; }

    /// <summary>
    /// Fim da serie. NULO = sem fim previsto (antipulgas mensal). Preenchido, e
    /// o "de x dia ate y dia": <see cref="AgendadoEm"/> comeca, este termina.
    /// </summary>
    public DateTime? RepetirAte { get; set; }
    public StatusLembreteEnum Status { get; set; }
    public DateTime CriadoEm { get; set; }
}
