using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.DTOs.Response;
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
    private readonly IConfiguration _configuration;

    public TelegramController(ITelegramService service, IConfiguration configuration)
    {
        _service = service;
        _configuration = configuration;
    }

    /// <summary>Envia uma mensagem de Telegram para o chatId informado.</summary>
    [HttpPost("enviar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Enviar([FromBody] TelegramRequest request)
    {
        await _service.EnviarMensagemAsync(request.ChatId, request.Mensagem);
        return NoContent();
    }

    /// <summary>Gera o link de vínculo do tutor com o bot do Telegram (deep link com o tutorId).</summary>
    [HttpGet("link/{tutorId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GerarLink(string tutorId)
    {
        var botUsername = _configuration["Telegram:BotUsername"];
        var link = $"https://t.me/{botUsername}?start={tutorId}";
        return Ok(new TelegramLinkResponse { Link = link });
    }
}
