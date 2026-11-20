namespace Siccrf.Guichet.Api.Domaine;

/// <summary>Demande d'évolution du SI (en-tête et suivi).</summary>
public class Demande
{
    public Guid Id { get; set; }

    /// <summary>Numéro de demande, ex. EB-2027-0001.</summary>
    public required string Numero { get; set; }

    /// <summary>Agent demandeur.</summary>
    public Guid AgentId { get; set; }

    public Agent? Agent { get; set; }

    /// <summary>Bureau du demandeur (recopié à la création).</summary>
    public required string Bureau { get; set; }

    /// <summary>Valideur déterminé à la soumission (chef de service).</summary>
    public Guid? ValideurId { get; set; }

    public Responsable? Valideur { get; set; }

    public StatutDemande Statut { get; set; }

    public CategorieDemande Categorie { get; set; }

    public DateTime DateCreation { get; set; }

    public DateTime? DateSoumission { get; set; }

    /// <summary>Version de fiche courante (la plus récente).</summary>
    public Guid? VersionCouranteId { get; set; }

    public VersionFiche? VersionCourante { get; set; }

    public ICollection<VersionFiche> Versions { get; set; } = [];

    public ICollection<Validation> Validations { get; set; } = [];

    public ICollection<Avis> Avis { get; set; } = [];

    public ICollection<ChangementStatut> ChangementsStatut { get; set; } = [];

    public ICollection<Notification> Notifications { get; set; } = [];
}
