using ClyvoVet.Api.Exceptions;
using ClyvoVet.Api.Models;
using ClyvoVet.Api.Repositories.Interfaces;
using ClyvoVet.Api.Services;
using ClyvoVet.Api.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ClyvoVet.Api.Tests.Unit;

/// <summary>
/// O contrato central da saúde preditiva: a IA é um REDATOR OPCIONAL, nunca uma
/// dependência. Estes testes provam os três caminhos — cache válido não gera
/// nada; IA configurada e saudável redige; IA ausente, quebrada ou respondendo
/// lixo cai nas regras — e que o aviso de Telegram é cortesia (falha dele não
/// falha o parecer).
/// </summary>
public class SaudePreditivaServiceTests
{
    private readonly Mock<IAnimalRepository> _animais = new();
    private readonly Mock<IBaseDoencaRepository> _base = new();
    private readonly Mock<IParecerIaRepository> _pareceres = new();
    private readonly Mock<IOciGenerativeAiClient> _ia = new();
    private readonly Mock<ITutorTelegramRepository> _tutorTelegram = new();
    private readonly Mock<ITelegramService> _telegram = new();

    private SaudePreditivaService Servico() => new(
        _animais.Object, _base.Object, _pareceres.Object, _ia.Object,
        _tutorTelegram.Object, _telegram.Object,
        NullLogger<SaudePreditivaService>.Instance);

    private static Animal Bolinha() => new()
    {
        Id = "animal-1",
        Nome = "Bolinha",
        Especie = "Cachorro",
        Raca = "Labrador Retriever",
        DataNascimento = DateTime.UtcNow.AddYears(-6),
        TutorId = "tutor-1",
        Tutor = new Tutor { Id = "tutor-1", Nome = "Lucas" },
        RacaId = "raca-1",
        RacaCatalogo = new Raca
        {
            Id = "raca-1", Especie = "CAO", Nome = "Labrador Retriever",
            Chave = "labrador-retriever", Ativo = true,
        },
    };

