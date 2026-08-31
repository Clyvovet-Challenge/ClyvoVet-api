using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.Filters;
using ClyvoVet.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVet.Api.Controllers;

/// <summary>
/// Disparo de mensagens no Telegram via bot próprio.
/// </summary>
[ApiController]
[Route("api/v1/telegram")]
[Produces("application/json")]
[TypeFilter(typeof(ApiKeyFilterAttribute), Arguments = new object[] { "Telegram:ApiKey" })]
public class TelegramController : ControllerBase
{
    private readonly ITelegramService _service;

    public TelegramController(ITelegramService service) => _service = service;

    /// <summary>Envia uma mensagem de Telegram para o chatId informado.</summary>
    [HttpPost("enviar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Enviar([FromBody] TelegramRequest request)
    {
        await _service.EnviarMensagemAsync(request.ChatId, request.Mensagem);
        return NoContent();
    }
}
