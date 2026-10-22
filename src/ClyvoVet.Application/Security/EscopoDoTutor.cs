using Microsoft.Extensions.Configuration;
using ClyvoVet.Application.Abstractions.Repositories;
using ClyvoVet.Domain.Exceptions;

namespace ClyvoVet.Application.Security;

/// <summary>
/// Decide se a requisição atual deve enxergar apenas os dados do próprio tutor.
///
/// <para>
/// <b>Ligado por padrão.</b> Nasceu desligado, quando a <c>X-Api-Key</c> era a única
/// barreira e ligar dependia de o app mandar token. Com o JWT obrigatório, desligado
/// significava um tutor listar os lembretes de todos e gerar convite do Telegram para o
/// tutor alheio. <c>Api:EscopoPorTutor=false</c> continua sendo a alavanca de emergência,
/// uma app setting, sem redeploy.
/// </para>
///
/// <para>
/// <b>Por que aqui e não no repositório.</b> Os dois <c>BackgroundService</c> desta
/// API — o de notificação de lembretes e o ouvinte do Telegram — vão direto aos
/// repositórios, sem <c>HttpContext</c>. Um filtro global no <c>DbContext</c> ou no
/// repositório os faria parar de encontrar lembretes <b>em silêncio</b>: nenhuma
/// exceção, nenhum log de erro, apenas notificações que deixam de sair. O recorte
/// vive na borda HTTP, que é onde existe um chamador para recortar.
/// </para>
/// </summary>
public class EscopoDoTutor(
    IConfiguration configuracao,
    IUsuarioAtual usuario,
    IAnimalRepository animais)
{
    private const string ChaveDaFlag = "Api:EscopoPorTutor";

    /// <summary>
    /// Se o recorte está ligado.
    ///
    /// <para>
    /// A leitura é tolerante de propósito. <c>IConfiguration.GetValue&lt;bool&gt;</c>
    /// <b>lança</b> quando o valor não é "true"/"false" — e um operador apressado
    /// escreve <c>Api__EscopoPorTutor=0</c>. O interruptor de emergência não pode
    /// ser capaz de derrubar a própria API que ele existe para salvar. Valor
    /// ilegível deixa o recorte <b>ligado</b>, como no <c>AcessoHandler</c>: falha
    /// para o lado seguro.
    /// </para>
    /// </summary>
    public bool Ativo => LerFlag(configuracao[ChaveDaFlag]);

    private static bool LerFlag(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return true;
        var limpo = valor.Trim();
        return bool.TryParse(limpo, out var booleano)
            ? booleano
            : limpo is not ("0" or "nao" or "no");
    }

    /// <summary>O tutor autenticado, ou <c>null</c> quando não há token de tutor.</summary>
    public string? TutorId => usuario.Identidade?.TutorId;

    /// <summary>
    /// O tutor cujos dados a requisição pode ver, para usar como filtro de
    /// listagem. <c>null</c> significa <b>sem filtro</b>, e só é devolvido quando o
    /// recorte está desligado.
    /// </summary>
    /// <exception cref="SemTutorNoTokenException">
    /// Quando o recorte está ligado e não há tutor no token. É o caso de ADMIN, de
    /// VETERINARIO, e de qualquer chamada que traga só a <c>X-Api-Key</c>.
    ///
    /// <para>
    /// Lançar aqui é o ponto inteiro desta classe. O idioma natural seria
    /// <c>if (tutorId != null) query = query.Where(...)</c> — e ele devolveria a
    /// base inteira exatamente para os perfis mais poderosos e para quem não se
    /// identificou. É o mesmo erro que a API Java já documenta ter cometido uma vez.
    /// </para>
    /// </exception>
    public string? FiltroDeListagem()
    {
        if (!Ativo) return null;

        var tutor = TutorId;
        if (string.IsNullOrEmpty(tutor))
            throw new SemTutorNoTokenException();

        return tutor;
    }

    /// <summary>
    /// Garante que o animal pertence ao tutor da requisição.
    ///
    /// <para>
    /// Devolve <c>false</c> para animal inexistente <b>e</b> para animal de outro
    /// tutor — os dois casos viram 404 no controller, de propósito. Um 403 aqui
    /// contaria ao chamador que o recurso existe, que é informação que ele não tem
    /// direito de saber.
    /// </para>
    /// </summary>
    public async Task<bool> AnimalEDoTutorAsync(string? animalId)
    {
        if (!Ativo) return true;

        var tutor = TutorId;
        if (string.IsNullOrEmpty(tutor)) throw new SemTutorNoTokenException();
        if (string.IsNullOrEmpty(animalId)) return false;

        var animal = await animais.GetByIdAsync(animalId);
        return animal is not null && animal.TutorId == tutor;
    }

    /// <summary>
    /// A forma que os controllers usam: animal que não é do tutor vira 404 com a mensagem
    /// da rota (o recurso pedido, não o animal). Com o recorte desligado, não faz nada.
    /// </summary>
    public async Task ExigirAnimalDoTutorAsync(string? animalId, string mensagemDeNaoEncontrado)
    {
        if (!await AnimalEDoTutorAsync(animalId))
            throw new NotFoundException(mensagemDeNaoEncontrado);
    }

    /// <summary>
    /// Se a requisição pode agir em nome deste tutor. Para rotas cujo recurso É o tutor
    /// (convite e vínculo do Telegram): lá o id já é conhecido de quem chama, então a
    /// resposta é 403, e não o 404 dos recursos de outro tutor. Sem tutor no token, nega.
    /// </summary>
    public bool PermiteTutor(string tutorId) =>
        !Ativo || string.Equals(TutorId, tutorId, StringComparison.Ordinal);
}

/// <summary>
/// O recorte está ligado, mas a requisição não identifica um tutor. Vira 403.
/// </summary>
public class SemTutorNoTokenException() : Exception(
    "Esta rota exige um token de tutor. Envie o header Authorization com o access " +
    "token emitido pela API de cadastro.");
