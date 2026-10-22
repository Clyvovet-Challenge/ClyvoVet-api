using ClyvoVet.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace ClyvoVet.Infrastructure.Mongo;

/// <summary>
/// Ida e volta entre o <see cref="ParecerIa"/> e o documento do Mongo. Feito à mão, com
/// <see cref="BsonDocument"/>, para o Domain continuar sem nenhum pacote (atributos do driver na
/// entidade a contaminariam).
/// </summary>
/// <remarks>
/// <para>
/// <b>O conteúdo muda de forma.</b> Na entidade ele é um <i>texto JSON</i> (a coluna do MySQL é TEXT); no
/// Mongo vira <i>subdocumento</i>, e é isso que justifica o NoSQL aqui. Na volta é devolvido como texto,
/// e o serviço o desserializa como sempre fez.
/// </para>
/// <para>
/// <b><c>_id</c> é o animal</b>: um parecer por animal, como o <c>UNIQUE(animal_id)</c> de hoje. O Guid
/// da entidade só existia por causa da tabela e não é gravado.
/// </para>
/// </remarks>
internal static class ParecerIaDocumento
{
    // O modo padrão do ToJson() é o "Shell" (NumberLong(...), ISODate(...)), que não é JSON.
    private static readonly JsonWriterSettings JsonPuro = new() { OutputMode = JsonOutputMode.RelaxedExtendedJson };

    /// <exception cref="FormatException">O conteúdo não é JSON válido.</exception>
    public static BsonDocument ParaDocumento(ParecerIa parecer) => new()
    {
        { "_id", parecer.AnimalId },
        { "origem", parecer.Origem },
        { "modelo", parecer.Modelo is null ? BsonNull.Value : (BsonValue)parecer.Modelo },
        { "conteudo", BsonDocument.Parse(parecer.Conteudo) },
        { "geradoEm", ComoUtc(parecer.GeradoEm) },
        { "validoAte", ComoUtc(parecer.ValidoAte) },
    };

    // O MySQL/EF devolve DateTime com Kind=Unspecified, e o valor é UTC. O BsonDateTime trata
    // Unspecified como horário LOCAL e deslocaria o instante pelo fuso da máquina (verificado: +3 h
    // em UTC-3), então o Kind é declarado aqui, e não deixado ao driver.
    private static BsonDateTime ComoUtc(DateTime data) => new(data.Kind == DateTimeKind.Local
        ? data.ToUniversalTime()
        : DateTime.SpecifyKind(data, DateTimeKind.Utc));

    public static ParecerIa ParaEntidade(BsonDocument documento) => new()
    {
        Id = documento["_id"].AsString,
        AnimalId = documento["_id"].AsString,
        Origem = documento["origem"].AsString,
        Modelo = documento.GetValue("modelo", BsonNull.Value) is { IsBsonNull: false } modelo ? modelo.AsString : null,
        Conteudo = documento["conteudo"].AsBsonDocument.ToJson(JsonPuro),
        GeradoEm = documento["geradoEm"].ToUniversalTime(),
        ValidoAte = documento["validoAte"].ToUniversalTime(),
    };
}
