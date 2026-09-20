namespace ClyvoVet.Domain.Exceptions;

/// <summary>
/// O registro está em uso por outro cadastro e não pode ser alterado ou removido.
///
/// <para>
/// Existe para que a Application e a Api não precisem conhecer o EF Core: quem
/// fala com o banco (<c>AppDbContext</c>) traduz a falha de integridade para cá.
/// </para>
///
/// <para>
/// A mensagem é fixa de propósito. O texto do provedor cita tabela e constraint —
/// detalhe de banco que não ajuda quem lê a tela e que descreve o schema para quem
/// não deveria vê-lo. A causa original fica em <see cref="Exception.InnerException"/>,
/// para o log.
/// </para>
/// </summary>
public class RegistroEmUsoException : Exception
{
    public const string MensagemPadrao = "Registro em uso por outro cadastro.";

    public RegistroEmUsoException(Exception? causa = null) : base(MensagemPadrao, causa) { }
}
