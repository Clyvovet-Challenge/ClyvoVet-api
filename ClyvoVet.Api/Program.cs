using System.Reflection;
using ClyvoVet.Api.Data;
using ClyvoVet.Api.Exceptions;
using ClyvoVet.Api.Repositories;
using ClyvoVet.Api.Repositories.Interfaces;
using ClyvoVet.Api.Services;
using ClyvoVet.Api.Services.Interfaces;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;   // OpenApiInfo, OpenApiContact (Microsoft.OpenApi 2.x)

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Clyvo Vet API",
        Version     = "v1",
        Description = """
            API REST de gerenciamento veterinário — domínio .NET (ASP.NET Core 8).

            **Tabelas gerenciadas por esta API:**
            - `t_clyvo_produto` — catálogo de produtos e serviços
            - `t_clyvo_sugestao_produto` — sugestão de produto para um animal
            - `t_clyvo_lembrete` — lembretes de cuidados por animal
            - `t_clyvo_evento_pet` — eventos públicos para pets

            **Lidas (somente) da API Java:**
            - `t_clyvo_animal` — para validar `animal_id` nas FKs
            - `t_clyvo_tutor` — incluído automaticamente via JOIN pelo EF Core

            **Banco de dados:** Oracle XE — FIAP
            """,
        Contact = new OpenApiContact
        {
            Name  = "Clyvo Vet — Equipe .NET",
            Email = "rm@fiap.com.br"
        }
    });

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
    options.RoutePrefix        = "swagger";
    options.DocumentTitle      = "Clyvo Vet API";
    options.DefaultModelsExpandDepth(-1); // esconde a seção Schemas por padrão
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
