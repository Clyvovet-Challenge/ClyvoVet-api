using ClyvoVet.Application.Abstractions.Repositories;
using ClyvoVet.Application.Services.Interfaces;

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
    IAnimalRepository animais) : IComandosDoBotService
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
}
