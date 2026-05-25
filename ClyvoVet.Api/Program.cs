using System.Reflection;
using ClyvoVet.Api.Data;
using ClyvoVet.Api.Exceptions;
using ClyvoVet.Api.Repositories;
using ClyvoVet.Api.Repositories.Interfaces;
using ClyvoVet.Api.Services;
using ClyvoVet.Api.Services.Interfaces;
using ClyvoVet.Api.Swagger;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;                      // OpenApiInfo, OpenApiContact (Microsoft.OpenApi 2.x)
using Swashbuckle.AspNetCore.SwaggerUI;       // DocExpansion

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "🐾 Clyvo Vet API",
        Version = "v1",
        Description = """
            API REST de gerenciamento veterinário — domínio **.NET** (ASP.NET Core 8 + Oracle).

            ---

            ### Recursos gerenciados por esta API

            | Recurso | Rota base | Tabela Oracle |
            |---------|-----------|---------------|
            | Produtos | `/api/v1/produtos` | `t_clyvo_produto` |
            | Eventos Pet | `/api/v1/eventos-pet` | `t_clyvo_evento_pet` |
            | Lembretes | `/api/v1/lembretes` | `t_clyvo_lembrete` |
            | Sugestões de Produto | `/api/v1/sugestoes-produto` | `t_clyvo_sugestao_produto` |

            ### Tabelas lidas da API Java (somente consulta)

            | Tabela | Finalidade |
            |--------|-----------|
            | `t_clyvo_animal` | Validação de `animalId` nas FKs |
            | `t_clyvo_tutor` | JOIN automático pelo EF Core nas respostas enriquecidas |

            ---

            **Banco de dados:** Oracle FIAP — `oracle.fiap.com.br:1521/ORCL`
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
            var other         => [other ?? "Outros"]
        });

    // Descrições por grupo de tag
    options.DocumentFilter<TagDescriptionsDocumentFilter>();

    // Ordena as rotas pelo caminho relativo
    options.OrderActionsBy(api => $"{api.RelativePath}_{api.HttpMethod}");

    // Inclui comentários XML gerados a partir dos /// dos controllers
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleConnection")));

builder.Services.AddScoped<IProdutoRepository,         ProdutoRepository>();
builder.Services.AddScoped<ISugestaoProdutoRepository, SugestaoProdutoRepository>();
builder.Services.AddScoped<ILembreteRepository,        LembreteRepository>();
builder.Services.AddScoped<IEventoPetRepository,       EventoPetRepository>();
builder.Services.AddScoped<IAnimalRepository,          AnimalRepository>();

builder.Services.AddScoped<IProdutoService,         ProdutoService>();
builder.Services.AddScoped<ISugestaoProdutoService, SugestaoProdutoService>();
builder.Services.AddScoped<ILembreteService,        LembreteService>();
builder.Services.AddScoped<IEventoPetService,       EventoPetService>();

var app = builder.Build();

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

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        context.Response.ContentType = "application/json";

        context.Response.StatusCode = exception switch
        {
            NotFoundException   => StatusCodes.Status404NotFound,
            BadRequestException => StatusCodes.Status400BadRequest,
            _                   => StatusCodes.Status500InternalServerError
        };

        var message = exception switch
        {
            NotFoundException   e => e.Message,
            BadRequestException e => e.Message,
            _                     => "Erro interno no servidor."
        };

        await context.Response.WriteAsJsonAsync(new { error = message });
    });
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
