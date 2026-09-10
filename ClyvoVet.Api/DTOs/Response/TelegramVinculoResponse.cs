namespace ClyvoVet.Api.DTOs.Response;

/// <summary>
/// O estado do vínculo de um tutor com o bot do Telegram.
///
/// <para>
/// Existe porque a tela de "conectar o Telegram" precisa saber em qual dos dois
/// estados ela está antes de desenhar qualquer coisa: oferecer "conectar" a
/// quem já conectou é um convite para refazer o que já está feito, e mostrar
/// "conectado" a quem não está é prometer lembrete que nunca vai chegar.
/// </para>
///
/// <para>
/// O <c>chatId</c> NÃO entra aqui de propósito. Ele identifica a conversa
/// no Telegram, e o app não faz nada com ele — quem envia é esta API. Devolvê-lo
/// só ampliaria o estrago de um token vazado.
/// </para>
/// </summary>
public class TelegramVinculoResponse
{
    /// <summary>Se há uma conversa do Telegram ligada a este tutor.</summary>
    public bool Vinculado { get; set; }

    /// <summary>Quando o vínculo foi feito. Nulo quando não há vínculo.</summary>
    public DateTime? Desde { get; set; }
}