    private static BaseDoenca Linha(string chave, string codigo, string nome, string categoria, int casos) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Especie = "CAO",
        RacaTexto = "labrador_retriever",
        RacaChave = chave,
        DoencaCodigo = codigo,
        DoencaNome = nome,
        Categoria = categoria,
        Casos = casos,
        Controles = 10,
        Doi = "10.5061/dryad.266k4",
    };

    private void ArmarBasePadrao()
    {
        _animais.Setup(r => r.GetByIdAsync("animal-1")).ReturnsAsync(Bolinha());
        _pareceres.Setup(r => r.GetByAnimalIdAsync("animal-1")).ReturnsAsync((ParecerIa?)null);
        _base.Setup(r => r.GetByEspecieAsync("CAO")).ReturnsAsync(
        [
            Linha("labrador-retriever", "MCT", "Mastocitoma", "ONCOLOGICA", 306),
            Linha("labrador-retriever", "CLLD", "Doenca do ligamento cruzado cranial", "ORTOPEDICA", 25),
            Linha("golden-retriever", "lymphoma", "Linfoma", "ONCOLOGICA", 40),
        ]);
        _tutorTelegram.Setup(r => r.GetChatIdByTutorIdAsync(It.IsAny<string>())).ReturnsAsync((long?)null);
    }

    [Fact]
    public async Task SemIaConfigurada_GeraPelasRegras_ComOsNumerosDaRaca()
    {
        ArmarBasePadrao();
        _ia.SetupGet(i => i.Configurado).Returns(false);

        var parecer = await Servico().GetParecerAsync("animal-1");

        Assert.Equal("REGRAS", parecer.Origem);
        Assert.Null(parecer.Modelo);
        // Só as linhas da raça do Bolinha; o linfoma é de golden e não entra.
        Assert.Equal(2, parecer.Riscos.Count);
        Assert.Equal("Mastocitoma", parecer.Riscos[0].Doenca);
        Assert.Equal("ALTO", parecer.Riscos[0].Nivel);
        Assert.Contains("306", parecer.Riscos[0].Justificativa);
        Assert.NotEmpty(parecer.Recomendacoes);
        Assert.False(parecer.BaseLimitada);

        // e o resultado foi para o cache com validade real
        _pareceres.Verify(r => r.SalvarAsync(It.Is<ParecerIa>(p =>
            p.AnimalId == "animal-1" &&
            p.Origem == "REGRAS" &&
            p.ValidoAte > DateTime.UtcNow.AddDays(6))), Times.Once);
    }

    [Fact]
    public async Task CacheValido_NaoGeraNadaDeNovo()
    {
        _animais.Setup(r => r.GetByIdAsync("animal-1")).ReturnsAsync(Bolinha());
        _pareceres.Setup(r => r.GetByAnimalIdAsync("animal-1")).ReturnsAsync(new ParecerIa
        {
            Id = "p1", AnimalId = "animal-1", Origem = "IA", Modelo = "meta.llama-3.3-70b-instruct",
            Conteudo = """{"riscos":[{"doenca":"Mastocitoma","nivel":"ALTO"}],"recomendacoes":["Checkup"],"resumo":"ok"}""",
            GeradoEm = DateTime.UtcNow.AddDays(-1),
            ValidoAte = DateTime.UtcNow.AddDays(6),
        });

        var parecer = await Servico().GetParecerAsync("animal-1");

        Assert.Equal("IA", parecer.Origem);
        Assert.Single(parecer.Riscos);
        _ia.Verify(i => i.GerarTextoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _pareceres.Verify(r => r.SalvarAsync(It.IsAny<ParecerIa>()), Times.Never);
        // cache não dispara Telegram: o tutor já foi avisado quando o parecer nasceu
        _telegram.Verify(t => t.EnviarMensagemAsync(It.IsAny<long>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task IaRespondendoJsonComCercaDeMarkdown_ParecerSaiDaIa()
    {
        ArmarBasePadrao();
        _ia.SetupGet(i => i.Configurado).Returns(true);
        _ia.SetupGet(i => i.ModelId).Returns("meta.llama-3.3-70b-instruct");
        _ia.Setup(i => i.GerarTextoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                ```json
                {"riscos":[{"doenca":"Mastocitoma","categoria":"ONCOLOGICA","nivel":"ALTO","justificativa":"306 casos na base."}],
                 "recomendacoes":["Checkup anual com palpacao de pele."],
                 "resumo":"Atencao preventiva a mastocitoma."}
                ```
                """);

        var parecer = await Servico().GetParecerAsync("animal-1");

        Assert.Equal("IA", parecer.Origem);
        Assert.Equal("meta.llama-3.3-70b-instruct", parecer.Modelo);
        Assert.Single(parecer.Riscos);
        Assert.Equal("Mastocitoma", parecer.Riscos[0].Doenca);
    }

    [Fact]
    public async Task IaForaDoAr_CaiNasRegras_SemPropagarErro()
    {
        ArmarBasePadrao();
        _ia.SetupGet(i => i.Configurado).Returns(true);
        _ia.Setup(i => i.GerarTextoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("OCI 503"));

        var parecer = await Servico().GetParecerAsync("animal-1");

        Assert.Equal("REGRAS", parecer.Origem);
        Assert.NotEmpty(parecer.Riscos);
    }

    [Fact]
    public async Task IaRespondendoLixo_CaiNasRegras()
    {
        ArmarBasePadrao();
        _ia.SetupGet(i => i.Configurado).Returns(true);
        _ia.Setup(i => i.GerarTextoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Claro! Aqui vão algumas dicas para o seu pet...");

        var parecer = await Servico().GetParecerAsync("animal-1");

        Assert.Equal("REGRAS", parecer.Origem);
    }

    [Fact]
    public async Task ParecerNovo_ComTelegramVinculado_AvisaOTutor()
    {
        ArmarBasePadrao();
        _ia.SetupGet(i => i.Configurado).Returns(false);
        _tutorTelegram.Setup(r => r.GetChatIdByTutorIdAsync("tutor-1")).ReturnsAsync(4242L);

        await Servico().GetParecerAsync("animal-1");

        _telegram.Verify(t => t.EnviarMensagemAsync(4242L,
            It.Is<string>(m => m.Contains("Bolinha"))), Times.Once);
    }

    [Fact]
    public async Task TelegramQuebrado_NaoDerrubaOParecer()
    {
        ArmarBasePadrao();
        _ia.SetupGet(i => i.Configurado).Returns(false);
        _tutorTelegram.Setup(r => r.GetChatIdByTutorIdAsync("tutor-1")).ReturnsAsync(4242L);
        _telegram.Setup(t => t.EnviarMensagemAsync(It.IsAny<long>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("telegram fora do ar"));

        var parecer = await Servico().GetParecerAsync("animal-1");

        Assert.NotEmpty(parecer.Riscos);
    }

    [Fact]
    public async Task EspecieSemDadosNaBase_MarcaBaseLimitada_EAindaResponde()
    {
        var jabuti = Bolinha();
        jabuti.Especie = "Reptil";
        jabuti.Raca = "Jabuti";
        jabuti.RacaCatalogo = null;
        _animais.Setup(r => r.GetByIdAsync("animal-1")).ReturnsAsync(jabuti);
        _pareceres.Setup(r => r.GetByAnimalIdAsync("animal-1")).ReturnsAsync((ParecerIa?)null);
        _base.Setup(r => r.GetByEspecieAsync("REPTIL")).ReturnsAsync([]);
        _tutorTelegram.Setup(r => r.GetChatIdByTutorIdAsync(It.IsAny<string>())).ReturnsAsync((long?)null);
        _ia.SetupGet(i => i.Configurado).Returns(false);

        var parecer = await Servico().GetParecerAsync("animal-1");

        Assert.True(parecer.BaseLimitada);
        Assert.NotEmpty(parecer.Recomendacoes);
    }

    [Fact]
    public async Task AnimalInexistente_E404()
    {
        _animais.Setup(r => r.GetByIdAsync("fantasma")).ReturnsAsync((Animal?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Servico().GetParecerAsync("fantasma"));
    }

    [Theory]
    [InlineData("Cachorro", "CAO")]
    [InlineData("GATO", "GATO")]
    [InlineData("Passaro", "AVE")]
    [InlineData("Reptil", "REPTIL")]
    [InlineData("Roedor", "ROEDOR")]
    [InlineData("Dinossauro", null)]
    [InlineData(null, null)]
    public void CodigoDoCatalogo_TraduzOVocabularioDoAnimal(string? especie, string? esperado)
    {
        Assert.Equal(esperado, SaudePreditivaService.CodigoDoCatalogo(especie));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sem json nenhum")]
    [InlineData("{\"riscos\":[]}")]
    [InlineData("{\"riscos\":[{\"nivel\":\"ALTO\"}]}")] // risco sem doença
    public void RespostasInvalidasDaIa_ViramNull(string? texto)
    {
        Assert.Null(SaudePreditivaService.TentarLerRespostaDaIa(texto));
    }

    // ------------------------------------------------------------------
    // A frase de abertura do card
    //
    // Ela é o que o tutor lê primeiro e o que o convence (ou não) a tocar no
    // botão. Por isso é testada como comportamento, e não como texto solto: o
    // que importa é que ela cite o pet, acerte o pronome e — o principal — NÃO
    // afirme se basear em dado que o animal não tem.
    // ------------------------------------------------------------------

    [Fact]
    public void Convite_CitaOPetPeloNome_EConvidaAAgendar()
    {
        var frase = SaudePreditivaService.MontarConvite(Bolinha(), "Mastocitoma");

        Assert.Contains("Bolinha", frase);
        Assert.Contains("mastocitoma", frase);
        Assert.Contains("checkup preventivo", frase);
        Assert.EndsWith("?", frase);
    }

    [Fact]
    public void Convite_UsaOPronomeDoSexoCadastrado()
    {
        var macho = Bolinha();
        macho.Sexo = "MACHO";
        var femea = Bolinha();
        femea.Sexo = "FEMEA";

        Assert.Contains("ele tem mais chance", SaudePreditivaService.MontarConvite(macho, "Linfoma"));
        Assert.Contains("ela tem mais chance", SaudePreditivaService.MontarConvite(femea, "Linfoma"));
    }

    /// <summary>
    /// O ponto que mais importa: um animal sem data de nascimento não pode
    /// ouvir "pela idade do seu pet". Seria inventar o fundamento da própria
    /// recomendação — e é exatamente o tipo de detalhe que derruba a confiança
    /// do tutor no resto do card.
    /// </summary>
    [Fact]
    public void Convite_SemDataDeNascimento_NaoAfirmaSeBasearNaIdade()
    {
        var semIdade = Bolinha();
        semIdade.DataNascimento = null;

        var frase = SaudePreditivaService.MontarConvite(semIdade, "Mastocitoma");

        Assert.DoesNotContain("idade", frase);
        Assert.Contains("Pela raça de Bolinha", frase);
    }

    [Fact]
    public void Convite_SemRacaNemIdade_NaoAfirmaBaseNenhuma()
    {
        var semNada = Bolinha();
        semNada.DataNascimento = null;
        semNada.Raca = null;
        semNada.RacaCatalogo = null;

        var frase = SaudePreditivaService.MontarConvite(semNada, "Mastocitoma");

        Assert.DoesNotContain("idade", frase);
        Assert.DoesNotContain("raça", frase);
        Assert.Contains("No perfil de Bolinha", frase);
    }

    /// <summary>Sem risco mapeado o convite continua existindo — o checkup vale de todo jeito.</summary>
    [Fact]
    public void Convite_SemDoenca_AindaConvidaAoCheckup()
    {
        var frase = SaudePreditivaService.MontarConvite(Bolinha(), null);

        Assert.Contains("Bolinha", frase);
        Assert.Contains("checkup preventivo", frase);
    }

    /// <summary>O parecer das regras entrega a frase pronta, não um rótulo clínico.</summary>
    [Fact]
    public async Task ParecerPelasRegras_TrazOConviteNoResumo()
    {
        ArmarBasePadrao();
        _ia.SetupGet(i => i.Configurado).Returns(false);

        var parecer = await Servico().GetParecerAsync("animal-1");

        Assert.NotNull(parecer.Resumo);
        Assert.Contains("Bolinha", parecer.Resumo!);
        Assert.Contains("checkup preventivo", parecer.Resumo!);
    }

    /// <summary>
    /// A IA pode devolver riscos e esquecer o resumo. O card abre por essa
    /// frase: sem ela, sobraria um cabeçalho solto e um botão sem contexto.
    /// </summary>
    [Fact]
    public async Task IaSemResumo_OConviteEntraPelasRegras()
    {
        ArmarBasePadrao();
        _ia.SetupGet(i => i.Configurado).Returns(true);
        _ia.SetupGet(i => i.ModelId).Returns("meta.llama-3.3-70b-instruct");
        _ia.Setup(i => i.GerarTextoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"riscos":[{"doenca":"Mastocitoma","nivel":"ALTO"}],"recomendacoes":["Checkup."]}""");

        var parecer = await Servico().GetParecerAsync("animal-1");

        Assert.Equal("IA", parecer.Origem);
        Assert.Contains("Bolinha", parecer.Resumo!);
        Assert.Contains("checkup preventivo", parecer.Resumo!);
    }
}
