namespace ClyvoVet.Api.Models;

public class Animal
{
    public string Id { get; set; } = null!;
    public string Nome { get; set; } = null!;
    public string? Especie { get; set; }
    public string? Raca { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string? Sexo { get; set; }
    public bool? Castrado { get; set; }

    public string TutorId { get; set; } = null!;
    public Tutor Tutor { get; set; } = null!;

    /// <summary>
    /// FK para o catálogo de raças (V14 do Flyway da Java). Anulável de
    /// nascença: raça fora do catálogo continua existindo como texto livre em
    /// <see cref="Raca"/>. Quando preenchida, <see cref="RacaCatalogo"/> dá a
    /// chave canônica — é ela que a saúde preditiva usa para casar com a base
    /// de doenças por igualdade, sem substring.
    /// </summary>
    public string? RacaId { get; set; }
    public Raca? RacaCatalogo { get; set; }
}
