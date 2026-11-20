namespace Siccrf.Guichet.Api.Domaine;

/// <summary>
/// Avis distinct d'un acteur de la gouvernance (Bureau XA, SICCRF, RSSI, Mission Data/IA).
/// Les avis ne sont jamais fusionnés de façon opaque.
/// </summary>
public class Avis
{
    public Guid Id { get; set; }

    public Guid DemandeId { get; set; }

    public Demande? Demande { get; set; }

    public EmetteurAvis Emetteur { get; set; }

    public Guid AuteurId { get; set; }

    public Agent? Auteur { get; set; }

    public DateTime Date { get; set; }

    public required string Contenu { get; set; }
}
