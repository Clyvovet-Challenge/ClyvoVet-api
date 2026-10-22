namespace ClyvoVet.Application.Common;

/// <summary>
/// "Essa data já passou?" — a pergunta que os serviços fazem antes de aceitar um
/// agendamento.
/// </summary>
/// <remarks>
/// As duas sobrecargas perguntam ao <b>mesmo</b> relógio, o <see cref="HorarioDeBrasilia"/>.
/// Já houve uma lendo o fuso do servidor e a outra UTC, discordando sobre que dia é hoje; e
/// depois as duas em UTC, três horas à frente do que o usuário digita.
/// </remarks>
public static class DataValidationHelper
{
    /// <summary>Data de calendário (evento do pet).</summary>
    public static bool EhDataNoPassado(DateOnly data, TimeProvider? relogio = null) =>
        data < HorarioDeBrasilia.Hoje(relogio);

    /// <summary>Instante (lembrete), em hora de Brasília.</summary>
    public static bool EhDataNoPassado(DateTime data, TimeProvider? relogio = null) =>
        data < HorarioDeBrasilia.Agora(relogio);
}
