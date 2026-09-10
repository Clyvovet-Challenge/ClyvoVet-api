namespace ClyvoVet.Api.Models;

/// <summary>
/// O corpo estruturado do parecer — o que vai serializado em
/// <see cref="ParecerIa.Conteudo"/> e o que a IA é instruída a devolver.
/// Ter UMA forma para os dois caminhos (IA e regras) é o que permite ao app
/// renderizar o card sem saber quem redigiu.
/// </summary>
public class ParecerConteudo
{
    public List<RiscoPreditivo> Riscos { get; set; } = [];
    public List<string> Recomendacoes { get; set; } = [];
    public string? Resumo { get; set; }

    /// <summary>
    /// Verdadeiro quando a base de referência não cobre a espécie/raça de forma
    /// útil (aves e répteis dos datasets são fauna selvagem; roedores não têm
    /// dados). O app mostra isso ao tutor em vez de fingir cobertura.
    /// </summary>
    public bool BaseLimitada { get; set; }
}

public class RiscoPreditivo
{
    public string Doenca { get; set; } = null!;
    public string? Categoria { get; set; }

    /// <summary>ALTO, MEDIO ou BAIXO.</summary>
    public string? Nivel { get; set; }

    public string? Justificativa { get; set; }
}
