using ClyvoVet.Api.Repositories.Interfaces;
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
/// <remarks>
/// <para><b>Por que duas chaves diferentes neste mesmo controller</b></para>
///
/// <para>
/// O filtro saiu da classe e passou a ser por acao. Nao e arrumacao: as acoes
/// aqui respondem a perguntas de risco muito diferente.
/// </para>
///
/// <para>
/// <c>enviar</c> manda QUALQUER mensagem para QUALQUER chatId. Uma chave que
/// abre isso nao pode viajar dentro de um aplicativo distribuido -- quem
/// extrair o bundle passa a poder escrever, como se fosse a clinica, para todo
/// tutor cujo chatId descobrir. Ela continua sendo a <c>Telegram:ApiKey</c>, de
/// servico para servico.
/// </para>
///
/// <para>
/// As outras tres sao do proprio tutor sobre o proprio vinculo, e o app precisa
/// delas para existir enquanto tela. Elas usam a <c>Api:ApiKey</c>, a mesma que
/// o app ja carrega para os lembretes, com o <see cref="EscopoDoTutor"/>
/// impedindo que um tutor mexa no vinculo de outro. Exigir a chave de envio
/// aqui obrigaria a embarcar a chave de envio -- que e exatamente o que o
/// paragrafo acima proibe.
/// </para>
/// </remarks>
[ApiController]
[Route("api/v1/telegram")]
[Produces("application/json")]
public class TelegramController : ControllerBase
{
    private readonly ITelegramService _service;
    private readonly IConfiguration _configuration;
    private readonly VinculosPendentesDeTelegram _convites;
    private readonly EscopoDoTutor _escopo;
    private readonly ITutorTelegramRepository _vinculos;

    public TelegramController(
        ITelegramService service,
        IConfiguration configuration,
        VinculosPendentesDeTelegram convites,
        EscopoDoTutor escopo,
        ITutorTelegramRepository vinculos)
    {
        _service = service;
        _configuration = configuration;
        _convites = convites;
        _escopo = escopo;
        _vinculos = vinculos;
    }

    /// <summary>Envia uma mensagem de Telegram para o chatId informado.</summary>
    [HttpPost("enviar")]
    [TypeFilter(typeof(ApiKeyFilterAttribute), Arguments = new object[] { "Telegram:ApiKey" })]
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
    [TypeFilter(typeof(ApiKeyFilterAttribute), Arguments = new object[] { "Api:ApiKey" })]
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
    /// <summary>Diz se este tutor ja tem uma conversa do Telegram ligada.</summary>
    /// <remarks>
    /// A tela de vinculo precisa disso antes de desenhar: oferecer "conectar" a
    /// quem ja conectou manda a pessoa refazer o que esta feito, e mostrar
    /// "conectado" a quem nao esta promete um lembrete que nunca chega.
    ///
    /// <para>
    /// Responde 200 nos dois casos, com <c>vinculado</c> dizendo qual deles. Um
    /// 404 para "nao vinculado" faria a ausencia de vinculo parecer erro, e o app
    /// teria de tratar como excecao um estado que e normal -- todo tutor comeca
    /// nele.
    /// </para>
    /// </remarks>
    [HttpGet("vinculo/{tutorId}")]
    [TypeFilter(typeof(ApiKeyFilterAttribute), Arguments = new object[] { "Api:ApiKey" })]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Vinculo(string tutorId)
    {
        if (_escopo.Ativo && !string.Equals(_escopo.TutorId, tutorId, StringComparison.Ordinal))
            return Forbid();

        var vinculo = await _vinculos.ObterVinculoAsync(tutorId);

        return Ok(new TelegramVinculoResponse
        {
            Vinculado = vinculo is not null,
            Desde = vinculo?.CriadoEm,
        });
    }

    /// <summary>Desliga o Telegram deste tutor.</summary>
    /// <remarks>
    /// Quem liga precisa poder desligar, e pelo mesmo lugar. Sem isto, sair das
    /// notificacoes exigiria bloquear o bot no proprio Telegram -- o vinculo
    /// continuaria no banco, e esta API continuaria tentando escrever para um
    /// chat que recusa.
    ///
    /// <para>
    /// Devolve 204 mesmo quando nao havia vinculo. O pedido e "que este tutor
    /// nao receba mais", e esse estado passa a valer nos dois casos; distinguir
    /// com 404 so daria ao app um erro para tratar sem nada a fazer com ele.
    /// </para>
    /// </remarks>
    [HttpDelete("vinculo/{tutorId}")]
    [TypeFilter(typeof(ApiKeyFilterAttribute), Arguments = new object[] { "Api:ApiKey" })]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Desvincular(string tutorId)
    {
        if (_escopo.Ativo && !string.Equals(_escopo.TutorId, tutorId, StringComparison.Ordinal))
            return Forbid();

        await _vinculos.DesvincularAsync(tutorId);
        return NoContent();
    }
}
