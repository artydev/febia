using Siccrf.Guichet.Api.Domaine;

namespace Siccrf.Guichet.Api.Contrats;

/// <summary>Représentation d'une fiche d'expression de besoin (version).</summary>
public record FicheDto(
    Guid Id,
    int NumeroVersion,
    string Source,
    bool AccepteeParAgent,
    DateTime DateCreation,
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
    string? PiecesComplementaires,
    string? PointsAPreciser);

/// <summary>Représentation d'une demande d'évolution.</summary>
public record DemandeDto(
    Guid Id,
    string Numero,
    string Statut,
    string Categorie,
    DateTime DateCreation,
    DateTime? DateSoumission,
    string Bureau,
    string Demandeur,
    string? Valideur,
    FicheDto? FicheCourante,
    IReadOnlyList<string> PointsAPreciser);

/// <summary>Réponse à un message de dialogue.</summary>
public record ReponseDialogueDto(
    string Message,
    FicheDto? FicheProposee,
    IReadOnlyList<string> QuestionsComplementaires,
    IReadOnlyList<string> PointsAPreciser);

/// <summary>Réponse à la création d'une demande : la demande créée + la réponse du premier tour de dialogue.</summary>
public record CreationDemandeDto(
    DemandeDto Demande,
    ReponseDialogueDto Dialogue);

/// <summary>Mappage entre le domaine et les DTOs.</summary>
public static class Mappeurs
{
    public static FicheDto VersDto(this VersionFiche fiche) => new(
        fiche.Id,
        fiche.NumeroVersion,
        fiche.Source.ToString(),
        fiche.AccepteeParAgent,
        fiche.DateCreation,
        fiche.ApplicationOuService,
        fiche.IntituleCourt,
        fiche.Synthese,
        fiche.SituationActuelle,
        fiche.DifficultesRencontrees,
        fiche.ExempleRepresentatif,
        fiche.ResultatRecherche,
        fiche.CriteresDeReussite,
        fiche.SolutionEnvisagee,
        fiche.UtilisateursConcernes,
        fiche.ProcessusMetierConcerne,
        fiche.BeneficesAttendus,
        fiche.EcheanceSouhaitee,
        fiche.Contraintes,
        fiche.PiecesComplementaires,
        fiche.PointsAPreciser);

    public static DemandeDto VersDto(this Demande demande) => new(
        demande.Id,
        demande.Numero,
        demande.Statut.ToString(),
        demande.Categorie.ToString(),
        demande.DateCreation,
        demande.DateSoumission,
        demande.Bureau,
        demande.Agent?.NomComplet ?? "—",
        demande.Valideur?.NomComplet,
        demande.VersionCourante?.VersDto(),
        PointsAPreciserDe(demande));

    private static IReadOnlyList<string> PointsAPreciserDe(Demande demande)
    {
        var points = demande.VersionCourante?.PointsAPreciser;
        if (string.IsNullOrWhiteSpace(points))
        {
            return [];
        }

        return points
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }
}
