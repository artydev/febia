namespace Siccrf.Guichet.Api.Domaine;

/// <summary>Notification émise par le guichet (log simulé, pas de SMTP réel).</summary>
public class Notification
{
    public Guid Id { get; set; }

    public Guid? DemandeId { get; set; }

    public Demande? Demande { get; set; }

    /// <summary>Destinataire (adresse fictive ou identité).</summary>
    public required string Destinataire { get; set; }

    public TypeNotification Type { get; set; }

    public DateTime Date { get; set; }

    public required string Contenu { get; set; }
}
