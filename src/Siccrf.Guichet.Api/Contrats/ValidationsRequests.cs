namespace Siccrf.Guichet.Api.Contrats;

/// <summary>Soumission d'une demande : confirmation explicite du demandeur.</summary>
public record SoumettreRequest(
    bool Confirme);

/// <summary>Décision d'un valideur sur une demande.</summary>
public record DecisionRequest(
    string Decision,
    string? Commentaire);
