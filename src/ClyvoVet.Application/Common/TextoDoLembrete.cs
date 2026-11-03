using ClyvoVet.Domain.Entities;

namespace ClyvoVet.Application.Common;

/// <summary>
/// O que o tutor lê quando um lembrete vence. Morava dentro do
/// <c>LembreteNotificationService</c>, na Infrastructure; o worker agora só decide
/// quando enviar e por onde.
/// </summary>
public static class TextoDoLembrete
{
    public static string Notificacao(Lembrete lembrete) =>
        $"Lembrete: {lembrete.Titulo} agendado para {lembrete.AgendadoEm:dd/MM/yyyy HH:mm} ({lembrete.Animal.Nome}).";
}
