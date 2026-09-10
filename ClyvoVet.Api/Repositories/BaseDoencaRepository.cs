using ClyvoVet.Api.Data;
using ClyvoVet.Api.Models;
using ClyvoVet.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Api.Repositories;

public class BaseDoencaRepository : IBaseDoencaRepository
{
    private readonly AppDbContext _context;

    public BaseDoencaRepository(AppDbContext context)
    {
        _context = context;
    }

    // A espécie inteira, e não só a raça: o serviço precisa das duas visões —
    // as linhas da raça do animal para o risco específico, e o agregado da
    // espécie para contexto quando a raça não está na base. São ~200 linhas no
    // pior caso (CAO); filtrar duas vezes no banco custaria mais round-trips
    // do que particionar em memória.
    public async Task<IReadOnlyList<BaseDoenca>> GetByEspecieAsync(string especieCodigo)
    {
        return await _context.BaseDoencas
            .Where(b => b.Especie == especieCodigo)
            .OrderByDescending(b => b.Casos)
            .ToListAsync();
    }
}
