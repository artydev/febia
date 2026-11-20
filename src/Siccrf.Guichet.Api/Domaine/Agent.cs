namespace Siccrf.Guichet.Api.Domaine;

/// <summary>Agent de la DGCCRF utilisant le guichet.</summary>
public class Agent
{
    public Guid Id { get; set; }

    /// <summary>Identifiant de connexion (fictif).</summary>
    public required string Identifiant { get; set; }

    public required string Nom { get; set; }

    public required string Prenom { get; set; }

    /// <summary>Bureau de rattachement de l'agent.</summary>
    public required string Bureau { get; set; }

    /// <summary>Courriel fictif (destinataire des notifications simulées).</summary>
    public required string Courriel { get; set; }

    /// <summary>Hash PBKDF2 du mot de passe (démonstrateur).</summary>
    public required string MotDePasseHash { get; set; }

    public ProfilAgent Profil { get; set; }

    public string NomComplet => $"{Prenom} {Nom}";

    public ICollection<Rattachement> Rattachements { get; set; } = [];

    public ICollection<Demande> Demandes { get; set; } = [];
}
