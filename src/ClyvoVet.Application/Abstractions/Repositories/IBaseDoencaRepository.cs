using ClyvoVet.Domain.Entities;

namespace ClyvoVet.Application.Abstractions.Repositories;

public interface IBaseDoencaRepository
{
    /// <param name="especieCodigo">Vocabulário do catálogo: CAO, GATO, ROEDOR, AVE, REPTIL.</param>
    Task<IReadOnlyList<BaseDoenca>> GetByEspecieAsync(string especieCodigo);
}
