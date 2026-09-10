using System.Text;
using System.Text.Json;
using ClyvoVet.Api.DTOs.Response;
using ClyvoVet.Api.Exceptions;
using ClyvoVet.Api.Models;
using ClyvoVet.Api.Repositories.Interfaces;
using ClyvoVet.Api.Services.Interfaces;

namespace ClyvoVet.Api.Services;

/// <summary>
/// A saúde preditiva com IA generativa — a evolução do widget por regras.
///
/// <para><b>O desenho em uma frase:</b> os FATOS vêm da base agregada de
/// doenças (t_clyvo_base_doencas, casos contados em datasets Dryad com DOI);
/// o LLM da OCI apenas REDIGE e prioriza em cima deles; o resultado fica em
/// cache por animal (t_clyvo_parecer_ia) por 7 dias; e quando a OCI está fora
/// do ar ou sem credencial, as MESMAS linhas da base geram um parecer
/// determinístico. A home nunca depende de nuvem para abrir.</para>
///
/// <para><b>Por que o cache importa tanto:</b> é ele que transforma "um LLM na
/// home" em uma chamada por animal por semana — controle de custo dos créditos
/// OCI e de latência (o card abre instantâneo nas visitas seguintes).</para>
///
/// <para><b>Telegram:</b> quando um parecer NOVO é gerado e o tutor tem o bot
/// vinculado, o resumo vai por mensagem — o mesmo conteúdo da home, no canal
/// que sobrou depois da saída do WhatsApp. Falha de envio não falha o parecer.</para>
/// </summary>
public class SaudePreditivaService : ISaudePreditivaService
{
    private static readonly TimeSpan Validade = TimeSpan.FromDays(7);

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly IAnimalRepository _animais;
    private readonly IBaseDoencaRepository _baseDoencas;
    private readonly IParecerIaRepository _pareceres;
    private readonly IOciGenerativeAiClient _ia;
    private readonly ITutorTelegramRepository _tutorTelegram;
    private readonly ITelegramService _telegram;
    private readonly ILogger<SaudePreditivaService> _logger;

    public SaudePreditivaService(
        IAnimalRepository animais,
        IBaseDoencaRepository baseDoencas,
        IParecerIaRepository pareceres,
        IOciGenerativeAiClient ia,
        ITutorTelegramRepository tutorTelegram,
        ITelegramService telegram,
        ILogger<SaudePreditivaService> logger)
    {
        _animais = animais;
        _baseDoencas = baseDoencas;
        _pareceres = pareceres;
        _ia = ia;
        _tutorTelegram = tutorTelegram;
        _telegram = telegram;
        _logger = logger;
    }

    public async Task<SaudePreditivaResponse> GetParecerAsync(string animalId, CancellationToken cancellationToken = default)
    {
        var animal = await _animais.GetByIdAsync(animalId)
            ?? throw new NotFoundException($"Animal com id {animalId} não encontrado.");

        var emCache = await _pareceres.GetByAnimalIdAsync(animalId);
        if (emCache is not null && emCache.ValidoAte > DateTime.UtcNow)
            return Montar(animal, emCache);

        var parecer = await GerarAsync(animal, cancellationToken);
        await _pareceres.SalvarAsync(parecer);
        await TentarAvisarNoTelegramAsync(animal, parecer);

        return Montar(animal, parecer);
    }

    // ------------------------------------------------------------------
    // Geração: IA primeiro, regras quando a IA não puder responder
    // ------------------------------------------------------------------

