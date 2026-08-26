using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ClyvoVet.Api.Swagger;

/// <summary>
/// Adiciona descrições aos grupos de tags exibidos no Swagger UI,
/// associando cada controller a uma descrição legível.
/// </summary>
public sealed class TagDescriptionsDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags = new HashSet<OpenApiTag>
        {
            new()
            {
                Name        = "Produtos",
                Description = "Catálogo de produtos e serviços veterinários. " +
                              "Suporta filtro por categoria e espécie indicada. " +
                              "Tabela: **`t_clyvo_produto`**."
            },
            new()
            {
                Name        = "Eventos Pet",
                Description = "Eventos públicos para pets: feiras, vacinações, workshops e afins. " +
                              "Suporta filtro por cidade, tipo e espécie-alvo. " +
                              "Tabela: **`t_clyvo_evento_pet`**."
            },
            new()
            {
                Name        = "Lembretes",
                Description = "Lembretes de cuidados vinculados a um animal. " +
                              "Requer `animalId` válido gerenciado pela **API Java**. " +
                              "Status é sempre iniciado como `Pendente`. " +
                              "Tabela: **`t_clyvo_lembrete`**."
            },
            new()
            {
                Name        = "Sugestões de Produto",
                Description = "Sugestões de produto vinculadas a um animal específico. " +
                              "Requer `animalId` (API Java) e `produtoId` (esta API) válidos. " +
                              "Resposta inclui `nomeAnimal` e `nomeProduto` enriquecidos. " +
                              "Tabela: **`t_clyvo_sugestao_produto`**."
            },
            new()
            {
                Name        = "Widget de Saúde Preditiva",
                Description = "Monta um card com predisposições de saúde relevantes para a " +
                              "espécie, raça e idade atual de um animal, sugerindo agendar " +
                              "consulta quando alguma condição relevante for encontrada. " +
                              "Tabela de referência: **`t_clyvo_predisposicao_saude`**."
            },
            new()
            {
                Name        = "WhatsApp",
                Description = "Ponto único de disparo de mensagens no WhatsApp, via Twilio Sandbox."
            },
        };
    }
}
