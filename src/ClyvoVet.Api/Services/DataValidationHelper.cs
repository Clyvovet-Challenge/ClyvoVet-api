namespace ClyvoVet.Api.Services;

/// <summary>
/// "Essa data já passou?" — a pergunta que os serviços fazem antes de aceitar um
/// agendamento.
/// </summary>
public static class DataValidationHelper
{
    /// <summary>
    /// Data de calendário (evento do pet).
    /// </summary>
    /// <remarks>
    /// <b>UTC, e não <c>DateTime.Today</c>.</b> As duas sobrecargas deste mesmo
    /// método usavam relógios diferentes: esta lia o fuso <b>local do servidor</b> e
    /// a de baixo lia UTC. Enquanto o processo roda em UTC — que é o padrão do
    /// contêiner e do App Service — as duas concordam e ninguém percebe. Basta
    /// alguém definir <c>WEBSITE_TIME_ZONE</c>, que é comum em aplicação brasileira,
    /// para elas passarem a discordar sobre que dia é hoje, e um evento marcado para
    /// hoje ser recusado como passado num fuso adiantado. Todo o resto da API grava
    /// e compara em UTC (<c>CriadoEm</c>, <c>AgendadoEm</c>, o serviço de
    /// notificação); esta linha era a única fora do acordo.
    /// </remarks>
    public static bool EhDataNoPassado(DateOnly data) =>
        data < DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Instante (lembrete), sempre em UTC.</summary>
    public static bool EhDataNoPassado(DateTime data) => data < DateTime.UtcNow;
}
