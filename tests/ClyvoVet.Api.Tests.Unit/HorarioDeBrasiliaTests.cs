using ClyvoVet.Application.Common;

namespace ClyvoVet.Api.Tests.Unit;

public class HorarioDeBrasiliaTests
{
    [Fact]
    public void Agora_RelogioEmUtc_DevolveTresHorasAMenos()
    {
        // Arrange
        var relogio = new RelogioDeTeste(new DateTimeOffset(2026, 10, 1, 11, 30, 0, TimeSpan.Zero));

        // Act
        var agora = HorarioDeBrasilia.Agora(relogio);

        // Assert
        Assert.Equal(new DateTime(2026, 10, 1, 8, 30, 0), agora);
    }

    [Fact]
    public void Agora_SemFusoNoValor_ComoOAgendadoEmQueOAppManda()
    {
        var agora = HorarioDeBrasilia.Agora(new RelogioDeTeste(DateTimeOffset.UtcNow));

        Assert.Equal(DateTimeKind.Unspecified, agora.Kind);
    }

    [Fact]
    public void Hoje_DepoisDasVinteUmEmBrasilia_AindaEODiaDeBrasilia()
    {
        // Arrange: 00:30 UTC do dia 1º = 21:30 do dia 30 em Brasília.
        var relogio = new RelogioDeTeste(new DateTimeOffset(2026, 10, 1, 0, 30, 0, TimeSpan.Zero));

        // Act
        var hoje = HorarioDeBrasilia.Hoje(relogio);

        // Assert
        Assert.Equal(new DateOnly(2026, 9, 30), hoje);
    }
}
