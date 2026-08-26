using ClyvoVet.Api.Data;
using ClyvoVet.Api.Models;
using ClyvoVet.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Api.Repositories;

public class AnimalRepository : IAnimalRepository
{
    private readonly AppDbContext _context;

    public AnimalRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Animal?> GetByIdAsync(string id)
    {
        return await _context.Animais
            .Include(a => a.Tutor)
            .FirstOrDefaultAsync(a => a.Id == id);
    }
}
