using Microsoft.EntityFrameworkCore;
using Siccrf.Guichet.Api.Domaine;
using Siccrf.Guichet.Api.Services;

namespace Siccrf.Guichet.Api.Infrastructure;

/// <summary>
/// Seed de données fictives conforme au scénario de démonstration :
/// contrôle des plates-formes e-commerce et prélèvements à distance.
/// Les sauvegardes sont faites en phases pour éviter la dépendance circulaire
/// Demande.VersionCouranteId ↔ VersionFiche.DemandeId.
/// </summary>
public static class SeedDonnees
{
    public static void Initialiser(GuichetDbContext db)
    {
        db.Database.EnsureCreated();

        if (db.Agents.Any())
        {
            return;
        }

        // --- Phase 1 : agents, responsables, rattachements ---
        var agentEcommerce = new Agent
        {
            Id = Guid.NewGuid(),
            Identifiant = "agent.ecommerce",
            Nom = "Martin",
            Prenom = "Claire",
            Bureau = "Bureau E1",
            Courriel = "claire.martin@dgccrf.finances.gouv.fr",
            MotDePasseHash = MotDePasseHash.Creer("demo"),
            Profil = ProfilAgent.Agent
        };

        var agentPilotage = new Agent
        {
            Id = Guid.NewGuid(),
            Identifiant = "agent.pilotage",
            Nom = "Bernard",
            Prenom = "Julien",
            Bureau = "Bureau E2",
            Courriel = "julien.bernard@dgccrf.finances.gouv.fr",
            MotDePasseHash = MotDePasseHash.Creer("demo"),
            Profil = ProfilAgent.Agent
        };

        var chefService = new Agent
        {
            Id = Guid.NewGuid(),
            Identifiant = "chef.service",
            Nom = "Dubois",
            Prenom = "Sophie",
            Bureau = "Bureau E1",
            Courriel = "sophie.dubois@dgccrf.finances.gouv.fr",
            MotDePasseHash = MotDePasseHash.Creer("demo"),
            Profil = ProfilAgent.ChefService
        };

        var bureauXa = new Agent
        {
            Id = Guid.NewGuid(),
            Identifiant = "bureau.xa",
            Nom = "Lefevre",
            Prenom = "Pierre",
            Bureau = "Bureau XA",
            Courriel = "pierre.lefevre@dgccrf.finances.gouv.fr",
            MotDePasseHash = MotDePasseHash.Creer("demo"),
            Profil = ProfilAgent.BureauXA
        };

        var siccrf = new Agent
        {
            Id = Guid.NewGuid(),
            Identifiant = "siccrf",
            Nom = "Moreau",
            Prenom = "Anne",
            Bureau = "SICCRF",
            Courriel = "anne.moreau@dgccrf.finances.gouv.fr",
            MotDePasseHash = MotDePasseHash.Creer("demo"),
            Profil = ProfilAgent.Siccrf
        };

        // Second chef de service (Bureau E2) et responsable hiérarchique des chefs de service :
        // chaque Responsable est lié à un compte Agent, de sorte que la personne notifiée
        // est bien celle qui se connecte et décide.
        var chefService2 = new Agent
        {
            Id = Guid.NewGuid(),
            Identifiant = "chef.service2",
            Nom = "Roux",
            Prenom = "Nadia",
            Bureau = "Bureau E2",
            Courriel = "nadia.roux@dgccrf.finances.gouv.fr",
            MotDePasseHash = MotDePasseHash.Creer("demo"),
            Profil = ProfilAgent.ChefService
        };

        var sousDirecteur = new Agent
        {
            Id = Guid.NewGuid(),
            Identifiant = "sous.directeur",
            Nom = "Petit",
            Prenom = "Marc",
            Bureau = "Sous-direction E",
            Courriel = "marc.petit@dgccrf.finances.gouv.fr",
            MotDePasseHash = MotDePasseHash.Creer("demo"),
            Profil = ProfilAgent.ChefService
        };

        db.Agents.AddRange(agentEcommerce, agentPilotage, chefService, chefService2, sousDirecteur, bureauXa, siccrf);

        // Un Responsable = un compte Agent habilité à valider pour un service.
        var responsableE1 = new Responsable
        {
            Id = Guid.NewGuid(),
            AgentId = chefService.Id,
            Nom = chefService.Nom,
            Prenom = chefService.Prenom,
            Service = "Bureau E1",
            Courriel = chefService.Courriel
        };

        var responsableE2 = new Responsable
        {
            Id = Guid.NewGuid(),
            AgentId = chefService2.Id,
            Nom = chefService2.Nom,
            Prenom = chefService2.Prenom,
            Service = "Bureau E2",
            Courriel = chefService2.Courriel
        };

        var responsableSousDirection = new Responsable
        {
            Id = Guid.NewGuid(),
            AgentId = sousDirecteur.Id,
            Nom = sousDirecteur.Nom,
            Prenom = sousDirecteur.Prenom,
            Service = "Sous-direction E",
            Courriel = sousDirecteur.Courriel
        };

        db.Responsables.AddRange(responsableE1, responsableE2, responsableSousDirection);

        // Chaque agent est rattaché au responsable qui valide ses demandes ;
        // les chefs de service sont rattachés à la sous-direction (jamais à eux-mêmes).
        db.Rattachements.AddRange(
            new Rattachement { Id = Guid.NewGuid(), AgentId = agentEcommerce.Id, Service = "Bureau E1", ResponsableId = responsableE1.Id },
            new Rattachement { Id = Guid.NewGuid(), AgentId = agentPilotage.Id, Service = "Bureau E2", ResponsableId = responsableE2.Id },
            new Rattachement { Id = Guid.NewGuid(), AgentId = chefService.Id, Service = "Bureau E1", ResponsableId = responsableSousDirection.Id },
            new Rattachement { Id = Guid.NewGuid(), AgentId = chefService2.Id, Service = "Bureau E2", ResponsableId = responsableSousDirection.Id }
        );

        db.SaveChanges();

        // --- Phase 2 : demandes (sans VersionCouranteId) ---
        var demandeEcommerce = new Demande
        {
            Id = Guid.NewGuid(),
            Numero = "EB-2027-0001",
            AgentId = agentEcommerce.Id,
            Bureau = "Bureau E1",
            Statut = StatutDemande.Brouillon,
            Categorie = CategorieDemande.NouvelleFonctionnalite,
            DateCreation = DateTime.UtcNow.AddDays(-3)
        };

        var demandePilotage = new Demande
        {
            Id = Guid.NewGuid(),
            Numero = "EB-2027-0002",
            AgentId = agentPilotage.Id,
            Bureau = "Bureau E2",
            Statut = StatutDemande.Soumise,
            Categorie = CategorieDemande.AmeliorationApplication,
            DateCreation = DateTime.UtcNow.AddDays(-2),
            DateSoumission = DateTime.UtcNow.AddDays(-1),
            ValideurId = responsableE2.Id
        };

        db.Demandes.AddRange(demandeEcommerce, demandePilotage);
        db.SaveChanges();

        // --- Phase 3 : versions de fiche ---
        var ficheEcommerce = new VersionFiche
        {
            Id = Guid.NewGuid(),
            DemandeId = demandeEcommerce.Id,
            NumeroVersion = 1,
            Source = SourceVersion.Ia,
            DateCreation = DateTime.UtcNow.AddDays(-3),
            AccepteeParAgent = true,
            ApplicationOuService = "Nouveau service",
            IntituleCourt = "Contrôle des plates-formes e-commerce et prélèvements à distance",
            Synthese = "La DGCCRF doit désormais contrôler les plates-formes e-commerce et réaliser des prélèvements à distance. L'objectif est de faire évoluer les pratiques, processus et l'outillage.",
            SituationActuelle = "Les contrôles des plates-formes e-commerce sont réalisés de manière manuelle, sans outillage dédié.",
            DifficultesRencontrees = "Tâches manuelles nombreuses, absence d'outillage, suivi difficile des prélèvements à distance.",
            ExempleRepresentatif = "Un contrôle de place de marché nécessite aujourd'hui plusieurs jours de collecte manuelle.",
            ResultatRecherche = "Disposer d'un outillage permettant de contrôler les plates-formes et de réaliser des prélèvements à distance.",
            CriteresDeReussite = "Réduction du temps de contrôle, traçabilité des prélèvements, restitution exploitable.",
            SolutionEnvisagee = "Évolution des pratiques, processus et outillage (à préciser).",
            UtilisateursConcernes = "Agents des bureaux de contrôle e-commerce.",
            ProcessusMetierConcerne = "Contrôle des plates-formes e-commerce.",
            BeneficesAttendus = "Gain de temps, meilleure traçabilité, capacité de contrôle accrue.",
            EcheanceSouhaitee = "2027",
            Contraintes = "Données sensibles, conformité RGPD.",
            PiecesComplementaires = null,
            PointsAPreciser = "Volumétrie exacte des contrôles, périmètre des plates-formes concernées."
        };

        var fichePilotage = new VersionFiche
        {
            Id = Guid.NewGuid(),
            DemandeId = demandePilotage.Id,
            NumeroVersion = 1,
            Source = SourceVersion.Ia,
            DateCreation = DateTime.UtcNow.AddDays(-2),
            AccepteeParAgent = true,
            ApplicationOuService = "Application de pilotage",
            IntituleCourt = "Pilotage des contrôles e-commerce",
            Synthese = "Améliorer le pilotage des contrôles des plates-formes e-commerce.",
            SituationActuelle = "Le pilotage est réalisé via des tableurs manuels.",
            DifficultesRencontrees = "Données dispersées, absence de vision consolidée.",
            ResultatRecherche = "Une vision consolidée du pilotage des contrôles.",
            CriteresDeReussite = "Tableau de bord à jour, indicateurs partagés.",
            UtilisateursConcernes = "Équipe de pilotage.",
            ProcessusMetierConcerne = "Pilotage des contrôles.",
            BeneficesAttendus = "Meilleure visibilité, décisions éclairées.",
            EcheanceSouhaitee = "2027",
            PointsAPreciser = "Indicateurs exacts attendus."
        };

        db.VersionsFiche.AddRange(ficheEcommerce, fichePilotage);
        db.SaveChanges();

        // --- Phase 4 : liaison VersionCouranteId ---
        demandeEcommerce.VersionCouranteId = ficheEcommerce.Id;
        demandePilotage.VersionCouranteId = fichePilotage.Id;
        db.SaveChanges();

        // --- Phase 5 : changements de statut et notifications ---
        db.ChangementsStatut.AddRange(
            new ChangementStatut
            {
                Id = Guid.NewGuid(),
                DemandeId = demandeEcommerce.Id,
                StatutAvant = StatutDemande.Brouillon,
                StatutApres = StatutDemande.Brouillon,
                Date = demandeEcommerce.DateCreation,
                AuteurId = agentEcommerce.Id,
                Commentaire = "Création de la demande"
            },
            new ChangementStatut
            {
                Id = Guid.NewGuid(),
                DemandeId = demandePilotage.Id,
                StatutAvant = StatutDemande.Brouillon,
                StatutApres = StatutDemande.Soumise,
                Date = demandePilotage.DateSoumission!.Value,
                AuteurId = agentPilotage.Id,
                Commentaire = "Soumission de la demande"
            }
        );

        db.Notifications.AddRange(
            new Notification
            {
                Id = Guid.NewGuid(),
                DemandeId = demandePilotage.Id,
                Destinataire = responsableE2.Courriel,
                Type = TypeNotification.Soumission,
                Date = demandePilotage.DateSoumission!.Value,
                Contenu = $"La demande {demandePilotage.Numero} a été soumise pour validation."
            }
        );

        db.SaveChanges();
    }
}
