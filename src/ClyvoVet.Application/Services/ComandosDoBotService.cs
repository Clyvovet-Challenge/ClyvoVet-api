using ClyvoVet.Application.Abstractions.Repositories;
using ClyvoVet.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ClyvoVet.Application.Services;

/// <summary>
/// As regras dos comandos do bot: quem está vinculado, o que listar e como dizer.
///
/// <para>
/// Moravam no <c>TelegramLinkListenerService</c>, na Infrastructure, que lia os
/// repositórios direto. Aqui ficam na camada de aplicação, como as demais regras, e o
/// ouvinte fica só com o polling e o envio.
/// </para>
/// </summary>
public class ComandosDoBotService(
    ITutorTelegramRepository vinculos,
    IAnimalRepository animais,
    ILembreteRepository lembretes,
    ILogger<ComandosDoBotService> logger) : IComandosDoBotService
{
    private const string SemVinculo =
        "Você ainda não está vinculado a nenhum tutor. Peça o link de vínculo no app da ClyvoVet.";

    public async Task<string> MeusAnimaisAsync(long chatId)
    {
        var tutorId = await vinculos.GetTutorIdByChatIdAsync(chatId);
        if (tutorId is null)
            return SemVinculo;

        var lista = (await animais.GetByTutorIdAsync(tutorId)).ToList();
        if (lista.Count == 0)
            return "Você ainda não tem nenhum pet cadastrado.";

        var linhas = lista.Select(a => $"• {a.Nome} — {a.Especie}{(string.IsNullOrWhiteSpace(a.Raca) ? "" : $" ({a.Raca})")}");
        return "🐾 Seus pets:\n\n" + string.Join("\n", linhas);
    }

    public async Task<string> MeusLembretesAsync(long chatId)
    {
        var tutorId = await vinculos.GetTutorIdByChatIdAsync(chatId);
        if (tutorId is null)
            return SemVinculo;

        var pendentes = (await lembretes.GetPendentesByTutorIdAsync(tutorId)).ToList();
        if (pendentes.Count == 0)
            return "Você não tem lembretes pendentes no momento. 🎉";

        var linhas = pendentes.Select(l => $"• {l.Titulo} ({l.Animal.Nome}) — {l.AgendadoEm:dd/MM/yyyy HH:mm}");
        return "📋 Seus lembretes pendentes:\n\n" + string.Join("\n", linhas);
    }

    public async Task VincularAsync(string tutorId, long chatId)
    {
        await vinculos.VincularAsync(tutorId, chatId);
        logger.LogInformation("Tutor {TutorId} vinculado ao chatId {ChatId} no Telegram.", tutorId, chatId);
    }

    public async Task<string> DesvincularAsync(long chatId)
    {
        var tutorId = await vinculos.GetTutorIdByChatIdAsync(chatId);
        if (tutorId is null)
            return "Você não está vinculado a nenhum tutor no momento.";

        await vinculos.DesvincularAsync(tutorId);
        logger.LogInformation("Tutor {TutorId} desvinculado do chatId {ChatId} no Telegram.", tutorId, chatId);
        return "✅ Vínculo removido. Você não vai mais receber notificações da ClyvoVet por aqui.";
    }
}
