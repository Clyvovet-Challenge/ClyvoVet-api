using ClyvoVet.Api.Hateoas;
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
/// Gerencia o catálogo de produtos e serviços veterinários.
/// Tabela: <c>t_clyvo_produto</c>
/// </summary>
[ApiController]
[Route("api/v1/produtos")]
[Produces("application/json")]
[Authorize(Policy = PoliticasDeAcesso.Autenticado)]
[TypeFilter(typeof(ApiKeyFilterAttribute), Arguments = new object[] { "Api:ApiKey" })]
public class ProdutoController : ControllerBase
{
    private readonly IProdutoService _service;
    private readonly GeradorDeLinks _links;

    public ProdutoController(IProdutoService service, GeradorDeLinks links)
    {
        _service = service;
        _links = links;
    }

    /// <summary>Lista produtos com paginação e filtros opcionais.</summary>
    /// <param name="page">Número da página (padrão: 1).</param>
    /// <param name="pageSize">Itens por página — máx. 100 (padrão: 10).</param>
    /// <param name="categoria">Filtro por categoria: <c>Racao | Medicamento | Acessorio | Servico | Outro</c></param>
    /// <param name="especieIndicada">
    /// Filtro por espécie: <c>Cachorro | Gato | Passaro | Reptil | Roedor | Todos | Outro | Bovino | Equino</c>.
    /// Pedir uma espécie traz também os produtos marcados <c>Todos</c> — sem isso,
    /// perguntar "o que serve para um cachorro" esconderia justamente o que serve
    /// para qualquer animal.
    /// </param>
    /// <param name="porteIndicado">
    /// Filtro por porte: <c>Pequeno | Medio | Grande | Todos</c>. Como a
    /// espécie, pedir um porte traz também os marcados <c>Todos</c> — consulta,
    /// shampoo neutro, vermífugo em gotas, que não têm porte e servem a
    /// qualquer animal.
    /// </param>
    /// <param name="ativo">
    /// Filtra por produto ativo. Omitido, traz ativos e inativos — é o
    /// comportamento de sempre, mantido para não quebrar quem já chamava. A
    /// vitrine do tutor pede <c>true</c>; a gestão da clínica omite, porque
    /// precisa ver o que desativou para poder reativar.
    /// </param>
    /// <param name="ordenarPor">
    /// Campo de ordenação (camelCase): <c>nome | descricao | categoria | preco | especieIndicada |
    /// porteIndicado | ativo | criadoEm</c>. Omitido, mantém a ordem de sempre (<c>nome</c>). Campo fora
    /// da lista responde 400.
    /// </param>
    /// <param name="direcao"><c>asc</c> (padrão) ou <c>desc</c>. Ignorada sem <c>ordenarPor</c>.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] CategoriaEnum? categoria = null,
        [FromQuery] EspecieEnum? especieIndicada = null,
        [FromQuery] bool? ativo = null,
        [FromQuery] PorteEnum? porteIndicado = null,
        [FromQuery] string? ordenarPor = null,
        [FromQuery] string? direcao = null)
    {
        var consulta = ParametrosDeListagem.Montar(page, pageSize, ordenarPor, direcao);

        var pagina = await _service.GetAllAsync(consulta, categoria, especieIndicada, ativo, porteIndicado);
        foreach (var item in pagina.Itens) _links.Produto(item);
        return this.RespostaDeListagem(pagina);
    }

    /// <summary>Retorna um produto pelo ID (UUID).</summary>
    /// <param name="id">UUID do produto.</param>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _service.GetByIdAsync(id);
        _links.Produto(result);
        return Ok(result);
    }

    /// <summary>
    /// Cadastra um novo produto.
    /// O <c>id</c> é gerado pela API, e não pelo banco: o MySQL não tem RETURNING,
    /// então depender de um DEFAULT exigiria ler a linha de volta.
    /// </summary>
    [Authorize(Policy = PoliticasDeAcesso.Equipe)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] ProdutoRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Atualiza um produto existente.</summary>
    /// <param name="id">UUID do produto a atualizar.</param>
    [Authorize(Policy = PoliticasDeAcesso.Equipe)]
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] ProdutoRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    /// <summary>Remove um produto pelo ID.</summary>
    /// <param name="id">UUID do produto a remover.</param>
    [Authorize(Policy = PoliticasDeAcesso.Equipe)]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
