using ClyvoVet.Api.Hateoas;
using ClyvoVet.Api.Listagem;
using ClyvoVet.Application.DTOs.Request;
using ClyvoVet.Domain.Enums;
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
/// Gerencia lembretes de cuidados vinculados a um animal.
/// Tabela: <c>t_clyvo_lembrete</c>
/// — requer <c>animal_id</c> válido em <c>t_clyvo_animal</c> (domínio Java).
/// </summary>
[ApiController]
[Route("api/v1/lembretes")]
[Produces("application/json")]
[Authorize(Policy = PoliticasDeAcesso.Autenticado)]
[TypeFilter(typeof(ApiKeyFilterAttribute), Arguments = new object[] { "Api:ApiKey" })]
public class LembreteController : ControllerBase
{
    private readonly ILembreteService _service;
    private readonly EscopoDoTutor _escopo;
    private readonly GeradorDeLinks _links;

    public LembreteController(ILembreteService service, EscopoDoTutor escopo, GeradorDeLinks links)
    {
        _service = service;
        _escopo = escopo;
        _links = links;
    }

    /// <summary>
    /// Falha com 404 se o lembrete existente nao for de um animal do tutor.
    ///
    /// <para>
    /// 404 e nao 403 de proposito: 403 confirmaria que o lembrete existe, e a
    /// existencia ja e informacao. Para quem nao e dono, o recurso simplesmente
    /// nao esta la.
    /// </para>
    ///
    /// <para>
    /// Com o recorte desligado nao faz consulta nenhuma e nao muda o caminho.
    /// </para>
    /// </summary>
    private async Task ExigirPropriedadeDoLembreteAsync(string id)
    {
        if (!_escopo.Ativo) return;

        var lembrete = await _service.GetByIdAsync(id);   // ja lanca 404 se nao existe
        await _escopo.ExigirAnimalDoTutorAsync(lembrete.AnimalId, $"Lembrete {id} nao encontrado.");
    }

    /// <summary>Lista lembretes com paginação, ordenação e filtros opcionais.</summary>
    /// <param name="page">Número da página (padrão: 1).</param>
    /// <param name="pageSize">Itens por página — máx. 100 (padrão: 10).</param>
    /// <param name="animalId">Filtra pelo UUID do animal.</param>
    /// <param name="status">Filtro: <c>Pendente | Enviado | Cancelado</c></param>
    /// <param name="tipo">Filtro: <c>Vacina | Medicamento | Consulta | Higiene | Outro</c></param>
    /// <param name="ordenarPor">
    /// Campo de ordenação (camelCase): <c>animalId | nomeAnimal | titulo | descricao | tipo | agendadoEm |
    /// recorrente | intervaloDias | repetirAte | status | criadoEm</c>. Omitido, mantém a ordem de sempre
    /// (<c>agendadoEm</c>). Campo fora da lista responde 400.
    /// </param>
    /// <param name="direcao"><c>asc</c> (padrão) ou <c>desc</c>. Ignorada sem <c>ordenarPor</c>.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<LembreteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ListagemHateoas(typeof(LembreteResponse))]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? animalId = null,
        [FromQuery] StatusLembreteEnum? status = null,
        [FromQuery] TipoLembreteEnum? tipo = null,
        [FromQuery] string? ordenarPor = null,
        [FromQuery] string? direcao = null)
    {
        var consulta = ParametrosDeListagem.Montar(page, pageSize, ordenarPor, direcao);

        // FiltroDeListagem() devolve null quando o recorte esta desligado (sem
        // filtro, comportamento de sempre) e LANCA quando esta ligado sem tutor no
        // token. Nao existe um terceiro caminho em que ele devolve null com o
        // recorte ligado -- que seria justamente o bug de "filtro opcional"
        // devolvendo a base inteira para ADMIN, VETERINARIO e para quem so mandou
        // a X-Api-Key.
        var pagina = await _service.GetAllAsync(consulta, animalId, status, tipo, _escopo.FiltroDeListagem());
        foreach (var item in pagina.Itens) _links.Lembrete(item);
        return this.RespostaDeListagem(pagina);
    }

    /// <summary>Retorna um lembrete pelo ID (UUID).</summary>
    /// <param name="id">UUID do lembrete.</param>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _service.GetByIdAsync(id);

        await _escopo.ExigirAnimalDoTutorAsync(result.AnimalId, $"Lembrete {id} nao encontrado.");

        _links.Lembrete(result);
        return Ok(result);
    }

    /// <summary>
    /// Cria um novo lembrete.
    /// O <c>id</c> é gerado pela própria API (<c>Guid.NewGuid()</c> no repositório).
    /// O campo <c>status</c> é forçado a <c>Pendente</c> independente do valor enviado.
    /// <c>agendadoEm</c> deve ser uma data/hora futura.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] LembreteRequest request)
    {
        // O POST tambem entra no recorte, e nao e detalhe: e o unico endpoint que
        // agenda uma NOTIFICACAO. Sem ele aqui, qualquer portador da X-Api-Key
        // marcaria lembrete no animal de outro tutor -- e o dono receberia o
        // WhatsApp ou o Telegram sem nunca ter pedido.
        await _escopo.ExigirAnimalDoTutorAsync(request.AnimalId, $"Animal {request.AnimalId} nao encontrado.");

        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Atualiza um lembrete existente.
    /// <c>agendadoEm</c> deve ser uma data/hora futura. O status não vem do corpo:
    /// mudar a data de um lembrete enviado o agenda de novo (volta a Pendente).
    /// </summary>
    /// <param name="id">UUID do lembrete a atualizar.</param>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] LembreteRequest request)
    {
        // DOIS animais precisam ser do tutor, e nao um.
        //
        // O PUT reescreve o AnimalId. Checar so o dono do lembrete EXISTENTE
        // deixaria transferi-lo para o animal de outro tutor -- e checar so o
        // AnimalId NOVO deixaria sequestrar o lembrete alheio. As duas checagens
        // fecham as duas metades.
        await ExigirPropriedadeDoLembreteAsync(id);

        await _escopo.ExigirAnimalDoTutorAsync(request.AnimalId, $"Animal {request.AnimalId} nao encontrado.");

        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    /// <summary>Remove um lembrete pelo ID.</summary>
    /// <param name="id">UUID do lembrete a remover.</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        await ExigirPropriedadeDoLembreteAsync(id);

        await _service.DeleteAsync(id);
        return NoContent();
    }
}
