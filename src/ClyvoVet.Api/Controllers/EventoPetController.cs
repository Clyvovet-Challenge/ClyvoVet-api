using ClyvoVet.Api.Listagem;
using ClyvoVet.Application.DTOs.Request;
using ClyvoVet.Domain.Enums;
using ClyvoVet.Api.Filters;
using ClyvoVet.Application.Services.Interfaces;
using ClyvoVet.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVet.Api.Controllers;

/// <summary>
/// Gerencia eventos públicos para pets (feiras, vacinações, workshops etc.).
/// Tabela: <c>t_clyvo_evento_pet</c>
/// </summary>
[ApiController]
[Route("api/v1/eventos-pet")]
[Produces("application/json")]
[Authorize(Policy = PoliticasDeAcesso.Autenticado)]
[TypeFilter(typeof(ApiKeyFilterAttribute), Arguments = new object[] { "Api:ApiKey" })]
public class EventoPetController : ControllerBase
{
    private readonly IEventoPetService _service;

    public EventoPetController(IEventoPetService service) => _service = service;

    /// <summary>Lista eventos pet com paginação, ordenação e filtros opcionais.</summary>
    /// <param name="page">Número da página (padrão: 1).</param>
    /// <param name="pageSize">Itens por página — máx. 100 (padrão: 10).</param>
    /// <param name="cidade">Filtro por cidade (case-insensitive).</param>
    /// <param name="tipo">Filtro: <c>Vacinacao | Feira | Castracao | Workshop | Outro</c></param>
    /// <param name="especieAlvo">Filtro: <c>Cachorro | Gato | Passaro | Reptil | Roedor | Todos | Outro | Bovino | Equino</c></param>
    /// <param name="ordenarPor">
    /// Campo de ordenação (camelCase): <c>titulo | descricao | tipo | rua | numero | bairro | cidade | estado |
    /// cep | dataInicio | dataFim | especieAlvo | organizador | gratuito | linkInscricao | ativo | criadoEm</c>.
    /// Omitido, mantém a ordem de sempre (<c>dataInicio</c>). Campo fora da lista responde 400.
    /// </param>
    /// <param name="direcao"><c>asc</c> (padrão) ou <c>desc</c>. Ignorada sem <c>ordenarPor</c>.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? cidade = null,
        [FromQuery] TipoEventoPetEnum? tipo = null,
        [FromQuery] EspecieEnum? especieAlvo = null,
        [FromQuery] string? ordenarPor = null,
        [FromQuery] string? direcao = null)
    {
        var consulta = ParametrosDeListagem.Montar(page, pageSize, ordenarPor, direcao);

        var pagina = await _service.GetAllAsync(consulta, cidade, tipo, especieAlvo);
        return this.RespostaDeListagem(pagina);
    }

    /// <summary>Retorna um evento pet pelo ID (UUID).</summary>
    /// <param name="id">UUID do evento.</param>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Cadastra um novo evento pet.
    /// O <c>id</c> é gerado pela API, e não pelo banco: o MySQL não tem RETURNING,
    /// então depender de um DEFAULT exigiria ler a linha de volta.
    /// <c>dataInicio</c> não pode ser no passado.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] EventoPetRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Atualiza um evento pet existente.
    /// Eventos já iniciados podem ser editados; <c>dataInicio</c> só pode ser
    /// alterada para uma data futura.
    /// </summary>
    /// <param name="id">UUID do evento a atualizar.</param>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] EventoPetRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    /// <summary>Remove um evento pet pelo ID.</summary>
    /// <param name="id">UUID do evento a remover.</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
