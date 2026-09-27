namespace ClyvoVet.Application.Common;

/// <summary>
/// "Agora" e "hoje" no horário de Brasília, o relógio das datas que o usuário informa.
///
/// <para>
/// <b>Por que não UTC.</b> O app manda <c>agendadoEm</c> como hora de Brasília, sem fuso
/// ("2026-10-01T10:00:00"), e mostra de volta exatamente o que recebeu. Comparar esse valor com
/// <c>DateTime.UtcNow</c>, três horas à frente, recusava como "passado" um lembrete para daqui a
/// uma hora e disparava a notificação quatro horas antes em vez de uma. Carimbos internos
/// (<c>CriadoEm</c>, validade do cache) continuam em UTC: ninguém os digita.
/// </para>
///
/// <para>
/// O valor devolvido não carrega fuso (<see cref="DateTimeKind.Unspecified"/>), igual ao que o
/// app manda e ao que o MySQL devolve, para as comparações serem entre relógios iguais.
/// </para>
/// </summary>
public static class HorarioDeBrasilia
{
    /// <summary>
    /// <c>America/Sao_Paulo</c>, com reserva fixa em −03:00. A reserva não é enfeite: imagem de
    /// contêiner sem tzdata não acha o fuso, e sem ela a API cairia no boot. Desde 2019 o Brasil
    /// não tem horário de verão, então as duas dão o mesmo resultado.
    /// </summary>
    private static readonly TimeZoneInfo Fuso = CarregarFuso();

    public static DateTime Agora(TimeProvider? relogio = null) =>
        TimeZoneInfo.ConvertTime((relogio ?? TimeProvider.System).GetUtcNow(), Fuso).DateTime;

    public static DateOnly Hoje(TimeProvider? relogio = null) =>
        DateOnly.FromDateTime(Agora(relogio));

    private static TimeZoneInfo CarregarFuso()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("Brasilia", TimeSpan.FromHours(-3), "Brasília", "Brasília");
        }
    }
}
