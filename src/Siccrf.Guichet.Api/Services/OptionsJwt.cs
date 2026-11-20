namespace Siccrf.Guichet.Api.Services;

/// <summary>Configuration JWT (clé de signature, émetteur, audience, durée).</summary>
public class OptionsJwt
{
    public const string Section = "Jwt";

    public string Cle { get; set; } = string.Empty;

    public string Emetteur { get; set; } = "Siccrf.Guichet";

    public string Audience { get; set; } = "Siccrf.Guichet.Api";

    public int DureeMinutes { get; set; } = 120;
}
