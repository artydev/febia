namespace Siccrf.Guichet.Api.Domaine;

/// <summary>Cadre valideur (chef de service) identifié dans le circuit de validation.</summary>
public class Responsable
{
    public Guid Id { get; set; }

    public required string Nom { get; set; }

    public required string Prenom { get; set; }

    /// <summary>Service dont le responsable a la charge.</summary>
    public required string Service { get; set; }

    public required string Courriel { get; set; }

    public string NomComplet => $"{Prenom} {Nom}";

    public ICollection<Rattachement> Rattachements { get; set; } = [];
}
