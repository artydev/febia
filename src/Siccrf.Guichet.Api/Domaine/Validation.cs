namespace Siccrf.Guichet.Api.Domaine;

/// <summary>Décision d'un valideur sur une demande (traçabilité complète).</summary>
public class Validation
{
    public Guid Id { get; set; }

    public Guid DemandeId { get; set; }

    public Demande? Demande { get; set; }

    public TypeDecision Decision { get; set; }

    /// <summary>Auteur de la décision (le valideur).</summary>
    public Guid AuteurId { get; set; }

    public Agent? Auteur { get; set; }

    public DateTime Date { get; set; }

    /// <summary>Motivation obligatoire pour « demander des compléments » et « refuser ».</summary>
    public string? Commentaire { get; set; }

    public StatutDemande StatutAvant { get; set; }

    public StatutDemande StatutApres { get; set; }
}
