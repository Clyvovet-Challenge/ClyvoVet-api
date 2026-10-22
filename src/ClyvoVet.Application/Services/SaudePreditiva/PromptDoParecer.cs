using System.Text;
using ClyvoVet.Domain.Entities;

namespace ClyvoVet.Application.Services.SaudePreditiva;

/// <summary>
/// O texto que vai para o LLM. Os FATOS saem da base agregada de doenças; o
/// modelo só redige e prioriza em cima deles — por isso o prompt proíbe inventar.
/// </summary>
internal static class PromptDoParecer
{
    internal static string Montar(
        Animal animal, IReadOnlyList<BaseDoenca> daRaca, IReadOnlyList<BaseDoenca> daEspecie, bool baseLimitada)
    {
        var idade = PerfilDoAnimal.CalcularIdadeAnos(animal.DataNascimento);
        var sb = new StringBuilder();

        var pronome = PerfilDoAnimal.Pronome(animal);

        sb.AppendLine("Você é um assistente veterinário PREVENTIVO falando DIRETAMENTE com o tutor.");
        sb.AppendLine("Escreva em português do Brasil, em tom acolhedor e simples — nada de jargão clínico.");
        sb.AppendLine("Baseie-se EXCLUSIVAMENTE nos dados fornecidos abaixo — não invente doenças nem estatísticas.");
        sb.AppendLine("Responda SOMENTE com um JSON válido, sem markdown, neste formato exato:");
        sb.AppendLine("""{"riscos":[{"doenca":"...","categoria":"...","nivel":"ALTO|MEDIO|BAIXO","justificativa":"..."}],"recomendacoes":["..."],"resumo":"uma frase"}""");
        sb.AppendLine();
        sb.AppendLine("O campo \"resumo\" é o mais importante: é a frase que o tutor lê primeiro.");
        sb.AppendLine($"Ela DEVE citar o animal pelo nome ({animal.Nome}), usar o pronome \"{pronome}\",");
        sb.AppendLine("dizer no que a observação se baseia (raça e/ou idade), mencionar a principal");
        sb.AppendLine("condição e TERMINAR convidando a agendar um checkup preventivo. Exemplo do tom:");
        sb.AppendLine($"\"Pela raça e pela idade de {animal.Nome}, {pronome} tem mais chance de desenvolver");
        sb.AppendLine("<condição>. Que tal agendar um checkup preventivo?\"");
        sb.AppendLine("NÃO afirme basear-se na idade se a idade não foi informada abaixo.");
        sb.AppendLine();
        sb.AppendLine("No máximo 4 riscos e 3 recomendações. Justificativas de uma frase, citando os números da base.");
        sb.AppendLine("Não dê diagnóstico nem dose de medicamento; recomende sempre acompanhamento veterinário.");
        sb.AppendLine();
        sb.AppendLine($"ANIMAL: {animal.Nome}; espécie {animal.Especie ?? "não informada"}; " +
                      $"raça {animal.Raca ?? "não informada"}; idade {(idade is null ? "não informada" : $"{idade} anos")}.");
        sb.AppendLine();

        if (daRaca.Count > 0)
        {
            sb.AppendLine("BASE DE REFERÊNCIA — registros da RAÇA deste animal (doença: casos/controles):");
            foreach (var l in daRaca.Take(8))
                sb.AppendLine($"- {l.DoencaNome} ({l.Categoria}): {l.Casos} casos, {l.Controles} controles. Fonte DOI {l.Doi}.");
        }

        var daEspecieAgregada = daEspecie
            .GroupBy(l => l.DoencaCodigo)
            .Select(g => (Nome: g.First().DoencaNome, Categoria: g.First().Categoria, Casos: g.Sum(x => x.Casos)))
            .OrderByDescending(x => x.Casos)
            .Take(6)
            .ToList();
        if (daEspecieAgregada.Count > 0)
        {
            sb.AppendLine("BASE DE REFERÊNCIA — agregado da ESPÉCIE (doença: casos):");
            foreach (var l in daEspecieAgregada)
                sb.AppendLine($"- {l.Nome} ({l.Categoria}): {l.Casos} casos.");
        }

        if (baseLimitada)
            sb.AppendLine("AVISO: a base cobre pouco esta espécie/raça. Diga isso no resumo e mantenha níveis BAIXO/MEDIO.");

        return sb.ToString();
    }
}
