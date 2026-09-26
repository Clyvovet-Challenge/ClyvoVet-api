using ClyvoVet.Domain.Entities;

namespace ClyvoVet.Application.Services.SaudePreditiva;

/// <summary>
/// O parecer determinístico: o que o card mostra quando a IA está fora do ar,
/// sem credencial ou devolveu algo ilegível. A home nunca depende de nuvem para abrir.
/// </summary>
internal static class ParecerPorRegras
{
    /// <summary>
    /// O fallback determinístico: os maiores riscos da base, na ordem dos
    /// números — sem opinião, só contagem e recomendação por categoria.
    /// </summary>
    internal static ParecerConteudo Gerar(
        IReadOnlyList<BaseDoenca> daRaca, IReadOnlyList<BaseDoenca> daEspecie, Animal animal, bool baseLimitada)
    {
        // Da raça quando há; da espécie inteira (agregada por doença) quando não.
        var candidatas = daRaca.Count > 0
            ? daRaca
                .GroupBy(l => l.DoencaCodigo)
                .Select(g => (Nome: g.First().DoencaNome, Categoria: g.First().Categoria, Casos: g.Sum(l => l.Casos)))
                .ToList()
            : daEspecie
                .GroupBy(l => l.DoencaCodigo)
                .Select(g => (Nome: g.First().DoencaNome, Categoria: g.First().Categoria, Casos: g.Sum(l => l.Casos)))
                .ToList();

        var top = candidatas.OrderByDescending(c => c.Casos).Take(4).ToList();
        var escopo = daRaca.Count > 0 ? "na raça" : "na espécie";

        var conteudo = new ParecerConteudo { BaseLimitada = baseLimitada };
        foreach (var c in top)
        {
            conteudo.Riscos.Add(new RiscoPreditivo
            {
                Doenca = c.Nome,
                Categoria = c.Categoria,
                Nivel = c.Casos >= 30 ? "ALTO" : c.Casos >= 10 ? "MEDIO" : "BAIXO",
                Justificativa = $"{c.Casos} casos registrados {escopo} na base de referência.",
            });
        }

        conteudo.Recomendacoes = top
            .Select(c => RecomendacaoPorCategoria(c.Categoria))
            .Distinct()
            .Take(3)
            .ToList();
        if (conteudo.Recomendacoes.Count == 0)
            conteudo.Recomendacoes.Add("Manter o checkup veterinário anual e as vacinas em dia.");

        conteudo.Resumo = MontarConvite(animal, top.Count > 0 ? top[0].Nome : null);

        return conteudo;
    }

    /// <summary>
    /// A frase que abre o card. Ela é dirigida ao tutor, cita o pet pelo nome e
    /// termina num convite — porque o card não existe para informar, existe para
    /// que alguém marque uma consulta.
    ///
    /// <para><b>Ela só afirma o que sabemos.</b> "Pela raça e pela idade" só
    /// aparece quando o animal tem raça no catálogo E data de nascimento; com um
    /// dos dois, a frase cita só esse; sem nenhum, não inventa base nenhuma. O
    /// pronome sai do sexo cadastrado. Dizer "com base na idade" de um animal sem
    /// data de nascimento seria inventar o fundamento da própria recomendação —
    /// e é o tipo de detalhe que o tutor percebe.</para>
    /// </summary>
    internal static string MontarConvite(Animal animal, string? doencaPrincipal)
    {
        var pronome = PerfilDoAnimal.Pronome(animal);
        var temRaca = animal.RacaCatalogo is not null || !string.IsNullOrWhiteSpace(animal.Raca);
        var temIdade = animal.DataNascimento is not null;

        var base_ = (temRaca, temIdade) switch
        {
            (true, true) => $"Pela raça e pela idade de {animal.Nome}",
            (true, false) => $"Pela raça de {animal.Nome}",
            (false, true) => $"Pela idade de {animal.Nome}",
            _ => $"No perfil de {animal.Nome}",
        };

        if (string.IsNullOrWhiteSpace(doencaPrincipal))
            return $"{base_} não encontramos predisposição mapeada na nossa base de referência. " +
                   "Que tal aproveitar e agendar o checkup preventivo?";

        var doenca = doencaPrincipal.Trim();
        // Nome de doença em caixa alta no meio da frase soaria como grito; a
        // primeira letra fica maiúscula só quando ela abre a oração.
        doenca = char.ToLowerInvariant(doenca[0]) + doenca[1..];

        return $"{base_}, {pronome} tem mais chance de desenvolver {doenca}. " +
               "Que tal agendar um checkup preventivo?";
    }

    private static string RecomendacaoPorCategoria(string? categoria) => categoria switch
    {
        "ORTOPEDICA" => "Avaliação ortopédica periódica; controle de peso e exercício de baixo impacto.",
        "ONCOLOGICA" => "Checkup anual com palpação de pele e linfonodos; investigar nódulos cedo.",
        "CARDIACA" => "Ausculta cardíaca anual; ecocardiograma se houver sopro, cansaço ou tosse.",
        "HEPATICA/VASCULAR" => "Exames de função hepática no checkup; atenção a apatia e perda de peso.",
        "NEUROLOGICA" => "Registrar episódios em vídeo e buscar avaliação neurológica.",
        "GASTROINTESTINAL" => "Atenção a diarreia persistente; avaliar com exames de fezes e sangue.",
        "RENAL" => "Creatinina e exame de urina no checkup anual, principalmente após os 7 anos.",
        "ENDOCRINA" => "Glicemia e T4 no checkup; atenção a sede e apetite fora do comum.",
        "METABOLICA" => "Painel bioquímico no checkup anual.",
        "OFTALMOLOGICA" => "Avaliação oftálmica se houver secreção ou vermelhidão persistente.",
        "INFECCIOSA" => "Vacinação em dia e avaliação ao notar lesões de pele ou apatia.",
        _ => "Manter o checkup veterinário anual e as vacinas em dia.",
    };
}
