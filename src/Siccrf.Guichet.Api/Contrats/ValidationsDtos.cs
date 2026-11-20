using Siccrf.Guichet.Api.Domaine;

namespace Siccrf.Guichet.Api.Contrats;

/// <summary>Décision de validation enregistrée (traçabilité).</summary>
public record ValidationDto(
    Guid Id,
    Guid DemandeId,
    string Numero,
    string Decision,
    string? Commentaire,
    DateTime Date,
    string StatutAvant,
    string StatutApres);

/// <summary>Demande en attente de validation (file du chef de service).</summary>
public record DemandeEnAttenteDto(
    Guid Id,
    string Numero,
    string? IntituleCourt,
    string Demandeur,
    string Bureau,
    DateTime? DateSoumission,
    string? Synthese);

/// <summary>Mappage des entités de validation vers les DTOs.</summary>
public static class MappeursValidation
{
    public static ValidationDto VersDto(this Validation validation, string numero) => new(
        validation.Id,
        validation.DemandeId,
        numero,
        validation.Decision.ToString(),
        validation.Commentaire,
        validation.Date,
        validation.StatutAvant.ToString(),
        validation.StatutApres.ToString());

    public static DemandeEnAttenteDto VersEnAttenteDto(this Demande demande) => new(
        demande.Id,
        demande.Numero,
        demande.VersionCourante?.IntituleCourt,
        demande.Agent?.NomComplet ?? "—",
        demande.Bureau,
        demande.DateSoumission,
        demande.VersionCourante?.Synthese);
}
