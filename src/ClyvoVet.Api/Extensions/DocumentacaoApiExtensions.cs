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

                    ### Recursos desta API

                    | Recurso | Rota base | Dados |
                    |---------|-----------|-------|
                    | Produtos | `/api/v1/produtos` | `t_clyvo_produto` |
                    | Eventos Pet | `/api/v1/eventos-pet` | `t_clyvo_evento_pet` |
                    | Lembretes | `/api/v1/lembretes` | `t_clyvo_lembrete` |
                    | Sugestões de Produto | `/api/v1/sugestoes-produto` | `t_clyvo_sugestao_produto` |
                    | Saúde Preditiva (IA) | `/api/v1/saude-preditiva` | parecer em cache no **MongoDB** (ou em `t_clyvo_parecer_ia`) |
                    | Widget de Saúde Preditiva | `/api/v1/widget-saude-preditiva` | `t_clyvo_predisposicao_saude` |
                    | Telegram | `/api/v1/telegram` | `t_clyvo_tutor_telegram` |

                    ### Tabelas da API Java (somente leitura)

                    | Tabela | Para quê |
                    |--------|----------|
                    | `t_clyvo_tutor` | Dono do animal, nas respostas enriquecidas e no escopo por tutor |
                    | `t_clyvo_animal` | Validação de `animalId` e perfil do animal na saúde preditiva |
                    | `t_clyvo_raca`, `t_clyvo_base_doencas` | Base de doenças por espécie e raça usada no parecer |

                    > O banco MySQL é **compartilhado com a API Java**, dona do schema (migrations Flyway).
                    > Esta API só escreve nas tabelas dela.

                    ### Autenticação

                    Clique em **Authorize** e informe as duas credenciais: o **Bearer** (access token emitido
                    pela API Java no login) e a **X-Api-Key**. O envio de mensagens do Telegram usa uma chave
                    própria. `/health*` e `/metrics` são públicos.

                    ---

                    **Dados:** MySQL (EF Core) · MongoDB (cache do parecer de IA)
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

            // Botão "Authorize" no Swagger — os endpoints de negócio exigem o header X-Api-Key
            // (Api:ApiKey). O cadeado só aparece neles (ver ApiKeySecurityDocumentFilter): o
            // envio do Telegram exige uma chave própria, diferente desta.
            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Name        = "X-Api-Key",
                Type        = SecuritySchemeType.ApiKey,
                In          = ParameterLocation.Header,
                Description = "Chave de API exigida pelos endpoints principais, junto com o token Bearer."
            });
            options.DocumentFilter<ApiKeySecurityDocumentFilter>();

            // O access token vem do login da API Java; o Swagger só o repassa no header.
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type         = SecuritySchemeType.Http,
                Scheme       = "bearer",
                BearerFormat = "JWT",
                Description  = "Access token emitido pela API Java no login. Cole só o token: o Swagger acrescenta o prefixo Bearer."
            });
            options.DocumentFilter<BearerSecurityDocumentFilter>();

            // Documenta o envelope opcional das listagens (application/vnd.clyvovet.hateoas+json).
            options.OperationFilter<EnvelopeHateoasOperationFilter>();
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
