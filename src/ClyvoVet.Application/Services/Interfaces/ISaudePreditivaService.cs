using ClyvoVet.Application.DTOs.Response;

namespace ClyvoVet.Application.Services.Interfaces;

public interface ISaudePreditivaService
{
    /// <summary>
    /// Devolve o parecer de saúde preditiva do animal — do cache quando ainda
    /// válido; gerado (IA com fallback determinístico) quando não.
    /// </summary>
    Task<SaudePreditivaResponse> GetParecerAsync(string animalId, CancellationToken cancellationToken = default);
}
