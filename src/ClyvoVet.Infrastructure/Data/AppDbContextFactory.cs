using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClyvoVet.Infrastructure.Data;

// Usada só pelo `dotnet ef` (migrations add / script), nunca em execução.
//
// Sem ela, a ferramenta montaria o host inteiro da Api para achar o AppDbContext --
// JWT, Telegram, Mongo, background services -- só para descobrir o provider. Gerar
// migration e script não abre conexão, então a connection string abaixo é fictícia
// de propósito: nenhuma credencial entra no repositório.
//
// A versão do servidor é a mesma fixada em InfrastructureServiceExtensions, pelo
// mesmo motivo (sem AutoDetect): o SQL gerado tem de ser o do MySQL que roda de verdade.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql("Server=localhost;Database=clyvovet", new MySqlServerVersion(new Version(8, 0)))
            .Options;

        return new AppDbContext(options);
    }
}
