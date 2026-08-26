namespace ClyvoVet.Api.Services;

public static class DataValidationHelper
{
    public static bool EhDataNoPassado(DateOnly data) => data < DateOnly.FromDateTime(DateTime.Today);

    public static bool EhDataNoPassado(DateTime data) => data < DateTime.UtcNow;
}
