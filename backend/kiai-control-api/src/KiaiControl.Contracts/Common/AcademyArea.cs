namespace KiaiControl.Contracts.Common;

public static class AcademyArea
{
    public const string General = "shared";
    public const string Fight = "fight";
    public const string Gym = "gym";

    public static string Normalize(string? area) => area?.Trim().ToLowerInvariant() switch
    {
        null or "" or General => General,
        Fight => Fight,
        Gym => Gym,
        _ => throw new ArgumentException("Selecione uma área válida: Geral, Fight ou Gym.")
    };

    public static string Label(string area) => Normalize(area) switch
    {
        Fight => "Fight",
        Gym => "Gym",
        _ => "Geral"
    };

    public static bool IncludesModality(string area, string type) => Normalize(area) switch
    {
        Fight => type is "martial_art" or "hybrid",
        Gym => type is "fitness" or "hybrid",
        _ => true
    };
}
