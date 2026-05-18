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

    public async Task<IEnumerable<Animal>> GetAllAsync(int page, int pageSize, string? tutorId)
    {
        var query = _context.Animais
            .Include(a => a.Tutor)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(tutorId))
            query = query.Where(a => a.TutorId == tutorId);

        return await query
            .OrderBy(a => a.Nome)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Animal?> GetByIdAsync(string id)
    {
        return await _context.Animais
            .Include(a => a.Tutor)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Animal> CreateAsync(Animal animal)
    {
        animal.Id = Guid.NewGuid().ToString();
        animal.CriadoEm = DateTime.UtcNow;
        _context.Animais.Add(animal);
        await _context.SaveChangesAsync();
        return animal;
    }

    public async Task<Animal?> UpdateAsync(string id, Animal animal)
    {
        var existing = await _context.Animais.FindAsync(id);
        if (existing is null) return null;

        existing.Nome = animal.Nome;
        existing.Especie = animal.Especie;
        existing.Raca = animal.Raca;
        existing.DataNascimento = animal.DataNascimento;
        existing.Sexo = animal.Sexo;
        existing.Castrado = animal.Castrado;
        existing.Ativo = animal.Ativo;
        existing.TutorId = animal.TutorId;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var animal = await _context.Animais.FindAsync(id);
        if (animal is null) return false;

        _context.Animais.Remove(animal);
        await _context.SaveChangesAsync();
        return true;
    }
}
