using System.Reflection;
using ClyvoVet.Api.Swagger;
using ClyvoVet.Application.DTOs.Request;
using Microsoft.OpenApi;                      // OpenApiInfo, OpenApiContact (Microsoft.OpenApi 2.x)
using Swashbuckle.AspNetCore.SwaggerUI;       // DocExpansion

namespace ClyvoVet.Api.Extensions;

public static class DocumentacaoApiExtensions
{
    public static IServiceCollection AddDocumentacaoApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title   = "🐾 Clyvo Vet API",
                Version = "v1",
                Description = """
                    API REST de gerenciamento veterinário — domínio **.NET** (ASP.NET Core 8 + MySQL).

                    ---

                    ### Recursos gerenciados por esta API

                    | Recurso | Rota base | Tabela |
                    |---------|-----------|---------------|
                    | Produtos | `/api/v1/produtos` | `t_clyvo_produto` |
                    | Eventos Pet | `/api/v1/eventos-pet` | `t_clyvo_evento_pet` |
                    | Lembretes | `/api/v1/lembretes` | `t_clyvo_lembrete` |
                    | Sugestões de Produto | `/api/v1/sugestoes-produto` | `t_clyvo_sugestao_produto` |

                    ### Tabelas da API Java (somente consulta)

                    | Tabela | Finalidade |
                    |--------|-----------|
                    | `animal` | Validação de `animalId` nas FKs |
                    | `tutor` | JOIN automático pelo EF Core nas respostas enriquecidas |

                    > Nesta entrega (Sprint 3 — DevOps Tools & Cloud Computing), o banco é um Azure Database
                    > for MySQL Flexible Server **compartilhado com a API Java** — as tabelas `tutor` e `animal`
                    > seguem o schema definido pelas migrations Flyway do time de Java.

                    ---

                    **Banco de dados:** Azure Database for MySQL Flexible Server
                    """,
                Contact = new OpenApiContact
                {
                    Name  = "Clyvo Vet — Equipe .NET",
                    Email = "rm562312@fiap.com.br"
                }
            });

            // Agrupa por controller com nomes amigáveis
            options.TagActionsBy(api =>
                api.ActionDescriptor.RouteValues["controller"] switch
                {
                    "Produto"         => ["Produtos"],
                    "Lembrete"        => ["Lembretes"],
                    "EventoPet"       => ["Eventos Pet"],
                    "SugestaoProduto" => ["Sugestões de Produto"],
                    "WidgetSaudePreditiva" => ["Widget de Saúde Preditiva"],
                    "SaudePreditiva"  => ["Saúde Preditiva (IA)"],
                    "Telegram"        => ["Telegram"],
                    var other         => [other ?? "Outros"]
                });

            // Descrições por grupo de tag
            options.DocumentFilter<TagDescriptionsDocumentFilter>();

            // Ordena as rotas pelo caminho relativo
            options.OrderActionsBy(api => $"{api.RelativePath}_{api.HttpMethod}");

            // Um XML por projeto que tem documentação para o Swagger: os controllers estão na Api,
            // e os DTOs — com o summary de cada campo — na Application. Sem o segundo, o Swagger
            // continua respondendo 200 e a descrição dos campos some em silêncio.
            foreach (var assembly in new[] { Assembly.GetExecutingAssembly(), typeof(ProdutoRequest).Assembly })
            {
                var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
                if (File.Exists(xmlPath))
                    options.IncludeXmlComments(xmlPath);
            }

            // Botão "Authorize" no Swagger — os endpoints principais (Produto, Lembrete,
            // EventoPet, SugestaoProduto) exigem o header X-Api-Key. O cadeado só aparece
            // nesses endpoints (ver ApiKeySecurityOperationFilter) — Widget não exige
            // chave, e WhatsApp/Telegram exigem chaves próprias e diferentes desta.
            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Name        = "X-Api-Key",
                Type        = SecuritySchemeType.ApiKey,
                In          = ParameterLocation.Header,
                Description = "Chave de API exigida pelos endpoints principais da Sprint 3."
            });
            options.DocumentFilter<ApiKeySecurityDocumentFilter>();
        });

        return services;
    }

    public static WebApplication UseDocumentacaoApi(this WebApplication app)
    {
        // Swagger sempre ativo — professor pode testar sem cliente HTTP externo
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Clyvo Vet API v1");
            options.RoutePrefix               = "swagger";
            options.DocumentTitle             = "Clyvo Vet — API de Gestão Veterinária";
            options.DefaultModelsExpandDepth(-1);            // oculta seção Schemas por padrão
            options.DocExpansion(DocExpansion.List);          // lista endpoints recolhidos
            options.DisplayRequestDuration();                 // exibe tempo de resposta em cada chamada
            options.EnableFilter();                           // caixa de busca/filtro de rotas
            options.EnableDeepLinking();                      // URLs navegáveis por endpoint (bookmark)
            options.EnableTryItOutByDefault();                // "Try it out" já aberto por padrão
        });

        return app;
    }
}
