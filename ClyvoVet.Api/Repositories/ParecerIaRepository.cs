using ClyvoVet.Api.Data;
using ClyvoVet.Api.Models;
using ClyvoVet.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Api.Repositories;

public class ParecerIaRepository : IParecerIaRepository
{
    private readonly AppDbContext _context;

    public ParecerIaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ParecerIa?> GetByAnimalIdAsync(string animalId)
    {
        return await _context.PareceresIa
            .FirstOrDefaultAsync(p => p.AnimalId == animalId);
    }

    public async Task SalvarAsync(ParecerIa parecer)
    {
        // Upsert pela chave natural (animal_id), não pelo id: o chamador não
        // precisa saber se já existia parecer — regenerou, substituiu.
        var existente = await _context.PareceresIa
            .FirstOrDefaultAsync(p => p.AnimalId == parecer.AnimalId);

        if (existente is null)
        {
            parecer.Id = string.IsNullOrEmpty(parecer.Id) ? Guid.NewGuid().ToString() : parecer.Id;
            _context.PareceresIa.Add(parecer);
        }
        else
        {
            existente.Origem = parecer.Origem;
            existente.Modelo = parecer.Modelo;
            existente.Conteudo = parecer.Conteudo;
            existente.GeradoEm = parecer.GeradoEm;
            existente.ValidoAte = parecer.ValidoAte;
        }

        await _context.SaveChangesAsync();
    }
}
