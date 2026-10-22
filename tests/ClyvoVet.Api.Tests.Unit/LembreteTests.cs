using ClyvoVet.Domain.Entities;
using ClyvoVet.Domain.Enums;

namespace ClyvoVet.Api.Tests.Unit;

public class LembreteTests
{
    private static readonly DateTime Agora = new(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);

    private static Lembrete Criar(int? intervaloDias, DateTime? repetirAte = null) => new()
    {
        AgendadoEm = Agora.AddMinutes(30),
        IntervaloDias = intervaloDias,
        RepetirAte = repetirAte,
        Status = StatusLembreteEnum.Pendente,
    };

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void RegistrarNotificacao_SemIntervalo_ViraEnviado(int? intervaloDias)
    {
        // Arrange
        var lembrete = Criar(intervaloDias);

        // Act
        lembrete.RegistrarNotificacao(Agora);

        // Assert
        Assert.Equal(StatusLembreteEnum.Enviado, lembrete.Status);
    }

    [Fact]
    public void RegistrarNotificacao_SerieSemFim_ContinuaPendenteNaProximaData()
    {
        // Arrange
        var lembrete = Criar(intervaloDias: 30);
        var agendadoAntes = lembrete.AgendadoEm;

        // Act
        lembrete.RegistrarNotificacao(Agora);

        // Assert
        Assert.Equal(StatusLembreteEnum.Pendente, lembrete.Status);
        Assert.Equal(agendadoAntes.AddDays(30), lembrete.AgendadoEm);
    }

    [Fact]
    public void RegistrarNotificacao_SerieNoFim_ViraEnviadoSemMexerNaData()
    {
        // Arrange
        var lembrete = Criar(intervaloDias: 30, repetirAte: Agora.AddDays(10));
        var agendadoAntes = lembrete.AgendadoEm;

        // Act
        lembrete.RegistrarNotificacao(Agora);

        // Assert
        Assert.Equal(StatusLembreteEnum.Enviado, lembrete.Status);
        Assert.Equal(agendadoAntes, lembrete.AgendadoEm);
    }
}
