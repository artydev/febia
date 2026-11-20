namespace Siccrf.Guichet.Api.Domaine;

/// <summary>
/// Table de correspondance fictive agent → service → responsable valideur.
/// Permet de déterminer le valideur d'une demande à la soumission.
/// </summary>
public class Rattachement
{
    public Guid Id { get; set; }

    public Guid AgentId { get; set; }

    public Agent? Agent { get; set; }

    /// <summary>Service de l'agent (peut différer du bureau administratif).</summary>
    public required string Service { get; set; }

    public Guid ResponsableId { get; set; }

    public Responsable? Responsable { get; set; }
}
