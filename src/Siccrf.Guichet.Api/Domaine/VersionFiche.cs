namespace Siccrf.Guichet.Api.Domaine;

/// <summary>
/// Version d'une fiche d'expression de besoin, conforme au modèle SICCRF (FEB.md).
/// Chaque modification (proposition IA acceptée ou saisie manuelle) crée une nouvelle version.
/// </summary>
public class VersionFiche
{
    public Guid Id { get; set; }

    public Guid DemandeId { get; set; }

    public Demande? Demande { get; set; }

    public int NumeroVersion { get; set; }

    public SourceVersion Source { get; set; }

    public DateTime DateCreation { get; set; }

    /// <summary>Vrai si l'agent a explicitement accepté cette version proposée par l'IA.</summary>
    public bool AccepteeParAgent { get; set; }

    // Rubriques du modèle SICCRF (FEB.md)

    /// <summary>Application ou service numérique concerné (« nouveau service » possible).</summary>
    public string? ApplicationOuService { get; set; }

    /// <summary>Intitulé court de la demande.</summary>
    public string? IntituleCourt { get; set; }

    /// <summary>Résumé en quelques lignes : problème, objectif et bénéfice principal.</summary>
    public string? Synthese { get; set; }

    /// <summary>Situation actuelle : comment le travail est réalisé aujourd'hui.</summary>
    public string? SituationActuelle { get; set; }

    /// <summary>Difficultés rencontrées : limites, tâches manuelles, conséquences.</summary>
    public string? DifficultesRencontrees { get; set; }

    /// <summary>Exemple représentatif (facultatif, sans données sensibles inutiles).</summary>
    public string? ExempleRepresentatif { get; set; }

    /// <summary>Résultat recherché : ce qui doit devenir possible ou plus simple.</summary>
    public string? ResultatRecherche { get; set; }

    /// <summary>Critères de réussite : comment constater que le besoin est satisfait.</summary>
    public string? CriteresDeReussite { get; set; }

    /// <summary>Solution envisagée par le demandeur (facultatif, non prescriptif).</summary>
    public string? SolutionEnvisagee { get; set; }

    /// <summary>Utilisateurs ou services concernés, population et volumétrie estimée.</summary>
    public string? UtilisateursConcernes { get; set; }

    /// <summary>Processus métier concerné.</summary>
    public string? ProcessusMetierConcerne { get; set; }

    /// <summary>Bénéfices attendus (qualitatifs et quantitatifs).</summary>
    public string? BeneficesAttendus { get; set; }

    /// <summary>Échéance souhaitée et justification.</summary>
    public string? EcheanceSouhaitee { get; set; }

    /// <summary>Contraintes (techniques, organisationnelles, réglementaires).</summary>
    public string? Contraintes { get; set; }

    /// <summary>Pièces complémentaires jointes.</summary>
    public string? PiecesComplementaires { get; set; }

    /// <summary>Points restant à préciser, signalés par l'IA (jamais inventés).</summary>
    public string? PointsAPreciser { get; set; }
}
