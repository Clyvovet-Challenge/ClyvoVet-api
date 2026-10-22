using ClyvoVet.Application.Common;
using ClyvoVet.Domain.Entities;

namespace ClyvoVet.Application.Services.SaudePreditiva;

/// <summary>
/// O que a saúde preditiva lê do animal. O prompt e o convite usam as mesmas
/// leituras, e ter uma cópia em cada lugar era pedir que um dia divergissem.
/// </summary>
internal static class PerfilDoAnimal
{
    internal static string Pronome(Animal animal) =>
        animal.Sexo?.Trim().ToUpperInvariant() == "FEMEA" ? "ela" : "ele";

    /// <summary>Do vocabulário livre do animal ('Cachorro') para o do catálogo/base ('CAO').</summary>
    internal static string? CodigoDoCatalogo(string? especie) =>
        especie?.Trim().ToUpperInvariant() switch
        {
            "CACHORRO" or "CAO" or "CÃO" or "CANINO" => "CAO",
            "GATO" or "FELINO" => "GATO",
            "PASSARO" or "PÁSSARO" or "AVE" => "AVE",
            "REPTIL" or "RÉPTIL" => "REPTIL",
            "ROEDOR" => "ROEDOR",
            _ => null,
        };

    /// <summary>
    /// Idade até o "hoje" de Brasília, o mesmo dia que o tutor vê no calendário: depois das
    /// 21h o dia UTC já virou, e o widget e o parecer davam idades diferentes ao mesmo animal.
    /// </summary>
    internal static decimal? CalcularIdadeAnos(DateTime? dataNascimento, TimeProvider? relogio = null)
    {
        if (dataNascimento is null)
            return null;

        var hoje = HorarioDeBrasilia.Hoje(relogio).ToDateTime(TimeOnly.MinValue);
        var dias = (hoje - dataNascimento.Value.Date).TotalDays;
        return Math.Round((decimal)(dias / 365.25), 1);
    }
}
