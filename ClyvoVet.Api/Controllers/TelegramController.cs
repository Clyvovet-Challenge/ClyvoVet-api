using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.DTOs.Response;
using ClyvoVet.Api.Filters;
using ClyvoVet.Api.Security;
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
    private readonly VinculosPendentesDeTelegram _convites;
    private readonly EscopoDoTutor _escopo;

    public TelegramController(
        ITelegramService service,
        IConfiguration configuration,
        VinculosPendentesDeTelegram convites,
        EscopoDoTutor escopo)
    {
        _service = service;
        _configuration = configuration;
        _convites = convites;
        _escopo = escopo;
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

    /// <summary>
    /// Gera o link de vínculo do tutor com o bot do Telegram.
    /// </summary>
    /// <remarks>
    /// O que vai no <c>start=</c> é um convite de uso único com quinze minutos de
    /// prazo, e não mais o <c>tutorId</c>. O motivo está em
    /// <see cref="VinculosPendentesDeTelegram"/>: o bot é público, e enquanto o
    /// parâmetro era o id do tutor, qualquer pessoa digitava <c>/start</c> com o id
    /// alheio e assumia as notificações daquele tutor.
    ///
    /// <para>
    /// O link muda a cada chamada, e isso é o desenho, não um efeito colateral:
    /// pedir de novo invalida nada, mas cada link só serve uma vez.
    /// </para>
    /// </remarks>
    [HttpGet("link/{tutorId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GerarLink(string tutorId)
    {
        // Defesa em profundidade, e só vale com o recorte ligado: com ele, quem
        // traz um token de tutor só pode gerar convite para si mesmo. Com o recorte
        // desligado (o padrao), a X-Api-Key continua sendo a unica barreira -- que e
        // exatamente o contrato ja documentado em EscopoDoTutor, e nao se muda aqui.
        if (_escopo.Ativo && !string.Equals(_escopo.TutorId, tutorId, StringComparison.Ordinal))
            return Forbid();

        var botUsername = _configuration["Telegram:BotUsername"];
        var token = _convites.Gerar(tutorId);
        var link = $"https://t.me/{botUsername}?start={token}";
        return Ok(new TelegramLinkResponse { Link = link });
    }
}
