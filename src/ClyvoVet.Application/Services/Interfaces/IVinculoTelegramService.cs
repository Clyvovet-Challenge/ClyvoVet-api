using ClyvoVet.Application.DTOs.Response;

namespace ClyvoVet.Application.Services.Interfaces;

/// <summary>O vínculo de um tutor com o bot do Telegram: convidar, consultar e desligar.</summary>
public interface IVinculoTelegramService
{
    TelegramLinkResponse GerarLink(string tutorId);
    Task<TelegramVinculoResponse> ObterVinculoAsync(string tutorId);
    Task DesvincularAsync(string tutorId);
}
