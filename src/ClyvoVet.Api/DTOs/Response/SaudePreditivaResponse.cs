namespace ClyvoVet.Api.DTOs.Response;

/// <summary>
/// O parecer de saúde preditiva que a home do app renderiza. Mesmo formato
/// para os dois caminhos de geração — <see cref="Origem"/> diz qual foi.
/// </summary>
public class SaudePreditivaResponse
{
    public string AnimalId { get; set; } = null!;
    public string NomeAnimal { get; set; } = null!;
    public string? Especie { get; set; }
    public string? Raca { get; set; }
    public decimal? IdadeAnos { get; set; }

    /// <summary>'IA' ou 'REGRAS'. O app mostra a diferença ao tutor.</summary>
    public string Origem { get; set; } = null!;
    public string? Modelo { get; set; }
    public DateTime GeradoEm { get; set; }
    public DateTime ValidoAte { get; set; }

    public string? Resumo { get; set; }
    public bool BaseLimitada { get; set; }
    public List<RiscoPreditivoResponse> Riscos { get; set; } = [];
    public List<string> Recomendacoes { get; set; } = [];

    /// <summary>
    /// Fixo por construção — não é o LLM quem decide se isto é diagnóstico.
    /// </summary>
    public string Disclaimer { get; set; } =
        "Orientação preventiva gerada a partir de bases de referência. Não é diagnóstico e não substitui consulta veterinária.";
}

public class RiscoPreditivoResponse
{
    public string Doenca { get; set; } = null!;
    public string? Categoria { get; set; }
    public string? Nivel { get; set; }
    public string? Justificativa { get; set; }
}
