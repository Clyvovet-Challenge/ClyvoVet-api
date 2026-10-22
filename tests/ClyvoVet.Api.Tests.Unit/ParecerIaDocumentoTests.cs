using System.Text.Json;
using ClyvoVet.Domain.Entities;
using ClyvoVet.Infrastructure.Mongo;
using MongoDB.Bson;

namespace ClyvoVet.Api.Tests.Unit;

/// <summary>
/// O mapeamento entre o <see cref="ParecerIa"/> (que carrega o conteúdo como TEXTO JSON, por causa da
/// coluna do MySQL) e o documento do Mongo (que guarda o conteúdo como SUBDOCUMENTO). Nenhum destes
/// testes precisa de servidor.
/// </summary>
public class ParecerIaDocumentoTests
{
    private const string ConteudoJson =
        """{"riscos":[{"doenca":"Displasia coxofemoral","categoria":"ORTOPEDICA","nivel":"ALTO","justificativa":"Raça predisposta — ação \"já\""}],"recomendacoes":["Consulta anual"],"resumo":null,"baseLimitada":false}""";

    private static readonly DateTime Gerado = new(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Valido = new(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc);

    private static ParecerIa Parecer(string? modelo = "meta.llama-3.3-70b-instruct") => new()
    {
        Id = "guid-que-so-existe-no-mysql",
        AnimalId = "animal-1",
        Origem = "IA",
        Modelo = modelo,
        Conteudo = ConteudoJson,
        GeradoEm = Gerado,
        ValidoAte = Valido,
    };

    private static string Normalizar(string json) => JsonSerializer.Serialize(JsonDocument.Parse(json).RootElement);

    [Fact]
    public void ParaDocumento_ParecerCompleto_UsaOAnimalIdComoIdEGravaOsCampos()
    {
        // Act
        var documento = ParecerIaDocumento.ParaDocumento(Parecer());

        // Assert
        Assert.Equal("animal-1", documento["_id"].AsString);
        Assert.Equal("IA", documento["origem"].AsString);
        Assert.Equal("meta.llama-3.3-70b-instruct", documento["modelo"].AsString);
        Assert.Equal(Gerado, documento["geradoEm"].ToUniversalTime());
        Assert.Equal(Valido, documento["validoAte"].ToUniversalTime());
        // O _id já é o animal, e o Guid do MySQL não faz sentido aqui: nenhum dos dois é gravado à parte.
        Assert.False(documento.Contains("animalId"));
        Assert.DoesNotContain("guid-que-so-existe-no-mysql", documento.ToString());
    }

    [Fact]
    public void ParaDocumento_Conteudo_VaiComoSubdocumentoEDeixaDeSerTexto()
    {
        // Act
        var documento = ParecerIaDocumento.ParaDocumento(Parecer());

        // Assert: é aqui que o "NoSQL" se justifica — dá para consultar dentro do conteúdo.
        Assert.Equal(BsonType.Document, documento["conteudo"].BsonType);
        Assert.Equal("Displasia coxofemoral", documento["conteudo"]["riscos"][0]["doenca"].AsString);
    }

    [Fact]
    public void ParaDocumento_ModeloNulo_GravaNulo()
    {
        var documento = ParecerIaDocumento.ParaDocumento(Parecer(modelo: null));

        Assert.True(documento["modelo"].IsBsonNull);
    }

    [Fact]
    public void ParaDocumento_DatasSemKind_TrataComoUtc()
    {
        // Arrange: o MySQL/EF devolve DateTime com Kind=Unspecified, e o valor é UTC.
        var parecer = Parecer();
        parecer.GeradoEm = DateTime.SpecifyKind(Gerado, DateTimeKind.Unspecified);
        parecer.ValidoAte = DateTime.SpecifyKind(Valido, DateTimeKind.Unspecified);

        // Act
        var documento = ParecerIaDocumento.ParaDocumento(parecer);

        // Assert: mesmo instante — sem deslocar pelo fuso da máquina.
        Assert.Equal(Gerado, documento["geradoEm"].ToUniversalTime());
        Assert.Equal(Valido, documento["validoAte"].ToUniversalTime());
    }

    [Fact]
    public void ParaDocumento_DatasEmHorarioLocal_ConverteParaOMesmoInstanteUtc()
    {
        // Arrange
        var parecer = Parecer();
        parecer.GeradoEm = Gerado.ToLocalTime();
        parecer.ValidoAte = Valido.ToLocalTime();

        // Act
        var documento = ParecerIaDocumento.ParaDocumento(parecer);

        // Assert: qualquer que seja o fuso da máquina, o instante é o mesmo.
        Assert.Equal(Gerado, documento["geradoEm"].ToUniversalTime());
        Assert.Equal(Valido, documento["validoAte"].ToUniversalTime());
    }

    [Fact]
    public void ParaDocumento_ConteudoQueNaoEJson_LancaFormatException()
    {
        var parecer = Parecer();
        parecer.Conteudo = "isto não é json";

        // O repositório captura isto e vira Warning (T3): cache nunca derruba a feature.
        Assert.Throws<FormatException>(() => ParecerIaDocumento.ParaDocumento(parecer));
    }

    [Fact]
    public void ParaEntidade_DocumentoGravado_RestauraOsCampos()
    {
        // Arrange
        var documento = ParecerIaDocumento.ParaDocumento(Parecer());

        // Act
        var volta = ParecerIaDocumento.ParaEntidade(documento);

        // Assert
        Assert.Equal("animal-1", volta.Id);
        Assert.Equal("animal-1", volta.AnimalId);
        Assert.Equal("IA", volta.Origem);
        Assert.Equal("meta.llama-3.3-70b-instruct", volta.Modelo);
        Assert.Equal(Gerado, volta.GeradoEm);
        Assert.Equal(Valido, volta.ValidoAte);
        Assert.Equal(DateTimeKind.Utc, volta.GeradoEm.Kind);
    }

    [Fact]
    public void ParaEntidade_Conteudo_VoltaComoTextoJsonEquivalente()
    {
        // Act
        var volta = ParecerIaDocumento.ParaEntidade(ParecerIaDocumento.ParaDocumento(Parecer()));

        // Assert: o SaudePreditivaService desserializa este texto exatamente como sempre desserializou.
        Assert.Equal(Normalizar(ConteudoJson), Normalizar(volta.Conteudo));
        var conteudo = JsonSerializer.Deserialize<ParecerConteudo>(
            volta.Conteudo, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true });
        Assert.Equal("Displasia coxofemoral", Assert.Single(conteudo!.Riscos).Doenca);
        Assert.False(conteudo.BaseLimitada);
    }

    [Fact]
    public void ParaEntidade_ModeloNulo_DevolveNull()
    {
        var volta = ParecerIaDocumento.ParaEntidade(ParecerIaDocumento.ParaDocumento(Parecer(modelo: null)));

        Assert.Null(volta.Modelo);
    }
}
