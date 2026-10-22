using ClyvoVet.Application.Abstractions.Repositories;
using ClyvoVet.Domain.Entities;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ClyvoVet.Infrastructure.Mongo;

/// <summary>
/// O cache do parecer de IA no MongoDB (ADR-002): coleção <c>pareceres_ia</c>, <c>_id</c> = animal, um
/// documento por animal. Interface, entidade e serviço são os mesmos do repositório EF/MySQL.
/// </summary>
/// <remarks>
/// <b>Toda falha do Mongo é engolida de propósito</b> (o <c>catch</c> largo abaixo): isto é um cache, e um
/// cache que derruba a feature que ele acelera é pior que nenhum. Ler com falha = <i>cache miss</i> (o
/// serviço gera o parecer pelo caminho normal); gravar com falha = <c>Warning</c> (o usuário já tem o
/// parecer, só não ficou cacheado). O tempo que o driver espera pelo servidor é curto por configuração
/// (registro no <c>AddInfrastructure</c>) — o padrão dele é 30 s.
/// </remarks>
public class ParecerIaMongoRepository : IParecerIaRepository
{
    public const string NomeDaColecao = "pareceres_ia";

    private readonly IMongoCollection<BsonDocument> _colecao;
    private readonly ILogger<ParecerIaMongoRepository> _logger;

    public ParecerIaMongoRepository(IMongoDatabase banco, ILogger<ParecerIaMongoRepository> logger)
    {
        _colecao = banco.GetCollection<BsonDocument>(NomeDaColecao);
        _logger = logger;
    }

    public async Task<ParecerIa?> GetByAnimalIdAsync(string animalId)
    {
        try
        {
            var documento = await _colecao.Find(Builders<BsonDocument>.Filter.Eq("_id", animalId)).FirstOrDefaultAsync();
            return documento is null ? null : ParecerIaDocumento.ParaEntidade(documento);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MongoDB indisponível ao ler o parecer do animal {AnimalId}; seguindo como cache miss.", animalId);
            return null;
        }
    }

    public async Task SalvarAsync(ParecerIa parecer)
    {
        try
        {
            // Upsert pela chave natural (o animal): quem chama não precisa saber se já havia parecer.
            await _colecao.ReplaceOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", parecer.AnimalId),
                ParecerIaDocumento.ParaDocumento(parecer),
                new ReplaceOptions { IsUpsert = true });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MongoDB indisponível ao gravar o parecer do animal {AnimalId}; ele não ficou em cache.", parecer.AnimalId);
        }
    }
}
