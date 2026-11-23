using Microsoft.EntityFrameworkCore;
using Siccrf.Guichet.Api.Contrats;
using Siccrf.Guichet.Api.Domaine;
using Siccrf.Guichet.Api.Infrastructure;

namespace Siccrf.Guichet.Api.Services;

/// <summary>
/// Circuit de validation : soumission (détermination du valideur, notification, traçabilité),
/// file du chef de service, décisions (valider / à compléter / refuser).
/// </summary>
public class ValidationService(GuichetDbContext db, INotificationService notifications)
{
    /// <summary>
    /// Soumet une demande : confirmation explicite, détermination du valideur via le rattachement,
    /// notification au chef de service, changement de statut (Brouillon → Soumise).
    /// </summary>
    public async Task<DemandeDto> SoumettreAsync(Guid demandeId, Guid agentId, bool confirme, CancellationToken ct = default)
    {
        if (!confirme)
        {
            throw new InvalidOperationException("La confirmation explicite du demandeur est requise pour soumettre.");
        }

        var demande = await ChargerDemandeAsync(demandeId, ct);
        VerifierProprietaire(demande, agentId);

        if (demande.Statut != StatutDemande.Brouillon && demande.Statut != StatutDemande.ACompleter)
        {
            throw new InvalidOperationException($"Seul un brouillon (ou une demande à compléter) peut être soumis (statut actuel : {demande.Statut}).");
        }

        if (demande.VersionCourante is null)
        {
            throw new InvalidOperationException("La demande n'a pas de fiche à soumettre.");
        }
        // Détermination du valideur via la table de correspondance agent → service → responsable
        var rattachement = await db.Rattachements
            .Include(r => r.Responsable)
            .SingleOrDefaultAsync(r => r.AgentId == agentId, ct)
            ?? throw new InvalidOperationException("Aucun rattachement (service → responsable) trouvé pour cet agent.");

        var statutAvant = demande.Statut;
        demande.Statut = StatutDemande.Soumise;
        demande.DateSoumission = DateTime.UtcNow;
        demande.ValideurId = rattachement.ResponsableId;

        db.ChangementsStatut.Add(new ChangementStatut
        {
            Id = Guid.NewGuid(),
            DemandeId = demande.Id,
            StatutAvant = statutAvant,
            StatutApres = StatutDemande.Soumise,
            Date = demande.DateSoumission.Value,
            AuteurId = agentId,
            Commentaire = "Soumission de la demande"
        });

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            DemandeId = demande.Id,
            Demande = demande,
            Destinataire = rattachement.Responsable.Courriel,
            Type = TypeNotification.Soumission,
            Date = demande.DateSoumission.Value,
            Contenu = $"La demande {demande.Numero} « {demande.VersionCourante.IntituleCourt} » a été soumise pour validation."
        };
        db.Notifications.Add(notification);

        await db.SaveChangesAsync(ct);

        // Notification simulée (log) : même objet que celui enregistré en base
        await notifications.NotifierAsync(notification, ct);

