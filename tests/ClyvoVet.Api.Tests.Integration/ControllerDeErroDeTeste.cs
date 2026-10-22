using ClyvoVet.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVet.Api.Tests.Integration;

/// <summary>
/// Rotas que só existem nos testes e sempre falham. Servem para provar a resposta de erro pelo
/// pipeline REAL (o <c>ExceptionHandlerMiddleware</c> incluso): um teste de unidade que monta o
/// contexto à mão não enxerga o que o pipeline faz com os headers da resposta.
/// </summary>
[ApiController]
[Route("teste-erro")]
public class ControllerDeErroDeTeste : ControllerBase
{
    [HttpGet("servidor")]
    public IActionResult FalhaDeServidor() =>
        throw new InvalidOperationException("detalhe interno que não deve vazar");

    [HttpGet("nao-encontrado")]
    public IActionResult ErroDoUsuario() =>
        throw new NotFoundException("Registro de teste não encontrado.");
}
