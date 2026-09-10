namespace ClyvoVet.Api.Models;

/// <summary>
/// O parecer de saúde preditiva persistido (t_clyvo_parecer_ia, V15). É um
/// cache com endereço: UM parecer por animal, com validade — o que transforma
/// "chamar um LLM na home" em uma chamada por animal por semana, e o que faz a
/// tela abrir instantânea nas visitas seguintes.
/// </summary>
public class ParecerIa
{
    public string Id { get; set; } = null!;
    public string AnimalId { get; set; } = null!;

    /// <summary>'IA' quando a OCI redigiu; 'REGRAS' quando o fallback determinístico respondeu.</summary>
    public string Origem { get; set; } = null!;

    /// <summary>Qual modelo redigiu (ex.: 'meta.llama-3.3-70b-instruct'); NULL nas regras.</summary>
    public string? Modelo { get; set; }

    /// <summary>O <see cref="ParecerConteudo"/> serializado em JSON.</summary>
    public string Conteudo { get; set; } = null!;

    public DateTime GeradoEm { get; set; }
    public DateTime ValidoAte { get; set; }
}
