using System.Collections.Concurrent;

namespace ClyvoVet.Application.Services.SaudePreditiva;

/// <summary>
/// Uma fila por animal para gerar o parecer: o segundo pedido espera o primeiro e lê o cache pronto.
///
/// <para>
/// <b>O que isto corrige.</b> A home e um pull-to-refresh chegando juntos, com o cache vencido,
/// faziam duas chamadas à OCI, duas gravações e <b>duas mensagens iguais</b> no Telegram do tutor.
/// </para>
///
/// <para>
/// <b>Por que singleton, e na memória.</b> O serviço é Scoped, então a trava precisa viver fora dele,
/// como o <see cref="Security.VinculosPendentesDeTelegram"/>. O limite é o mesmo: vale para UMA
/// instância. Com várias, cada uma tem as suas travas e a duplicata rara volta a ser possível; o
/// Render roda uma só. O dicionário não encolhe: é um semáforo por animal que já pediu parecer, o que
/// é pouco perto do resto que a API guarda.
/// </para>
/// </summary>
public class TravasPorAnimal
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _travas = new(StringComparer.Ordinal);

    /// <summary>Espera a vez deste animal. A trava é devolvida no <c>Dispose</c> do resultado.</summary>
    public async Task<IDisposable> EntrarAsync(string animalId, CancellationToken cancellationToken = default)
    {
        var trava = _travas.GetOrAdd(animalId, _ => new SemaphoreSlim(1, 1));
        await trava.WaitAsync(cancellationToken);
        return new Saida(trava);
    }

    /// <summary>Solta uma vez só: um segundo <c>Dispose</c> liberaria a vez de outro pedido.</summary>
    private sealed class Saida(SemaphoreSlim trava) : IDisposable
    {
        private int _liberada;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _liberada, 1) == 0)
                trava.Release();
        }
    }
}
