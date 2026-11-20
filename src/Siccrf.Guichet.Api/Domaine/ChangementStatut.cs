namespace Siccrf.Guichet.Api.Domaine;

/// <summary>Historique des changements de statut d'une demande (traçabilité).</summary>
public class ChangementStatut
{
    public Guid Id { get; set; }

    public Guid DemandeId { get; set; }

    public Demande? Demande { get; set; }

    public StatutDemande StatutAvant { get; set; }

    public StatutDemande StatutApres { get; set; }

    public DateTime Date { get; set; }

    public Guid? AuteurId { get; set; }

    public Agent? Auteur { get; set; }

    public string? Commentaire { get; set; }
}
