using ClyvoVet.Application.Common;

namespace ClyvoVet.Api.Tests.Unit;

/// <summary>
/// "Essa data já passou?", sempre no horário de Brasília.
///
/// <para>
/// O app manda <c>agendadoEm</c> como hora de Brasília, sem fuso, e a API comparava com
/// <c>DateTime.UtcNow</c>, três horas à frente. Um lembrete para daqui a uma hora era
/// recusado como passado, e depois das 21h o "hoje" do servidor já era amanhã.
/// </para>
/// </summary>
public class DataValidationHelperTests
{
    // 11:00 UTC = 08:00 em Brasília.
    private static readonly RelogioDeTeste OitoDaManha = new(new DateTimeOffset(2026, 10, 1, 11, 0, 0, TimeSpan.Zero));

    // 01:00 UTC do dia 1º = 22:00 em Brasília do dia 30.
    private static readonly RelogioDeTeste DezDaNoite = new(new DateTimeOffset(2026, 10, 1, 1, 0, 0, TimeSpan.Zero));

    [Fact]
    public void EhDataNoPassado_InstanteUmaHoraAFrenteEmBrasilia_NaoContaComoPassado()
    {
        Assert.False(DataValidationHelper.EhDataNoPassado(new DateTime(2026, 10, 1, 9, 0, 0), OitoDaManha));
    }

    [Fact]
    public void EhDataNoPassado_InstanteUmMinutoAtrasEmBrasilia_ContaComoPassado()
    {
        Assert.True(DataValidationHelper.EhDataNoPassado(new DateTime(2026, 10, 1, 7, 59, 0), OitoDaManha));
    }

    [Fact]
    public void EhDataNoPassado_HojeAsDezDaNoiteEmBrasilia_AindaEHoje()
    {
        Assert.False(DataValidationHelper.EhDataNoPassado(new DateOnly(2026, 9, 30), DezDaNoite));
    }

    [Fact]
    public void EhDataNoPassado_OntemEmBrasilia_ContaComoPassado()
    {
        Assert.True(DataValidationHelper.EhDataNoPassado(new DateOnly(2026, 9, 29), DezDaNoite));
    }

    [Fact]
    public void EhDataNoPassado_SemRelogio_UsaOHorarioDeBrasiliaAtual()
    {
        Assert.False(DataValidationHelper.EhDataNoPassado(HorarioDeBrasilia.Agora().AddMinutes(1)));
        Assert.False(DataValidationHelper.EhDataNoPassado(HorarioDeBrasilia.Hoje()));
    }
}
