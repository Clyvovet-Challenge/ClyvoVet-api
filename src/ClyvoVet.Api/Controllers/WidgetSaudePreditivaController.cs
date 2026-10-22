using ClyvoVet.Api.Hateoas;
using ClyvoVet.Api.Filters;
using ClyvoVet.Application.Security;
using ClyvoVet.Application.Services.Interfaces;
using ClyvoVet.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVet.Api.Controllers;

/// <summary>
/// Widget de Saúde Preditiva — monta um card com as predisposições de saúde
/// relevantes para a espécie, raça e idade atual de um animal, sugerindo
/// agendar consulta quando alguma condição relevante for encontrada.
/// </summary>
/// <remarks>
/// <para><b>Existem dois endpoints de saude preditiva, e este NAO e o que o app usa</b></para>
///
/// <para>
/// Este le <c>t_clyvo_predisposicao_saude</c> e devolve a lista crua de
/// predisposicoes da especie/raca/idade. Foi a primeira geracao da ideia, e
/// continua entregue, testada e documentada no README.
/// </para>
///
/// <para>
/// O que a home do aplicativo consome e o
/// <see cref="SaudePreditivaController"/> (<c>/api/v1/saude-preditiva/{animalId}</c>),
/// que le <c>t_clyvo_base_doencas</c>, passa pela OCI Generative AI para
/// redigir e prioriza, e guarda o resultado em cache por sete dias.
/// </para>
///
/// <para>
/// Mexeu na saude preditiva que o tutor ve? E o outro arquivo.
/// </para>
/// </remarks>
[ApiController]
[Route("api/v1/widget-saude-preditiva")]
[Produces("application/json")]
[Authorize(Policy = PoliticasDeAcesso.Autenticado)]
[TypeFilter(typeof(ApiKeyFilterAttribute), Arguments = new object[] { "Api:ApiKey" })]
public class WidgetSaudePreditivaController : ControllerBase
{
    private readonly IWidgetSaudePreditivaService _service;
    private readonly EscopoDoTutor _escopo;
    private readonly GeradorDeLinks _links;

    public WidgetSaudePreditivaController(IWidgetSaudePreditivaService service, EscopoDoTutor escopo, GeradorDeLinks links)
    {
        _service = service;
        _escopo = escopo;
        _links = links;
    }

    /// <summary>Retorna o card de saúde preditiva de um animal pelo ID.</summary>
    /// <param name="animalId">UUID do animal.</param>
    [HttpGet("{animalId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAnimalId(string animalId)
    {
        // O animalId vem na ROTA, entao aqui ele e o proprio alvo do recorte -- nao
        // ha listagem a filtrar, ha um recurso a autorizar. O que este endpoint
        // devolve e um retrato de saude por raca e idade: dado de saude do animal
        // de alguem, e nao catalogo publico.
        await _escopo.ExigirAnimalDoTutorAsync(animalId, $"Animal {animalId} nao encontrado.");

        var result = await _service.GetPredisposicoesAsync(animalId);
        _links.Widget(result, animalId);
        return Ok(result);
    }
}