    private async Task<ParecerIa> GerarAsync(Animal animal, CancellationToken cancellationToken)
    {
        var especieCodigo = CodigoDoCatalogo(animal.Especie);
        var linhas = especieCodigo is null
            ? []
            : await _baseDoencas.GetByEspecieAsync(especieCodigo);

        var chave = animal.RacaCatalogo?.Chave;
        var daRaca = chave is null
            ? []
            : linhas.Where(l => l.RacaChave == chave).ToList();

        // AVE/REPTIL: os datasets são fauna selvagem (tentilhões de Galápagos,
        // resgate de répteis sem diagnóstico). ROEDOR: sem dados. O parecer sai
        // mesmo assim, mas dizendo isso — fingir cobertura seria pior que não ter.
        var baseLimitada = especieCodigo is null or "AVE" or "REPTIL" or "ROEDOR" || linhas.Count == 0;

        ParecerConteudo? conteudo = null;
        string? modelo = null;

        if (_ia.Configurado)
        {
            try
            {
                var texto = await _ia.GerarTextoAsync(
                    MontarPrompt(animal, daRaca, linhas, baseLimitada), cancellationToken);
                conteudo = TentarLerRespostaDaIa(texto);
                if (conteudo is not null)
                {
                    conteudo.BaseLimitada = baseLimitada;
                    modelo = _ia.ModelId;

                    // O modelo pode devolver riscos e esquecer o resumo. O card
                    // abre por essa frase: sem ela, sobra um cabeçalho solto.
                    if (string.IsNullOrWhiteSpace(conteudo.Resumo))
                        conteudo.Resumo = MontarConvite(animal, conteudo.Riscos.FirstOrDefault()?.Doenca);
                }
                else
                {
                    _logger.LogWarning(
                        "Resposta da OCI para o animal {AnimalId} não era o JSON esperado; caindo para as regras.",
                        animal.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "OCI Generative AI indisponível para o animal {AnimalId}; caindo para as regras.", animal.Id);
            }
        }

        conteudo ??= GerarPorRegras(daRaca, linhas, animal, baseLimitada);

        return new ParecerIa
        {
            AnimalId = animal.Id,
            Origem = modelo is null ? "REGRAS" : "IA",
            Modelo = modelo,
            Conteudo = JsonSerializer.Serialize(conteudo, Json),
            GeradoEm = DateTime.UtcNow,
            ValidoAte = DateTime.UtcNow.Add(Validade),
        };
    }

    /// <summary>
    /// O fallback determinístico: os maiores riscos da base, na ordem dos
    /// números — sem opinião, só contagem e recomendação por categoria.
    /// </summary>
    private static ParecerConteudo GerarPorRegras(
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
        var pronome = animal.Sexo?.Trim().ToUpperInvariant() == "FEMEA" ? "ela" : "ele";
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

    // ------------------------------------------------------------------
    // Prompt e leitura da resposta
    // ------------------------------------------------------------------

    private static string MontarPrompt(
        Animal animal, IReadOnlyList<BaseDoenca> daRaca, IReadOnlyList<BaseDoenca> daEspecie, bool baseLimitada)
    {
        var idade = CalcularIdadeAnos(animal.DataNascimento);
        var sb = new StringBuilder();

        var pronome = animal.Sexo?.Trim().ToUpperInvariant() == "FEMEA" ? "ela" : "ele";

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

    /// <summary>
    /// LLMs devolvem JSON com cercas de markdown, prefixos e afins — extrai o
    /// primeiro objeto e valida a forma. Qualquer coisa fora disso vira null,
    /// e null vira fallback: resposta ruim de IA nunca chega ao tutor.
    /// </summary>
    internal static ParecerConteudo? TentarLerRespostaDaIa(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return null;

        var inicio = texto.IndexOf('{');
        var fim = texto.LastIndexOf('}');
        if (inicio < 0 || fim <= inicio)
            return null;

        try
        {
            var conteudo = JsonSerializer.Deserialize<ParecerConteudo>(texto[inicio..(fim + 1)], Json);
            if (conteudo is null || conteudo.Riscos.Count == 0)
                return null;
            if (conteudo.Riscos.Any(r => string.IsNullOrWhiteSpace(r.Doenca)))
                return null;
            return conteudo;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // ------------------------------------------------------------------
    // Entrega
    // ------------------------------------------------------------------

    private SaudePreditivaResponse Montar(Animal animal, ParecerIa parecer)
    {
        ParecerConteudo conteudo;
        try
        {
            conteudo = JsonSerializer.Deserialize<ParecerConteudo>(parecer.Conteudo, Json) ?? new ParecerConteudo();
        }
        catch (JsonException)
        {
            // Cache corrompido não derruba a home; o próximo ciclo regenera.
            _logger.LogWarning("Parecer em cache do animal {AnimalId} ilegível; devolvendo vazio.", animal.Id);
            conteudo = new ParecerConteudo { BaseLimitada = true };
        }

        return new SaudePreditivaResponse
        {
            AnimalId = animal.Id,
            NomeAnimal = animal.Nome,
            Especie = animal.Especie,
            Raca = animal.Raca,
            IdadeAnos = CalcularIdadeAnos(animal.DataNascimento),
            Origem = parecer.Origem,
            Modelo = parecer.Modelo,
            GeradoEm = parecer.GeradoEm,
            ValidoAte = parecer.ValidoAte,
            Resumo = conteudo.Resumo,
            BaseLimitada = conteudo.BaseLimitada,
            Riscos = conteudo.Riscos.Select(r => new RiscoPreditivoResponse
            {
                Doenca = r.Doenca,
                Categoria = r.Categoria,
                Nivel = r.Nivel,
                Justificativa = r.Justificativa,
            }).ToList(),
            Recomendacoes = conteudo.Recomendacoes,
        };
    }

    private async Task TentarAvisarNoTelegramAsync(Animal animal, ParecerIa parecer)
    {
        try
        {
            var chatId = await _tutorTelegram.GetChatIdByTutorIdAsync(animal.TutorId);
            if (chatId is null)
                return;

            var conteudo = JsonSerializer.Deserialize<ParecerConteudo>(parecer.Conteudo, Json);
            var resumo = conteudo?.Resumo ?? "novo parecer disponível na home do app.";
            await _telegram.EnviarMensagemAsync(chatId.Value,
                $"🐾 Saúde preditiva de {animal.Nome}: {resumo} " +
                "Detalhes na home do app. (Orientação preventiva — não substitui consulta veterinária.)");
        }
        catch (Exception ex)
        {
            // O parecer é o produto; o aviso é cortesia. Telegram fora do ar
            // não pode transformar um GET da home em erro.
            _logger.LogWarning(ex, "Falha ao avisar o parecer do animal {AnimalId} no Telegram.", animal.Id);
        }
    }

    /// <summary>Do vocabulário livre do animal ('Cachorro') para o do catálogo/base ('CAO').</summary>
    internal static string? CodigoDoCatalogo(string? especie) =>
        especie?.Trim().ToUpperInvariant() switch
        {
            "CACHORRO" or "CAO" or "CÃO" or "CANINO" => "CAO",
            "GATO" or "FELINO" => "GATO",
            "PASSARO" or "PÁSSARO" or "AVE" => "AVE",
            "REPTIL" or "RÉPTIL" => "REPTIL",
            "ROEDOR" => "ROEDOR",
            _ => null,
        };

    private static decimal? CalcularIdadeAnos(DateTime? dataNascimento)
    {
        if (dataNascimento is null)
            return null;

        var dias = (DateTime.UtcNow.Date - dataNascimento.Value.Date).TotalDays;
        return Math.Round((decimal)(dias / 365.25), 1);
    }
}
