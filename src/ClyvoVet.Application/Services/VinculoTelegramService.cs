using ClyvoVet.Application.Abstractions.Repositories;
using ClyvoVet.Application.DTOs.Response;
using ClyvoVet.Application.Security;
using ClyvoVet.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClyvoVet.Application.Services;

/// <summary>
/// Os casos de uso do vínculo com o Telegram, que antes moravam no controller.
///
/// <para>
/// O controller falava direto com o repositório e montava o deep link; era o único
/// da API a pular a camada de serviço. A checagem de escopo e os <c>_links</c>
/// continuam lá, porque decidir entre 403 e 200 e gerar URL de rota são assuntos de
/// HTTP.
/// </para>
/// </summary>
public class VinculoTelegramService(
    IConfiguration configuracao,
    VinculosPendentesDeTelegram convites,
    ITutorTelegramRepository vinculos,
    ILogger<VinculoTelegramService> logger) : IVinculoTelegramService
{
    /// <summary>O valor de exemplo do appsettings, que ninguém trocou.</summary>
    private const string PlaceholderDoBot = "SEU_BOT_USERNAME";

    public TelegramLinkResponse GerarLink(string tutorId)
    {
        var botUsername = configuracao["Telegram:BotUsername"];

        // Sem o nome do bot, o link não abre conversa nenhuma, e o erro aparece no
        // celular do tutor, longe dos logs. O link sai mesmo assim: derrubar o pedido
        // com 500 não conserta a configuração, e o aviso aponta o que falta.
        if (string.IsNullOrWhiteSpace(botUsername) || botUsername == PlaceholderDoBot)
        {
            logger.LogWarning(
                "Telegram:BotUsername não configurado; o link do Telegram não vai abrir o bot.");
        }

        var token = convites.Gerar(tutorId);
        return new TelegramLinkResponse { Link = $"https://t.me/{botUsername}?start={token}" };
    }

    public async Task<TelegramVinculoResponse> ObterVinculoAsync(string tutorId)
    {
        var vinculo = await vinculos.ObterVinculoAsync(tutorId);
        return new TelegramVinculoResponse
        {
            Vinculado = vinculo is not null,
            Desde = vinculo?.CriadoEm,
        };
    }

    public Task DesvincularAsync(string tutorId) => vinculos.DesvincularAsync(tutorId);
}
