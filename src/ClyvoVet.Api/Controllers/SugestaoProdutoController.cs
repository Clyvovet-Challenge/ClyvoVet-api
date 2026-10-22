using ClyvoVet.Api.Hateoas;
using ClyvoVet.Api.Listagem;
using ClyvoVet.Application.DTOs.Request;
using ClyvoVet.Api.Filters;
using ClyvoVet.Application.Security;
using ClyvoVet.Application.Services.Interfaces;
using ClyvoVet.Api.Security;
using Microsoft.AspNetCore.Authorization;
using ClyvoVet.Api.Swagger;
using ClyvoVet.Application.DTOs.Response;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVet.Api.Controllers;

/// <summary>
/// Gerencia sugestões de produto vinculadas a um animal.
/// Tabela: <c>t_clyvo_sugestao_produto</c>
/// — requer <c>animal_id</c> válido em <c>t_clyvo_animal</c> e
///   <c>produto_id</c> válido em <c>t_clyvo_produto</c>.
/// </summary>
[ApiController]
[Route("api/v1/sugestoes-produto")]
[Produces("application/json")]
[Authorize(Policy = PoliticasDeAcesso.Autenticado)]
[TypeFilter(typeof(ApiKeyFilterAttribute), Arguments = new object[] { "Api:ApiKey" })]
public class SugestaoProdutoController : ControllerBase
{
    private readonly ISugestaoProdutoService _service;
    private readonly EscopoDoTutor _escopo;
    private readonly GeradorDeLinks _links;

    public SugestaoProdutoController(ISugestaoProdutoService service, EscopoDoTutor escopo, GeradorDeLinks links)
    {
        _service = service;
        _escopo = escopo;
        _links = links;
    }

    /// <summary>404 se a sugestao existente nao for de um animal do tutor.</summary>
    private async Task ExigirPropriedadeAsync(string id)
    {
        if (!_escopo.Ativo) return;

        var sugestao = await _service.GetByIdAsync(id);   // ja lanca 404 se nao existe
        await _escopo.ExigirAnimalDoTutorAsync(sugestao.AnimalId, $"Sugestao {id} nao encontrada.");
    }

    /// <summary>Lista sugestões com paginação, ordenação e filtros opcionais.</summary>
    /// <param name="page">Número da página (padrão: 1).</param>
    /// <param name="pageSize">Itens por página — máx. 100 (padrão: 10).</param>
    /// <param name="animalId">Filtra pelo UUID do animal.</param>
    /// <param name="ativo">Filtra por sugestão ativa. Omitido, traz ativas e inativas.</param>
    /// <param name="ordenarPor">
    /// Campo de ordenação (camelCase): <c>animalId | nomeAnimal | produtoId | nomeProduto | justificativa |
    /// dataSugestao | ativo | criadoEm</c>. Omitido, mantém a ordem de sempre (<c>dataSugestao</c>,
    /// da mais recente para a mais antiga). Campo fora da lista responde 400.
    /// </param>
    /// <param name="direcao"><c>asc</c> (padrão) ou <c>desc</c>. Ignorada sem <c>ordenarPor</c>.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SugestaoProdutoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ListagemHateoas(typeof(SugestaoProdutoResponse))]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? animalId = null,
        [FromQuery] bool? ativo = null,
        [FromQuery] string? ordenarPor = null,
        [FromQuery] string? direcao = null)
    {
        var consulta = ParametrosDeListagem.Montar(page, pageSize, ordenarPor, direcao);

        var pagina = await _service.GetAllAsync(consulta, animalId, _escopo.FiltroDeListagem(), ativo);
        foreach (var item in pagina.Itens) _links.Sugestao(item);
        return this.RespostaDeListagem(pagina);
    }

    /// <summary>Retorna uma sugestão de produto pelo ID (UUID).</summary>
    /// <param name="id">UUID da sugestão.</param>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _service.GetByIdAsync(id);

        await _escopo.ExigirAnimalDoTutorAsync(result.AnimalId, $"Sugestao {id} nao encontrada.");

        _links.Sugestao(result);
        return Ok(result);
    }

    /// <summary>
    /// Cria uma nova sugestão de produto.
    /// O <c>id</c> é gerado pela própria API (<c>Guid.NewGuid()</c> no repositório).
    /// Valida a existência de <c>animalId</c> e <c>produtoId</c> antes de salvar.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] SugestaoProdutoRequest request)
    {
        await _escopo.ExigirAnimalDoTutorAsync(request.AnimalId, $"Animal {request.AnimalId} nao encontrado.");

        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Atualiza uma sugestão de produto existente.</summary>
    /// <param name="id">UUID da sugestão a atualizar.</param>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] SugestaoProdutoRequest request)
    {
        // Os DOIS animais: o da sugestao atual e o do corpo. Checar so um permitiria
        // transferir a sugestao para fora, ou sequestrar a alheia.
        await ExigirPropriedadeAsync(id);

        await _escopo.ExigirAnimalDoTutorAsync(request.AnimalId, $"Animal {request.AnimalId} nao encontrado.");

        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    /// <summary>Remove uma sugestão de produto pelo ID.</summary>
    /// <param name="id">UUID da sugestão a remover.</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        await ExigirPropriedadeAsync(id);

        await _service.DeleteAsync(id);
        return NoContent();
    }
}
