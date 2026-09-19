using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace ClyvoVet.Api.Security;

/// <summary>
/// Convites de vínculo com o bot do Telegram: um segredo de uso único, com prazo.
///
/// <para>
/// <b>O que isto corrige.</b> O deep link era
/// <c>t.me/&lt;bot&gt;?start=&lt;tutorId&gt;</c>, e o ouvinte gravava o vínculo para
/// qualquer <c>tutorId</c> que chegasse — sem verificar que quem mandou é o dono
/// daquele id. O bot é público por natureza: qualquer pessoa no Telegram digitava
/// <c>/start &lt;uuid de outro tutor&gt;</c> e passava a receber, no próprio celular,
/// os lembretes daquele tutor; <c>/meusanimais</c> e <c>/meuslembretes</c> devolviam
/// os pets e os lembretes dele; e como <c>VincularAsync</c> sobrescreve o vínculo
/// existente, o dono de verdade parava de receber qualquer notificação — inclusive o
/// WhatsApp, porque o envio ao Telegram tinha "sucesso" e a alternativa nem era
/// tentada. O <c>tutorId</c> não é segredo: ele viaja em <c>/auth/me</c> e no corpo
/// de cada animal.
/// </para>
///
/// <para>
/// <b>O desenho.</b> O que vai na URL passa a ser um token de 256 bits, sorteado por
/// <see cref="RandomNumberGenerator"/>, que só existe porque alguém autenticado pediu
/// um link para aquele tutor. Ele vale uma vez e por quinze minutos. Saber o
/// <c>tutorId</c> deixa de servir para alguma coisa.
/// </para>
///
/// <para>
/// <b>Por que na memória, e não numa tabela.</b> A vida inteira de um convite são os
/// segundos entre o app mostrar o link e o tutor tocar nele — e este processo é o
/// mesmo que gera e que consome, porque o ouvinte do Telegram roda dentro da API. Em
/// troca, aceita-se uma limitação explícita: com mais de uma instância, ou depois de
/// um restart, um convite ainda não usado deixa de valer e o tutor pede outro. O
/// custo alternativo seria uma coluna nova numa tabela que já tem DDL escrito para
/// Oracle e para MySQL, num deploy que ainda não rodou — caro demais para fechar um
/// buraco que este dicionário fecha por inteiro.
/// </para>
/// </summary>
public class VinculosPendentesDeTelegram
{
    /// <summary>
    /// Quinze minutos. É folgado para quem pediu o link e foi procurar o celular, e
    /// curto para um token que porventura vaze num print de tela ou num log.
    /// </summary>
    public static readonly TimeSpan Validade = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, Convite> _convites = new(StringComparer.Ordinal);
    private readonly TimeProvider _relogio;

    public VinculosPendentesDeTelegram(TimeProvider? relogio = null)
        => _relogio = relogio ?? TimeProvider.System;

    private readonly record struct Convite(string TutorId, DateTimeOffset ExpiraEm);

    /// <summary>
    /// Sorteia um convite para o tutor e devolve o token que vai na URL.
    /// </summary>
    /// <remarks>
    /// O token é Base64 na variante URL, sem preenchimento, o que dá 43 caracteres
    /// no alfabeto <c>A-Z a-z 0-9 - _</c>. Não é enfeite: o parâmetro <c>start</c> do
    /// Telegram aceita <b>somente</b> esse alfabeto e no máximo 64 caracteres. Um
    /// Base64 comum traria <c>+</c>, <c>/</c> e <c>=</c>, e o Telegram recusaria o
    /// link inteiro — em silêncio, do lado do cliente, onde nenhum log desta API
    /// veria.
    /// </remarks>
    public string Gerar(string tutorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tutorId);

        // Faxina oportunista: sem ela, um token nunca resgatado ficaria na memória
        // para sempre. Roda no caminho raro (gerar), nunca no de consumir.
        RemoverExpirados();

        var token = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        _convites[token] = new Convite(tutorId, _relogio.GetUtcNow().Add(Validade));
        return token;
    }

    /// <summary>
    /// Troca o token pelo tutor, de uma vez só. Devolve <c>null</c> para token
    /// desconhecido, já usado ou vencido — os três casos são indistinguíveis para
    /// quem chama, de propósito.
    /// </summary>
    public string? Consumir(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        // TryRemove é o que torna o convite de uso único, e é atômico: dois /start
        // com o mesmo token, ao mesmo tempo, só podem ter um vencedor.
        if (!_convites.TryRemove(token, out var convite)) return null;

        return convite.ExpiraEm > _relogio.GetUtcNow() ? convite.TutorId : null;
    }

    /// <summary>Quantos convites ainda estão de pé. Existe para os testes.</summary>
    public int Pendentes
    {
        get
        {
            RemoverExpirados();
            return _convites.Count;
        }
    }

    private void RemoverExpirados()
    {
        var agora = _relogio.GetUtcNow();
        foreach (var (token, convite) in _convites)
        {
            if (convite.ExpiraEm <= agora) _convites.TryRemove(token, out _);
        }
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
