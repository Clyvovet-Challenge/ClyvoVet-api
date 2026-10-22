using ClyvoVet.Domain.Enums;

namespace ClyvoVet.Domain.Entities;

public class Lembrete
{
    public string Id { get; set; } = null!;
    public string AnimalId { get; set; } = null!;
    public Animal Animal { get; set; } = null!;
    public string Titulo { get; set; } = null!;
    public string? Descricao { get; set; }
    public TipoLembreteEnum Tipo { get; set; }
    public DateTime AgendadoEm { get; set; }
    /// <summary>
    /// DERIVADO de <see cref="IntervaloDias"/>, e mantido por compatibilidade: o
    /// app le este campo hoje. Quem decide e o intervalo — ver LembreteService.
    /// </summary>
    public bool Recorrente { get; set; }

    /// <summary>
    /// De quantos em quantos dias o lembrete volta. NULO = nao repete.
    /// </summary>
    public int? IntervaloDias { get; set; }

    /// <summary>
    /// Fim da serie. NULO = sem fim previsto (antipulgas mensal). Preenchido, e
    /// o "de x dia ate y dia": <see cref="AgendadoEm"/> comeca, este termina.
    /// </summary>
    public DateTime? RepetirAte { get; set; }
    public StatusLembreteEnum Status { get; set; }
    public DateTime CriadoEm { get; set; }

    /// <summary>
    /// O tutor foi avisado: o lembrete avulso vira Enviado, e o de série anda para a
    /// próxima data — ou vira Enviado, se a série acabou.
    /// </summary>
    /// <remarks>
    /// Mora aqui, e não no BackgroundService que envia, porque é regra do lembrete:
    /// quem notifica só precisa saber que notificou.
    ///
    /// <para>O de série continua Pendente DE PROPÓSITO: é a mesma linha que volta,
    /// com a data empurrada para a frente.</para>
    /// </remarks>
    public void RegistrarNotificacao(DateTime agora)
    {
        if (IntervaloDias is not > 0)
        {
            Status = StatusLembreteEnum.Enviado;
            return;
        }

        var (proxima, terminou) = AvancarSerie(AgendadoEm, IntervaloDias.Value, RepetirAte, agora);
        if (terminou)
            Status = StatusLembreteEnum.Enviado;
        else
            AgendadoEm = proxima;
    }

    /// <summary>
    /// Onde a serie cai depois de um disparo, e se ela acabou.
    /// </summary>
    /// <remarks>
    /// <para>Estatico e puro de proposito: a aritmetica da serie se testa com
    /// datas na mao, sem banco nem BackgroundService.</para>
    ///
    /// <para><b>O laco existe por causa do tempo parado.</b> Se a API ficou fora do
    /// ar por um mes, um lembrete diario esta trinta dias atrasado. Avancar UM
    /// intervalo o deixaria ainda no passado, ele seria varrido no minuto
    /// seguinte, notificado outra vez, e o tutor receberia trinta mensagens
    /// iguais para se atualizar. O laco pula de uma vez para a proxima data
    /// futura: uma notificacao, e a serie volta ao ritmo.</para>
    ///
    /// <para>O teto de iteracoes e cinto de seguranca. O CHECK do banco e o
    /// <c>[Range(1,365)]</c> do request ja impedem intervalo zero ou negativo;
    /// se algum dia um deles falhar, o que acontece e um limite atingido e nao
    /// um BackgroundService girando para sempre.</para>
    /// </remarks>
    public static (DateTime proxima, bool serieTerminou) AvancarSerie(
        DateTime agendadoEm, int intervaloDias, DateTime? repetirAte, DateTime agora)
    {
        if (intervaloDias <= 0)
            return (agendadoEm, true);

        const int TetoDeIteracoes = 1000;
        var proxima = agendadoEm;

        for (var i = 0; i < TetoDeIteracoes; i++)
        {
            proxima = proxima.AddDays(intervaloDias);

            // O fim da serie manda, mesmo que a data ainda esteja no passado:
            // uma serie que acabou nao volta so porque houve atraso.
            if (repetirAte.HasValue && proxima > repetirAte.Value)
                return (proxima, true);

            if (proxima > agora)
                return (proxima, false);
        }

        return (proxima, false);
    }
}
