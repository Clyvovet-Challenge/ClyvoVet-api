using ClyvoVet.Api.Enums;

namespace ClyvoVet.Api.DTOs.Response;

public class LembreteResponse
{
    public string Id { get; set; } = null!;
    public string AnimalId { get; set; } = null!;
    public string NomeAnimal { get; set; } = null!;
    public string Titulo { get; set; } = null!;
    public string? Descricao { get; set; }
    public TipoLembreteEnum Tipo { get; set; }
    public DateTime AgendadoEm { get; set; }
    public bool Recorrente { get; set; }

    /// <summary>"A cada quantos dias". NULO quando o lembrete nao repete.</summary>
    public int? IntervaloDias { get; set; }

    /// <summary>Fim da serie. NULO quando repete sem fim previsto.</summary>
    public DateTime? RepetirAte { get; set; }
    public StatusLembreteEnum Status { get; set; }
    public DateTime CriadoEm { get; set; }
}
