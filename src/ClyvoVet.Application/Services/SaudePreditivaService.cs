using Microsoft.Extensions.Logging;
using ClyvoVet.Application.Abstractions.External;
using System.Text.Json;
using ClyvoVet.Application.DTOs.Response;
using ClyvoVet.Domain.Exceptions;
using ClyvoVet.Domain.Entities;
using ClyvoVet.Application.Abstractions.Repositories;
using ClyvoVet.Application.Services.Interfaces;
using ClyvoVet.Application.Services.SaudePreditiva;

namespace ClyvoVet.Application.Services;

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
///
/// <para><b>Esta classe só coordena.</b> O prompt, a leitura da resposta da IA,
/// o parecer por regras e as leituras do animal moram em
/// <c>Services/SaudePreditiva/</c>, como funções puras testáveis sem mock.</para>
/// </summary>
public class SaudePreditivaService : ISaudePreditivaService
{
    private static readonly TimeSpan Validade = TimeSpan.FromDays(7);
    private static readonly TimeSpan ValidadeDoFallback = TimeSpan.FromHours(1);

    private readonly IAnimalRepository _animais;
    private readonly IBaseDoencaRepository _baseDoencas;
    private readonly IParecerIaRepository _pareceres;
    private readonly IOciGenerativeAiClient _ia;
    private readonly ITutorTelegramRepository _tutorTelegram;
    private readonly ITelegramService _telegram;
    private readonly TravasPorAnimal _travas;
    private readonly ILogger<SaudePreditivaService> _logger;

    public SaudePreditivaService(
        IAnimalRepository animais,
        IBaseDoencaRepository baseDoencas,
        IParecerIaRepository pareceres,
        IOciGenerativeAiClient ia,
        ITutorTelegramRepository tutorTelegram,
        ITelegramService telegram,
        TravasPorAnimal travas,
        ILogger<SaudePreditivaService> logger)
    {
        _animais = animais;
        _baseDoencas = baseDoencas;
        _pareceres = pareceres;
        _ia = ia;
        _tutorTelegram = tutorTelegram;
        _telegram = telegram;
        _travas = travas;
        _logger = logger;
    }

    public async Task<SaudePreditivaResponse> GetParecerAsync(string animalId, CancellationToken cancellationToken = default)
    {
        var animal = await _animais.GetByIdAsync(animalId)
            ?? throw new NotFoundException($"Animal com id {animalId} não encontrado.");

        var emCache = await _pareceres.GetByAnimalIdAsync(animalId);
        if (Valido(emCache))
            return Montar(animal, emCache!);

        // Um animal por vez, e o cache relido DENTRO da trava: quem esperou a vez encontra o
        // parecer que o primeiro acabou de gravar, e nao chama a OCI nem avisa o tutor de novo.
        using (await _travas.EntrarAsync(animalId, cancellationToken))
        {
            emCache = await _pareceres.GetByAnimalIdAsync(animalId);
            if (Valido(emCache))
                return Montar(animal, emCache!);

            var parecer = await GerarAsync(animal, cancellationToken);
            await _pareceres.SalvarAsync(parecer);
            await TentarAvisarNoTelegramAsync(animal, parecer);

            return Montar(animal, parecer);
        }
    }

    private static bool Valido(ParecerIa? parecer) => parecer is not null && parecer.ValidoAte > DateTime.UtcNow;

    // ------------------------------------------------------------------
    // Geração: IA primeiro, regras quando a IA não puder responder
    // ------------------------------------------------------------------

    private async Task<ParecerIa> GerarAsync(Animal animal, CancellationToken cancellationToken)
    {
        var especieCodigo = PerfilDoAnimal.CodigoDoCatalogo(animal.Especie);
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
                    PromptDoParecer.Montar(animal, daRaca, linhas, baseLimitada), cancellationToken);
                conteudo = LeitorDaRespostaDaIa.TentarLer(texto);
                if (conteudo is not null)
                {
                    conteudo.BaseLimitada = baseLimitada;
                    modelo = _ia.ModelId;

                    // O modelo pode devolver riscos e esquecer o resumo. O card
                    // abre por essa frase: sem ela, sobra um cabeçalho solto.
                    if (string.IsNullOrWhiteSpace(conteudo.Resumo))
                        conteudo.Resumo = ParecerPorRegras.MontarConvite(animal, conteudo.Riscos.FirstOrDefault()?.Doenca);
                }
                else
                {
                    _logger.LogWarning(
                        "Resposta da OCI para o animal {AnimalId} não era o JSON esperado; caindo para as regras.",
                        animal.Id);
                }
            }
            // Antes do catch generico: quem cancelou desistiu do parecer. Tratado como "OCI fora
            // do ar", o pedido abandonado ainda gravava as regras e avisava no Telegram.
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "OCI Generative AI indisponível para o animal {AnimalId}; caindo para as regras.", animal.Id);
            }
        }

        conteudo ??= ParecerPorRegras.Gerar(daRaca, linhas, animal, baseLimitada);

        // Regras no lugar de uma IA configurada sao tapa-buraco, e nao a resposta da semana:
        // com a validade cheia, um soluco da OCI deixava o animal sete dias sem IA. Sem OCI
        // configurada, tentar de novo nao muda nada, e as regras valem o prazo inteiro.
        var validade = modelo is null && _ia.Configurado ? ValidadeDoFallback : Validade;

        return new ParecerIa
        {
            AnimalId = animal.Id,
            Origem = modelo is null ? "REGRAS" : "IA",
            Modelo = modelo,
            Conteudo = JsonSerializer.Serialize(conteudo, LeitorDaRespostaDaIa.Json),
            GeradoEm = DateTime.UtcNow,
            ValidoAte = DateTime.UtcNow.Add(validade),
        };
    }

    // ------------------------------------------------------------------
    // Entrega
    // ------------------------------------------------------------------

    private SaudePreditivaResponse Montar(Animal animal, ParecerIa parecer)
    {
        ParecerConteudo conteudo;
        try
        {
            conteudo = JsonSerializer.Deserialize<ParecerConteudo>(parecer.Conteudo, LeitorDaRespostaDaIa.Json)
                ?? new ParecerConteudo();
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
            IdadeAnos = PerfilDoAnimal.CalcularIdadeAnos(animal.DataNascimento),
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

            var conteudo = JsonSerializer.Deserialize<ParecerConteudo>(parecer.Conteudo, LeitorDaRespostaDaIa.Json);
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
}
