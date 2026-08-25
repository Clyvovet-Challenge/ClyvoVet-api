using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Models;

namespace ClyvoVet.Api.Repositories.Interfaces;

public interface IPredisposicaoSaudeRepository
{
    Task<IEnumerable<PredisposicaoSaude>> GetByEspecieAsync(EspecieEnum especie);
}
