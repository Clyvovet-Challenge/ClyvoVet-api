using ClyvoVet.Api.Security;

namespace ClyvoVet.Api.Tests.Unit;

/// <summary>
/// Relógio de mentira, escrito à mão.
///
/// <para>
/// <c>FakeTimeProvider</c> viria do pacote
/// <c>Microsoft.Extensions.TimeProvider.Testing</c>, e trazer um pacote inteiro para
/// dez linhas custaria mais do que vale: cada dependência nova entra na varredura de
/// vulnerabilidades deste projeto.
/// </para>
/// </summary>
internal sealed class RelogioDeTeste(DateTimeOffset inicio) : TimeProvider
{
    private DateTimeOffset _agora = inicio;

    public override DateTimeOffset GetUtcNow() => _agora;

    public void Avancar(TimeSpan quanto) => _agora = _agora.Add(quanto);
}

/// <summary>
/// O convite de vínculo com o bot do Telegram.
///
/// <para>
/// Estes testes existem por um defeito real: o deep link levava o próprio
/// <c>tutorId</c>, e o ouvinte gravava o vínculo para qualquer id que chegasse. Como
/// o bot é público e o <c>tutorId</c> viaja em <c>/auth/me</c> e no corpo de cada
/// animal, qualquer pessoa digitava <c>/start &lt;uuid alheio&gt;</c> e passava a
/// receber as notificações daquele tutor — deixando o dono de verdade sem nenhuma,
/// nem por WhatsApp, porque o envio ao Telegram "dava certo".
/// </para>
/// </summary>
public class VinculosPendentesDeTelegramTests
{
    private const string Tutor = "tutor-abc-123";

    private static RelogioDeTeste NovoRelogio() =>
        new(new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Consumir_ComTokenRecemGerado_DevolveOTutor()
    {
        var cofre = new VinculosPendentesDeTelegram();

        var token = cofre.Gerar(Tutor);

        Assert.Equal(Tutor, cofre.Consumir(token));
    }

    /// <summary>
    /// O ponto inteiro da classe: saber o tutorId deixa de servir para alguma coisa.
    /// </summary>
    [Fact]
    public void Consumir_ComOProprioTutorId_NaoVincula()
    {
        var cofre = new VinculosPendentesDeTelegram();
        cofre.Gerar(Tutor);

        Assert.Null(cofre.Consumir(Tutor));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("token-que-nunca-existiu")]
    public void Consumir_ComEntradaQueNaoEConvite_DevolveNulo(string entrada)
    {
        var cofre = new VinculosPendentesDeTelegram();
        cofre.Gerar(Tutor);

        Assert.Null(cofre.Consumir(entrada));
    }

    [Fact]
    public void Consumir_ComNulo_DevolveNulo()
    {
        Assert.Null(new VinculosPendentesDeTelegram().Consumir(null));
    }

    /// <summary>
    /// Uso único. Sem isto, um token que aparecesse num print de tela ou num log
    /// continuaria valendo para quem o lesse depois.
    /// </summary>
    [Fact]
    public void Consumir_DuasVezes_SoFuncionaNaPrimeira()
    {
        var cofre = new VinculosPendentesDeTelegram();
        var token = cofre.Gerar(Tutor);

        Assert.Equal(Tutor, cofre.Consumir(token));
        Assert.Null(cofre.Consumir(token));
    }

    [Fact]
    public void Consumir_DepoisDeVencer_DevolveNulo()
    {
        var relogio = NovoRelogio();
        var cofre = new VinculosPendentesDeTelegram(relogio);
        var token = cofre.Gerar(Tutor);

        relogio.Avancar(VinculosPendentesDeTelegram.Validade + TimeSpan.FromSeconds(1));

        Assert.Null(cofre.Consumir(token));
    }

    [Fact]
    public void Consumir_UmSegundoAntesDeVencer_AindaFunciona()
    {
        var relogio = NovoRelogio();
        var cofre = new VinculosPendentesDeTelegram(relogio);
        var token = cofre.Gerar(Tutor);

        relogio.Avancar(VinculosPendentesDeTelegram.Validade - TimeSpan.FromSeconds(1));

        Assert.Equal(Tutor, cofre.Consumir(token));
    }

    [Fact]
    public void Gerar_DuasVezes_ProduzTokensDiferentes()
    {
        var cofre = new VinculosPendentesDeTelegram();

        Assert.NotEqual(cofre.Gerar(Tutor), cofre.Gerar(Tutor));
    }

    /// <summary>
    /// O parâmetro <c>start</c> do Telegram aceita <b>somente</b> este alfabeto e no
    /// máximo 64 caracteres. Um Base64 comum traria <c>+</c>, <c>/</c> e <c>=</c>, e
    /// o Telegram recusaria o link inteiro do lado do cliente — em silêncio, onde
    /// nenhum log desta API enxergaria.
    /// </summary>
    [Fact]
    public void Gerar_ProduzTokenQueOTelegramAceitaNaUrl()
    {
        var cofre = new VinculosPendentesDeTelegram();

        for (var i = 0; i < 200; i++)
        {
            var token = cofre.Gerar(Tutor);
            Assert.InRange(token.Length, 1, 64);
            Assert.Matches("^[A-Za-z0-9_-]+$", token);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Gerar_SemTutor_Recusa(string? tutorId)
    {
        var cofre = new VinculosPendentesDeTelegram();

        Assert.ThrowsAny<ArgumentException>(() => cofre.Gerar(tutorId!));
    }

    /// <summary>
    /// Sem a faxina, um convite nunca resgatado ficaria na memória para sempre — e o
    /// processo desta API roda por semanas sem reiniciar.
    /// </summary>
    [Fact]
    public void Gerar_VarreOsConvitesJaVencidos()
    {
        var relogio = NovoRelogio();
        var cofre = new VinculosPendentesDeTelegram(relogio);
        cofre.Gerar("tutor-1");
        cofre.Gerar("tutor-2");
        Assert.Equal(2, cofre.Pendentes);

        relogio.Avancar(VinculosPendentesDeTelegram.Validade + TimeSpan.FromMinutes(1));
        cofre.Gerar("tutor-3");

        Assert.Equal(1, cofre.Pendentes);
    }

    /// <summary>
    /// Dois <c>/start</c> com o mesmo token ao mesmo tempo só podem ter um vencedor:
    /// é o <c>TryRemove</c> do dicionário que garante isso, e não um lock nosso.
    /// </summary>
    [Fact]
    public void Consumir_EmParalelo_SoUmVence()
    {
        var cofre = new VinculosPendentesDeTelegram();
        var token = cofre.Gerar(Tutor);

        var resultados = new string?[64];
        Parallel.For(0, resultados.Length, i => resultados[i] = cofre.Consumir(token));

        Assert.Single(resultados, r => r is not null);
    }
}
