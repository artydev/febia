using Siccrf.Guichet.Api.Domaine;

namespace Siccrf.Guichet.Api.Services;

/// <summary>Service d'envoi de notifications (log simulé, pas de SMTP réel).</summary>
public interface INotificationService
{
    Task NotifierAsync(Notification notification, CancellationToken ct = default);
}

/// <summary>
/// Implémentation des notifications en log simulé : écrit dans la console / le journal.
/// Aucun courriel réel n'est envoyé.
/// </summary>
public class NotificationLogService(ILogger<NotificationLogService> logger) : INotificationService
{
    public Task NotifierAsync(Notification notification, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[NOTIFICATION SIMULÉE] {Date:yyyy-MM-dd HH:mm} | {Type} | À : {Destinataire} | Demande : {Numero} | {Contenu}",
            notification.Date,
            notification.Type,
            notification.Destinataire,
            notification.Demande?.Numero ?? "—",
            notification.Contenu);

        return Task.CompletedTask;
    }
}
