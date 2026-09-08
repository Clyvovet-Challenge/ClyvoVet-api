using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Exceptions;
using ClyvoVet.Api.Filters;
using ClyvoVet.Api.Security;
using ClyvoVet.Api.Services.Interfaces;
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
[TypeFilter(typeof(ApiKeyFilterAttribute), Arguments = new object[] { "Api:ApiKey" })]
public class LembreteController : ControllerBase
{
    private readonly ILembreteService _service;
    private readonly EscopoDoTutor _escopo;

    public LembreteController(ILembreteService service, EscopoDoTutor escopo)
    {
        _service = service;
        _escopo = escopo;
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
        if (!await _escopo.AnimalEDoTutorAsync(lembrete.AnimalId))
            throw new NotFoundException($"Lembrete {id} nao encontrado.");
    }

    /// <summary>Lista lembretes com paginação e filtros opcionais.</summary>
    /// <param name="page">Número da página (padrão: 1).</param>
    /// <param name="pageSize">Itens por página — máx. 100 (padrão: 10).</param>
    /// <param name="animalId">Filtra pelo UUID do animal.</param>
    /// <param name="status">Filtro: <c>Pendente | Enviado | Cancelado</c></param>
    /// <param name="tipo">Filtro: <c>Vacina | Medicamento | Consulta | Higiene | Outro</c></param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? animalId = null,
        [FromQuery] StatusLembreteEnum? status = null,
        [FromQuery] TipoLembreteEnum? tipo = null)
    {
        if (page < 1)
            return BadRequest(new { error = "O parâmetro 'page' deve ser maior que zero." });
        if (pageSize < 1 || pageSize > 100)
            return BadRequest(new { error = "O parâmetro 'pageSize' deve estar entre 1 e 100." });

        // FiltroDeListagem() devolve null quando o recorte esta desligado (sem
        // filtro, comportamento de sempre) e LANCA quando esta ligado sem tutor no
        // token. Nao existe um terceiro caminho em que ele devolve null com o
        // recorte ligado -- que seria justamente o bug de "filtro opcional"
        // devolvendo a base inteira para ADMIN, VETERINARIO e para quem so mandou
        // a X-Api-Key.
        var result = await _service.GetAllAsync(page, pageSize, animalId, status, tipo, _escopo.FiltroDeListagem());
        return Ok(result);
    }

    /// <summary>Retorna um lembrete pelo ID (UUID).</summary>
    /// <param name="id">UUID do lembrete.</param>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _service.GetByIdAsync(id);

        if (_escopo.Ativo && !await _escopo.AnimalEDoTutorAsync(result.AnimalId))
            throw new NotFoundException($"Lembrete {id} nao encontrado.");

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
        if (_escopo.Ativo && !await _escopo.AnimalEDoTutorAsync(request.AnimalId))
            throw new NotFoundException($"Animal {request.AnimalId} nao encontrado.");

        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Atualiza um lembrete existente.
    /// <c>agendadoEm</c> deve ser uma data/hora futura.
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

        if (_escopo.Ativo && !await _escopo.AnimalEDoTutorAsync(request.AnimalId))
            throw new NotFoundException($"Animal {request.AnimalId} nao encontrado.");

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
