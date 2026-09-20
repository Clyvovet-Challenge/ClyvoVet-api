using ClyvoVet.Domain.Enums;
using ClyvoVet.Domain.Entities;

namespace ClyvoVet.Application.Abstractions.Repositories;

public interface IPredisposicaoSaudeRepository
{
    Task<IEnumerable<PredisposicaoSaude>> GetByEspecieAsync(EspecieEnum especie);
}
