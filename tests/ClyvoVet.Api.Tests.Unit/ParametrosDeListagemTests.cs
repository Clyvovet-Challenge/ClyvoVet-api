using ClyvoVet.Api.Listagem;
using ClyvoVet.Application.Common;
using ClyvoVet.Domain.Exceptions;

namespace ClyvoVet.Api.Tests.Unit;

public class ParametrosDeListagemTests
{
    [Fact]
    public void Montar_ParametrosValidosSemOrdenacao_DevolveConsultaSimples()
    {
        var consulta = ParametrosDeListagem.Montar(2, 25, null, null);

        Assert.Equal(new ConsultaPaginada(2, 25), consulta);
    }

    [Theory]
    [InlineData(0, 10, "O parâmetro 'page' deve ser maior que zero.")]
    [InlineData(-3, 10, "O parâmetro 'page' deve ser maior que zero.")]
    [InlineData(1, 0, "O parâmetro 'pageSize' deve estar entre 1 e 100.")]
    [InlineData(1, 101, "O parâmetro 'pageSize' deve estar entre 1 e 100.")]
    public void Montar_PaginaOuTamanhoInvalido_LancaBadRequestComAMensagemDeSempre(int page, int pageSize, string mensagem)
    {
        var erro = Assert.Throws<BadRequestException>(() => ParametrosDeListagem.Montar(page, pageSize, null, null));

        Assert.Equal(mensagem, erro.Message);
    }

    [Theory]
    [InlineData("desc", DirecaoOrdenacao.Desc)]
    [InlineData("DESC", DirecaoOrdenacao.Desc)]
    [InlineData("asc", DirecaoOrdenacao.Asc)]
    [InlineData(null, DirecaoOrdenacao.Asc)]
    [InlineData("", DirecaoOrdenacao.Asc)]
    public void Montar_ComOrdenarPor_LeADirecaoSemDistinguirCaixa(string? direcao, DirecaoOrdenacao esperada)
    {
        var consulta = ParametrosDeListagem.Montar(1, 10, "  titulo ", direcao);

        Assert.Equal("titulo", consulta.OrdenarPor);
        Assert.Equal(esperada, consulta.Direcao);
    }

    [Fact]
    public void Montar_DirecaoInvalidaComOrdenarPor_LancaBadRequest()
    {
        var erro = Assert.Throws<BadRequestException>(() => ParametrosDeListagem.Montar(1, 10, "titulo", "sideways"));

        Assert.Equal("Direção 'sideways' inválida. Use 'asc' ou 'desc'.", erro.Message);
    }

    [Fact]
    public void Montar_DirecaoSemOrdenarPor_EhIgnoradaAteQuandoInvalida()
    {
        // O design §6.3: "direcao sem ordenarPor é ignorada" — inclusive não é validada.
        var consulta = ParametrosDeListagem.Montar(1, 10, null, "sideways");

        Assert.Equal(new ConsultaPaginada(1, 10), consulta);
    }
}
