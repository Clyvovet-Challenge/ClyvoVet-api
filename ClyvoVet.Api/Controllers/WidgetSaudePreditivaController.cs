using ClyvoVet.Api.Exceptions;
using ClyvoVet.Api.Filters;
using ClyvoVet.Api.Security;
using ClyvoVet.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVet.Api.Controllers;

/// <summary>
/// Widget de Saúde Preditiva — monta um card com as predisposições de saúde
/// relevantes para a espécie, raça e idade atual de um animal, sugerindo
/// agendar consulta quando alguma condição relevante for encontrada.
/// </summary>
[ApiController]
[Route("api/v1/widget-saude-preditiva")]
[Produces("application/json")]
[TypeFilter(typeof(ApiKeyFilterAttribute), Arguments = new object[] { "Api:ApiKey" })]
public class WidgetSaudePreditivaController : ControllerBase
{
    private readonly IWidgetSaudePreditivaService _service;
    private readonly EscopoDoTutor _escopo;

    public WidgetSaudePreditivaController(IWidgetSaudePreditivaService service, EscopoDoTutor escopo)
    {
        _service = service;
        _escopo = escopo;
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
        if (_escopo.Ativo && !await _escopo.AnimalEDoTutorAsync(animalId))
            throw new NotFoundException($"Animal {animalId} nao encontrado.");

        var result = await _service.GetPredisposicoesAsync(animalId);
        return Ok(result);
    }
}
