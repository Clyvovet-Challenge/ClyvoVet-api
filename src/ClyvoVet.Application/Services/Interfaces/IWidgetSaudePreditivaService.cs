using ClyvoVet.Application.DTOs.Response;

namespace ClyvoVet.Application.Services.Interfaces;

public interface IWidgetSaudePreditivaService
{
    Task<WidgetSaudePreditivaResponse> GetPredisposicoesAsync(string animalId);
}
