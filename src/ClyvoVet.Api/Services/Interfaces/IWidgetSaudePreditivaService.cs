using ClyvoVet.Api.DTOs.Response;

namespace ClyvoVet.Api.Services.Interfaces;

public interface IWidgetSaudePreditivaService
{
    Task<WidgetSaudePreditivaResponse> GetPredisposicoesAsync(string animalId);
}
