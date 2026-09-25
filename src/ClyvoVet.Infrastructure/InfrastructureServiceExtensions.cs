using ClyvoVet.Application.Abstractions.External;
using ClyvoVet.Application.Abstractions.Repositories;
using ClyvoVet.Infrastructure.Background;
using ClyvoVet.Infrastructure.Data;
using ClyvoVet.Infrastructure.External;
using ClyvoVet.Infrastructure.HealthChecks;
using ClyvoVet.Infrastructure.Mongo;
using ClyvoVet.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using MySqlConnector;                        // MySqlConnectionStringBuilder (transitivo via Pomelo)
using Telegram.Bot;

namespace ClyvoVet.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var mysqlConnectionString = configuration.GetConnectionString("DefaultConnection");

        // TETO DE CONEXOES EXPLICITO
        // O padrao do MySqlConnector e 100 conexoes POR INSTANCIA. A API Java opera com o
        // padrao do HikariCP, 10 -- e ela tem 74 endpoints contra os 24 daqui. Esta API
        // podia abrir dez vezes mais conexao que a que recebe mais trafego. Nao era
        // dimensionamento: era o padrao que ninguem tocou.
        //
        // POR QUE AQUI E NAO NO appsettings.json
        // Na Azure a connection string vem de app setting e substitui a do arquivo. Um
        // teto escrito la nao chegaria a producao, que e exatamente onde ele importa.
        //
        // Quem precisar de outro valor sobrescreve por Database__MaxPoolSize, sem tocar em
        // codigo. Antes de subir, confirme o teto real do servidor com
        // SHOW VARIABLES LIKE 'max_connections' -- o Flexible Server e Standard_B1ms, tier
        // Burstable, e o limite dele nao se presume pelo tier.
        if (!string.IsNullOrWhiteSpace(mysqlConnectionString))
        {
            mysqlConnectionString = new MySqlConnectionStringBuilder(mysqlConnectionString)
            {
                MaximumPoolSize = configuration.GetValue<uint?>("Database:MaxPoolSize") ?? 15,
            }.ConnectionString;
        }

        // VERSAO FIXA, E NAO AutoDetect.
        //
        // ServerVersion.AutoDetect ABRE UMA CONEXAO com o banco durante a construcao do
        // host -- antes de a aplicacao existir. No App Service isso e uma dependencia de
        // BOOT: se o MySQL nao estiver alcancavel naquele instante (banco reiniciando,
        // regra de firewall ainda propagando, manutencao do Flexible Server), o processo
        // morre na inicializacao e o container entra em ciclo de restart. O sintoma no
        // portal e "Application Error", sem nada util no log da aplicacao -- porque a
        // aplicacao nunca chegou a subir para logar.
        //
        // Com a versao declarada, a app sobe mesmo com o banco fora e falha so na
        // requisicao que precisa dele -- que e onde o health check /health/ready ja sabe
        // reportar o problema.
        //
        // 8.0 e o que o azure/02-banco-mysql.sh provisiona (MYSQL_VERSION="8.0") e o que
        // o docker-compose local usa (mysql:8.0). Se um dia o servidor subir de versao,
        // esta linha muda junto -- e essa e a intencao: virar decisao explicita.
        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(mysqlConnectionString, new MySqlServerVersion(new Version(8, 0))));

        services.AddScoped<IProdutoRepository,         ProdutoRepository>();
        services.AddScoped<ISugestaoProdutoRepository, SugestaoProdutoRepository>();
        services.AddScoped<ILembreteRepository,        LembreteRepository>();
        services.AddScoped<IEventoPetRepository,       EventoPetRepository>();
        services.AddScoped<IAnimalRepository,          AnimalRepository>();
        services.AddScoped<IPredisposicaoSaudeRepository, PredisposicaoSaudeRepository>();
        services.AddScoped<ITutorTelegramRepository, TutorTelegramRepository>();
        services.AddScoped<IBaseDoencaRepository, BaseDoencaRepository>();

        // O parecer de IA é um cache: com Mongo:ConnectionString ele vai para o MongoDB (ADR-002);
        // sem ela (produção hoje, e o ambiente Testing) continua na tabela do MySQL.
        var usaMongo = services.AddMongoSeConfigurado(configuration, environment);
        if (!usaMongo)
            services.AddScoped<IParecerIaRepository, ParecerIaRepository>();

        // OCI Generative AI via HttpClient tipado. Sem credencial no ambiente o
        // cliente nasce com Configurado=false e a saude preditiva responde pelas
        // regras -- a home nunca depende da nuvem para abrir. Timeout curto pelo
        // mesmo motivo: melhor um fallback em 20s do que um card pendurado.
        services.AddHttpClient<IOciGenerativeAiClient, OciGenerativeAiClient>(client =>
            client.Timeout = TimeSpan.FromSeconds(20));

        services.AddSingleton<ITelegramBotClient>(sp =>
            new TelegramBotClient(sp.GetRequiredService<IConfiguration>()["Telegram:BotToken"]!));
        services.AddSingleton<ITelegramService, TelegramService>();

        if (!environment.IsEnvironment("Testing"))
        {
            services.AddHostedService<TelegramLinkListenerService>();
            services.AddHostedService<LembreteNotificationService>();
        }

        // Health Checks — "self" cobre liveness (processo respondendo), "mysql-database" cobre
        // readiness (Database.CanConnectAsync() contra o MySQL). "telegram-bot" verifica o
        // serviço externo integrado pela API, mas fica fora da tag "ready": uma instabilidade
        // nele não deveria tirar a API inteira de rotação, já que os outros recursos (Produto,
        // Lembrete, EventoPet, SugestaoProduto) continuam funcionando normalmente sem Telegram.
        //
        // O WHATSAPP SAIU DA API INTEIRA — decisão de escopo da Sprint 3, não detalhe
        // de sonda. O Telegram passou a ser o único canal de mensagem (lembretes e
        // saúde preditiva), e com ele saiu o Twilio do grafo de dependências. A sonda
        // do Twilio já tinha sido removida antes por outro motivo, documentado no
        // histórico: sem credencial no ambiente, o /health agregado respondia 503 com
        // a API 100% funcional.
        //
        // A OCI Generative AI NÃO tem sonda de propósito: ela é opcional por design
        // (fallback determinístico) e uma sonda a transformaria em dependência.
        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>(
                name: "mysql-database",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready", "database", "external"])
            .AddCheck<TelegramHealthCheck>("telegram-bot", tags: ["external"]);

        return services;
    }
}
