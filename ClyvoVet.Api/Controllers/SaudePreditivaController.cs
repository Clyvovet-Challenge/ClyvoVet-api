using ClyvoVet.Api.Exceptions;
using ClyvoVet.Api.Filters;
using ClyvoVet.Api.Security;
using ClyvoVet.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVet.Api.Controllers;

/// <summary>
/// Saúde preditiva com IA generativa (OCI) — o parecer de riscos e
/// recomendações que a home do app mostra por animal. Substitui o widget por
/// regras como recurso principal; o endpoint antigo continua no ar.
/// </summary>
/// <remarks>
/// <para><b>Este e o endpoint que a home do aplicativo consome.</b></para>
///
/// <para>
/// Nao confundir com <see cref="WidgetSaudePreditivaController"/>
/// (<c>/api/v1/widget-saude-preditiva/{animalId}</c>), que e a geracao anterior
/// e le outra tabela: aquele devolve a lista crua de
/// <c>t_clyvo_predisposicao_saude</c>; este parte de
/// <c>t_clyvo_base_doencas</c>, usa a OCI para redigir e priorizar, e mantem o
/// parecer em cache por sete dias.
/// </para>
/// </remarks>
[ApiController]
[Route("api/v1/saude-preditiva")]
[Produces("application/json")]
[TypeFilter(typeof(ApiKeyFilterAttribute), Arguments = new object[] { "Api:ApiKey" })]
public class SaudePreditivaController : ControllerBase
{
    private readonly ISaudePreditivaService _service;
    private readonly EscopoDoTutor _escopo;

    public SaudePreditivaController(ISaudePreditivaService service, EscopoDoTutor escopo)
    {
        _service = service;
        _escopo = escopo;
    }

    /// <summary>Retorna o parecer de saúde preditiva de um animal pelo ID.</summary>
    /// <param name="animalId">UUID do animal.</param>
    [HttpGet("{animalId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAnimalId(string animalId, CancellationToken cancellationToken)
    {
        // Mesma regra do widget: isto é dado de saúde do animal de alguém.
        // 404 e não 403 para não confirmar a existência de animal alheio.
        if (_escopo.Ativo && !await _escopo.AnimalEDoTutorAsync(animalId))
            throw new NotFoundException($"Animal {animalId} nao encontrado.");

        var result = await _service.GetParecerAsync(animalId, cancellationToken);
        return Ok(result);
    }
}
