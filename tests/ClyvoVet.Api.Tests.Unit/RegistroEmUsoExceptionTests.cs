using ClyvoVet.Domain.Exceptions;

namespace ClyvoVet.Api.Tests.Unit;

public class RegistroEmUsoExceptionTests
{
    [Fact]
    public void Construtor_SemCausa_UsaAMensagemPadrao()
    {
        // Arrange & Act
        var excecao = new RegistroEmUsoException();

        // Assert
        Assert.Equal("Registro em uso por outro cadastro.", excecao.Message);
        Assert.Null(excecao.InnerException);
    }

    [Fact]
    public void Construtor_ComCausa_PreservaAExcecaoOriginalSemVazarSuaMensagem()
    {
        // Arrange
        var causa = new InvalidOperationException(
            "FK fk_sugestao_produto na tabela t_clyvo_sugestao_produto");

        // Act
        var excecao = new RegistroEmUsoException(causa);

        // Assert
        Assert.Same(causa, excecao.InnerException);
        Assert.Equal("Registro em uso por outro cadastro.", excecao.Message);
    }
}
