namespace ClyvoVet.Api.Models;

/// <summary>
/// Catálogo de raças (t_clyvo_raca, criado pela V14 do Flyway da API Java).
/// Somente leitura aqui — quem escreve é a outra API. A <see cref="Chave"/> é o
/// identificador compartilhado ('labrador-retriever', 'siames'): é por ela que a
/// saúde preditiva casa o animal com a base de doenças, sem comparar texto livre.
/// </summary>
public class Raca
{
    public string Id { get; set; } = null!;
    public string Especie { get; set; } = null!;
    public string Nome { get; set; } = null!;
    public string Chave { get; set; } = null!;
    public string? PorteTipico { get; set; }
    public bool Ativo { get; set; }
}
