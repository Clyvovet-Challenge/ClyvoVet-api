using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.Filters;
using ClyvoVet.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVet.Api.Controllers;

/// <summary>
/// Disparo de mensagens no WhatsApp via Twilio Sandbox.
/// </summary>
[ApiController]
[Route("api/v1/whatsapp")]
[Produces("application/json")]
[TypeFilter(typeof(ApiKeyFilterAttribute), Arguments = new object[] { "WhatsApp:ApiKey" })]
public class WhatsAppController : ControllerBase
{
    private readonly IWhatsAppService _service;

    public WhatsAppController(IWhatsAppService service) => _service = service;

    /// <summary>Envia uma mensagem de WhatsApp para o número informado.</summary>
    [HttpPost("enviar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Enviar([FromBody] WhatsAppRequest request)
    {
        await _service.EnviarMensagemAsync(request.Telefone, request.Mensagem);
        return NoContent();
    }
}
