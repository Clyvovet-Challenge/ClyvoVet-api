using ClyvoVet.Api.Data;
using ClyvoVet.Api.Models;
using ClyvoVet.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Api.Repositories;

public class TutorTelegramRepository : ITutorTelegramRepository
{
    private readonly AppDbContext _context;

    public TutorTelegramRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task VincularAsync(string tutorId, long chatId)
    {
        var existente = await _context.TutoresTelegram.FirstOrDefaultAsync(t => t.TutorId == tutorId);

        if (existente is not null)
        {
            existente.ChatId = chatId;
        }
        else
        {
            _context.TutoresTelegram.Add(new TutorTelegram
            {
                Id = Guid.NewGuid().ToString(),
                TutorId = tutorId,
                ChatId = chatId,
                CriadoEm = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<long?> GetChatIdByTutorIdAsync(string tutorId)
    {
        var vinculo = await _context.TutoresTelegram.FirstOrDefaultAsync(t => t.TutorId == tutorId);
        return vinculo?.ChatId;
    }

    public async Task<string?> GetTutorIdByChatIdAsync(long chatId)
    {
        var vinculo = await _context.TutoresTelegram.FirstOrDefaultAsync(t => t.ChatId == chatId);
        return vinculo?.TutorId;
    }

    public async Task<bool> DesvincularAsync(string tutorId)
    {
        var vinculo = await _context.TutoresTelegram.FirstOrDefaultAsync(t => t.TutorId == tutorId);
        if (vinculo is null)
            return false;

        _context.TutoresTelegram.Remove(vinculo);
        await _context.SaveChangesAsync();
        return true;
    }
}
