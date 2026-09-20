using ClyvoVet.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVet.Api.Tests.Integration;

[ApiController]
[Route("teste-autenticacao")]
public class ControllerDeAutenticacaoDeTeste : ControllerBase
{
    [HttpGet("autenticado")]
    [Authorize(Policy = PoliticasDeAcesso.Autenticado)]
    public IActionResult Autenticado() => Ok();

    [HttpGet("equipe")]
    [Authorize(Policy = PoliticasDeAcesso.Equipe)]
    public IActionResult Equipe() => Ok();
}
