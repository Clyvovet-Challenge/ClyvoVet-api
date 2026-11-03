namespace ClyvoVet.Application.Services.Interfaces;

/// <summary>
/// O que o bot do Telegram responde a cada comando. Devolve o texto; quem envia é o
/// ouvinte, na Infrastructure, que só conhece o Telegram.
/// </summary>
public interface IComandosDoBotService
{
    /// <summary>Resposta ao <c>/meusanimais</c>.</summary>
    Task<string> MeusAnimaisAsync(long chatId);

    /// <summary>Resposta ao <c>/meuslembretes</c>.</summary>
    Task<string> MeusLembretesAsync(long chatId);
}