        return demande.VersDto();
    }

    /// <summary>File des demandes en attente de validation pour un chef de service.</summary>
    public async Task<IReadOnlyList<DemandeEnAttenteDto>> FileEnAttenteAsync(Guid agentId, CancellationToken ct = default)
    {
        var agent = await db.Agents.FindAsync([agentId], ct)
            ?? throw new InvalidOperationException("Agent inconnu.");

        if (agent.Profil != ProfilAgent.ChefService)
        {
            throw new UnauthorizedAccessException("Seul un chef de service peut consulter la file de validation.");
        }

        // Le chef de service valide les demandes dont le valideur est son propre compte Responsable.
        var serviceIds = await db.Responsables
            .Where(r => r.AgentId == agentId)
            .Select(r => r.Id)
            .ToListAsync(ct);

        var demandes = await db.Demandes
            .Include(d => d.Agent)
            .Include(d => d.VersionCourante)
            .Where(d => d.Statut == StatutDemande.Soumise
                        && d.AgentId != agentId // jamais ses propres demandes
                        && d.ValideurId != null && serviceIds.Contains(d.ValideurId.Value))
            .OrderBy(d => d.DateSoumission)
            .ToListAsync(ct);

        return demandes.Select(d => d.VersEnAttenteDto()).ToList();
    }

    /// <summary>
    /// Décision d'un valideur : valider / demander des compléments / refuser.
    /// Motivation obligatoire pour « à compléter » et « refuser ».
    /// </summary>
    public async Task<ValidationDto> DeciderAsync(Guid demandeId, Guid agentId, DecisionRequest requete, CancellationToken ct = default)
    {
        var demande = await ChargerDemandeAsync(demandeId, ct);
        await VerifierValideurAsync(demande, agentId, ct);

        if (demande.Statut != StatutDemande.Soumise)
        {
            throw new InvalidOperationException($"Seule une demande soumise peut être décidée (statut actuel : {demande.Statut}).");
        }

        if (!Enum.TryParse<TypeDecision>(requete.Decision, ignoreCase: true, out var decision))
        {
            throw new InvalidOperationException($"Décision inconnue : {requete.Decision}. Valeurs attendues : Valider, DemanderComplements, Refuser.");
        }

        if (decision != TypeDecision.Valider && string.IsNullOrWhiteSpace(requete.Commentaire))
        {
            throw new InvalidOperationException("La motivation est obligatoire pour « demander des compléments » ou « refuser ».");
        }

        var statutAvant = demande.Statut;
        var statutApres = decision switch
        {
            TypeDecision.Valider => StatutDemande.Validee,
            TypeDecision.DemanderComplements => StatutDemande.ACompleter,
            TypeDecision.Refuser => StatutDemande.Refusee,
            _ => throw new InvalidOperationException("Décision non gérée.")
        };

        demande.Statut = statutApres;

        var validation = new Validation
        {
            Id = Guid.NewGuid(),
            DemandeId = demande.Id,
            Decision = decision,
            AuteurId = agentId,
            Date = DateTime.UtcNow,
            Commentaire = requete.Commentaire,
            StatutAvant = statutAvant,
            StatutApres = statutApres
        };

        db.Validations.Add(validation);

        db.ChangementsStatut.Add(new ChangementStatut
        {
            Id = Guid.NewGuid(),
            DemandeId = demande.Id,
            StatutAvant = statutAvant,
            StatutApres = statutApres,
            Date = validation.Date,
            AuteurId = agentId,
            Commentaire = requete.Commentaire
        });

        var destinataire = demande.Agent?.Courriel ?? "agent.inconnu@dgccrf.finances.gouv.fr";
        var contenu = decision switch
        {
            TypeDecision.Valider => $"Votre demande {demande.Numero} a été validée.",
            TypeDecision.DemanderComplements => $"Votre demande {demande.Numero} nécessite des compléments : {requete.Commentaire}",
            TypeDecision.Refuser => $"Votre demande {demande.Numero} a été refusée : {requete.Commentaire}",
            _ => $"Votre demande {demande.Numero} a reçu une décision."
        };

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            DemandeId = demande.Id,
            Demande = demande,
            Destinataire = destinataire,
            Type = decision == TypeDecision.DemanderComplements ? TypeNotification.DemandeComplements : TypeNotification.Decision,
            Date = validation.Date,
            Contenu = contenu
        };
        db.Notifications.Add(notification);

        await db.SaveChangesAsync(ct);

        await notifications.NotifierAsync(notification, ct);

        return validation.VersDto(demande.Numero);
    }

    private async Task<Demande> ChargerDemandeAsync(Guid demandeId, CancellationToken ct)
        => await db.Demandes
            .Include(d => d.Agent)
            .Include(d => d.Valideur)
            .Include(d => d.VersionCourante)
            .SingleOrDefaultAsync(d => d.Id == demandeId, ct)
            ?? throw new InvalidOperationException("Demande introuvable.");

    private static void VerifierProprietaire(Demande demande, Guid agentId)
    {
        if (demande.AgentId != agentId)
        {
            throw new UnauthorizedAccessException("Vous n'êtes pas autorisé à accéder à cette demande.");
        }
    }

    private async Task VerifierValideurAsync(Demande demande, Guid agentId, CancellationToken ct)
    {
        var agent = await db.Agents.FindAsync([agentId], ct)
            ?? throw new UnauthorizedAccessException("Agent inconnu.");

        if (agent.Profil != ProfilAgent.ChefService)
        {
            throw new UnauthorizedAccessException("Seul un chef de service peut prendre une décision de validation.");
        }

        // Séparation des rôles : on ne valide jamais sa propre demande.
        if (demande.AgentId == agentId)
        {
            throw new UnauthorizedAccessException("Vous ne pouvez pas valider votre propre demande.");
        }

        // Le valideur désigné à la soumission doit être le compte qui décide.
        var estValideur = await db.Responsables
            .AnyAsync(r => r.Id == demande.ValideurId && r.AgentId == agentId, ct);

        if (!estValideur)
        {
            throw new UnauthorizedAccessException("Cette demande n'est pas dans votre périmètre de validation.");
        }
    }
}
