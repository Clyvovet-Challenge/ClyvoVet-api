using ClyvoVet.Api.Errors;
using ClyvoVet.Api.Exceptions;
using ClyvoVet.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Api.Tests.Unit;

/// <summary>
/// A tradução de exceção para HTTP, que é onde a paridade com a API Java mora.
///
/// <para>
/// Estes testes existem em UNIDADE, e não em integração, por um motivo concreto:
/// o caso do 409 só acontece quando uma chave estrangeira real recusa o delete,
/// e os testes de integração rodam em EF Core InMemory, que não aplica FK. Lá o
/// mesmo delete responde 204, e um teste escrito ali passaria pelo motivo
/// errado — exatamente o ponto cego que já deixou passar um 500 neste projeto.
/// </para>
/// </summary>
public class MapaDeErroTests
{
    /// <summary>
    /// Registro em uso é 409, e não 500.
    ///
    /// A FK recusando é a regra funcionando, não a API quebrando. 500 diria a
    /// coisa errada ao app, ao log e a quem estiver avaliando — e a API Java
    /// responde 409 na mesma situação.
    /// </summary>
    [Fact]
    public void FalhaDeChaveEstrangeira_Vira409()
    {
        var excecao = new DbUpdateException("FK violation", (Exception?)null);

        Assert.Equal(StatusCodes.Status409Conflict, MapaDeErro.Status(excecao));
    }

    /// <summary>
    /// E a mensagem não pode citar tabela nem constraint.
    ///
    /// O texto do EF traz o nome da FK e da tabela: detalhe de banco que não
    /// ajuda quem lê a tela e que descreve o schema para quem não deveria vê-lo.
    /// </summary>
    [Fact]
    public void FalhaDeChaveEstrangeira_NaoVazaOSchemaNaMensagem()
    {
        var excecao = new DbUpdateException(
            "The DELETE statement conflicted with the REFERENCE constraint "
                + "\"fk_sugestao_produto\" on table \"t_clyvo_sugestao_produto\".",
            (Exception?)null);

        var mensagem = MapaDeErro.Mensagem(excecao);

        Assert.Equal("Registro em uso por outro cadastro.", mensagem);
        Assert.DoesNotContain("fk_sugestao_produto", mensagem);
        Assert.DoesNotContain("t_clyvo_", mensagem);
    }

    [Fact]
    public void NaoEncontrado_Vira404ComAPropriaMensagem()
    {
        var excecao = new NotFoundException("Produto com id abc não encontrado.");

        Assert.Equal(StatusCodes.Status404NotFound, MapaDeErro.Status(excecao));
        Assert.Equal("Produto com id abc não encontrado.", MapaDeErro.Mensagem(excecao));
    }

    [Fact]
    public void RequisicaoInvalida_Vira400()
    {
        Assert.Equal(
            StatusCodes.Status400BadRequest,
            MapaDeErro.Status(new BadRequestException("data no passado")));
    }

    /// <summary>
    /// Recorte ligado e requisição sem tutor é 403, não 401: a X-Api-Key foi
    /// aceita — o que falta é identidade, não credencial.
    /// </summary>
    [Fact]
    public void SemTutorNoToken_Vira403()
    {
        Assert.Equal(
            StatusCodes.Status403Forbidden,
            MapaDeErro.Status(new SemTutorNoTokenException()));
    }

    /// <summary>
    /// O que não é conhecido continua 500 com mensagem genérica — uma exceção
    /// inesperada não deve descrever a si mesma para quem chamou.
    /// </summary>
    [Fact]
    public void ExcecaoDesconhecida_Vira500Generico()
    {
        var excecao = new InvalidOperationException("detalhe interno que nao deve vazar");

        Assert.Equal(StatusCodes.Status500InternalServerError, MapaDeErro.Status(excecao));
        Assert.Equal("Erro interno no servidor.", MapaDeErro.Mensagem(excecao));
    }
}
