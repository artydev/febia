namespace Siccrf.Guichet.Api.Contrats;

/// <summary>Création d'une nouvelle demande (brouillon) avec le premier message de dialogue.</summary>
public record CreerDemandeRequest(
    string Message);

/// <summary>Message envoyé dans le cadre d'un dialogue existant.</summary>
public record EnvoyerMessageRequest(
    string Message);

/// <summary>Édition manuelle de la fiche (champs modifiables).</summary>
public record EditerFicheRequest(
    string? ApplicationOuService,
    string? IntituleCourt,
    string? Synthese,
    string? SituationActuelle,
    string? DifficultesRencontrees,
    string? ExempleRepresentatif,
    string? ResultatRecherche,
    string? CriteresDeReussite,
    string? SolutionEnvisagee,
    string? UtilisateursConcernes,
    string? ProcessusMetierConcerne,
    string? BeneficesAttendus,
    string? EcheanceSouhaitee,
    string? Contraintes,
    string? PiecesComplementaires);

/// <summary>Acceptation explicite d'une proposition de fiche faite par l'IA.</summary>
public record AccepterPropositionRequest(
    Guid VersionId);
