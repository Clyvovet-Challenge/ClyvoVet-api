using ClyvoVet.Domain.Enums;
using ClyvoVet.Domain.Entities;

namespace ClyvoVet.Api.Repositories.Interfaces;

public interface IPredisposicaoSaudeRepository
{
    Task<IEnumerable<PredisposicaoSaude>> GetByEspecieAsync(EspecieEnum especie);
}
