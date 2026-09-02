using ClyvoVet.Api.Data;
using ClyvoVet.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVet.Api.Tests.Integration;

public class TutorTelegramRepositoryTests
{
    private static AppDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"TutorTelegramTestDb-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task VincularAsync_TutorSemVinculo_CriaNovoRegistro()
    {
        // Arrange
        using var context = CriarContexto();
        var repository = new TutorTelegramRepository(context);

        // Act
        await repository.VincularAsync("tutor-1", 111111);

        // Assert
        var registro = await context.TutoresTelegram.SingleAsync(t => t.TutorId == "tutor-1");
        Assert.Equal(111111, registro.ChatId);
    }

    [Fact]
    public async Task VincularAsync_TutorJaVinculado_AtualizaChatIdEmVezDeDuplicar()
    {
        // Arrange
        using var context = CriarContexto();
        var repository = new TutorTelegramRepository(context);
        await repository.VincularAsync("tutor-2", 222222);

        // Act — mesmo tutor, chatId diferente (ex.: religou o Telegram)
        await repository.VincularAsync("tutor-2", 333333);

        // Assert
        var registros = await context.TutoresTelegram.Where(t => t.TutorId == "tutor-2").ToListAsync();
        var registro = Assert.Single(registros);
        Assert.Equal(333333, registro.ChatId);
    }

    [Fact]
    public async Task GetChatIdByTutorIdAsync_TutorVinculado_RetornaOChatId()
    {
        // Arrange
        using var context = CriarContexto();
        var repository = new TutorTelegramRepository(context);
        await repository.VincularAsync("tutor-3", 444444);

        // Act
        var chatId = await repository.GetChatIdByTutorIdAsync("tutor-3");

        // Assert
        Assert.Equal(444444, chatId);
    }

    [Fact]
    public async Task GetChatIdByTutorIdAsync_TutorSemVinculo_RetornaNull()
    {
        // Arrange
        using var context = CriarContexto();
        var repository = new TutorTelegramRepository(context);

        // Act
        var chatId = await repository.GetChatIdByTutorIdAsync("tutor-sem-vinculo");

        // Assert
        Assert.Null(chatId);
    }

    [Fact]
    public async Task GetTutorIdByChatIdAsync_ChatVinculado_RetornaOTutorId()
    {
        // Arrange
        using var context = CriarContexto();
        var repository = new TutorTelegramRepository(context);
        await repository.VincularAsync("tutor-4", 555555);

        // Act
        var tutorId = await repository.GetTutorIdByChatIdAsync(555555);

        // Assert
        Assert.Equal("tutor-4", tutorId);
    }

    [Fact]
    public async Task GetTutorIdByChatIdAsync_ChatSemVinculo_RetornaNull()
    {
        // Arrange
        using var context = CriarContexto();
        var repository = new TutorTelegramRepository(context);

        // Act
        var tutorId = await repository.GetTutorIdByChatIdAsync(999999);

        // Assert
        Assert.Null(tutorId);
    }
}
