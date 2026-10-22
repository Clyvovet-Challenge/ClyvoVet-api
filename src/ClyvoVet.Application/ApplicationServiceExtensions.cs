using ClyvoVet.Application.Security;
using ClyvoVet.Application.Services;
using ClyvoVet.Application.Services.SaudePreditiva;
using ClyvoVet.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ClyvoVet.Application;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IProdutoService,              ProdutoService>();
        services.AddScoped<ISugestaoProdutoService,      SugestaoProdutoService>();
        services.AddScoped<ILembreteService,             LembreteService>();
        services.AddScoped<IEventoPetService,            EventoPetService>();
        services.AddScoped<IWidgetSaudePreditivaService, WidgetSaudePreditivaService>();
        services.AddScoped<ISaudePreditivaService,       SaudePreditivaService>();
        services.AddScoped<IVinculoTelegramService,      VinculoTelegramService>();

        services.AddScoped<EscopoDoTutor>();

        // Singleton, e nao Scoped: quem GERA o convite e uma requisicao HTTP, quem o CONSOME
        // e o BackgroundService do Telegram. Se cada um recebesse a sua instancia, todo link
        // nasceria ja invalido.
        services.AddSingleton<VinculosPendentesDeTelegram>();

        // Singleton pelo mesmo motivo: a fila de um animal precisa valer entre requisicoes.
        services.AddSingleton<TravasPorAnimal>();

        return services;
    }
}
