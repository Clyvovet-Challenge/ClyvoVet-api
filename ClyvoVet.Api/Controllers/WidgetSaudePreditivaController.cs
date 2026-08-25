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
public class WidgetSaudePreditivaController : ControllerBase
{
    private readonly IWidgetSaudePreditivaService _service;

    public WidgetSaudePreditivaController(IWidgetSaudePreditivaService service) => _service = service;

    /// <summary>Retorna o card de saúde preditiva de um animal pelo ID.</summary>
    /// <param name="animalId">UUID do animal.</param>
    [HttpGet("{animalId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAnimalId(string animalId)
    {
        var result = await _service.GetPredisposicoesAsync(animalId);
        return Ok(result);
    }
}
