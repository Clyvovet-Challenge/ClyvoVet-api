using ClyvoVet.Api.Services;

namespace ClyvoVet.Api.Tests.Unit;

/// <summary>
/// As duas sobrecargas de <see cref="DataValidationHelper.EhDataNoPassado(DateOnly)"/>
/// e <see cref="DataValidationHelper.EhDataNoPassado(DateTime)"/> precisam concordar
/// sobre que dia é hoje.
///
/// <para>
/// Uma lia o fuso local do servidor e a outra lia UTC. Em contêiner UTC as duas
/// concordam e nada aparece; com <c>WEBSITE_TIME_ZONE</c> definido — comum em
/// aplicação brasileira — elas divergem por até um dia inteiro, e um evento marcado
/// para hoje passa a ser recusado como passado.
/// </para>
/// </summary>
public class DataValidationHelperTests
{
    [Fact]
    public void AsDuasSobrecargas_ConcordamSobreQueDiaEHoje()
    {
        var hojeEmUtc = DateOnly.FromDateTime(DateTime.UtcNow);

        Assert.False(DataValidationHelper.EhDataNoPassado(hojeEmUtc));
        Assert.False(DataValidationHelper.EhDataNoPassado(DateTime.UtcNow.AddMinutes(1)));
    }

    [Fact]
    public void Ontem_EmUtc_ContaComoPassado()
    {
        Assert.True(DataValidationHelper.EhDataNoPassado(
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))));
    }

    [Fact]
    public void Amanha_EmUtc_NaoContaComoPassado()
    {
        Assert.False(DataValidationHelper.EhDataNoPassado(
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))));
    }

    [Fact]
    public void Instante_NoPassado_ContaComoPassado()
    {
        Assert.True(DataValidationHelper.EhDataNoPassado(DateTime.UtcNow.AddSeconds(-5)));
    }
}
