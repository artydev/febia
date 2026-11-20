namespace Siccrf.Guichet.Api.Contrats;

/// <summary>Demande de connexion (démonstrateur).</summary>
public record LoginRequest(
    string Identifiant,
    string MotDePasse);

/// <summary>Réponse de connexion.</summary>
public record LoginResponse(
    string Jeton,
    DateTime Expiration,
    Guid AgentId,
    string NomComplet,
    string Profil);
