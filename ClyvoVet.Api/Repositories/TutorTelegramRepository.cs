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
            _context.TutoresTelegram.Add(new TutorTelegram { TutorId = tutorId, ChatId = chatId });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<long?> GetChatIdByTutorIdAsync(string tutorId)
    {
        var vinculo = await _context.TutoresTelegram.FirstOrDefaultAsync(t => t.TutorId == tutorId);
        return vinculo?.ChatId;
    }
}
