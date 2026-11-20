using Siccrf.Guichet.Api.Domaine;

namespace Siccrf.Guichet.Api.Services;

/// <summary>
/// Implémentation simulée de l'assistant IA : réponses codées en dur,
/// permet de démontrer le parcours sans clé API.
/// </summary>
public class AssistantSimule : IAssistantService
{
    public Task<ReponseAssistant> TraiterMessageAsync(
        Demande demande,
        VersionFiche ficheCourante,
        string message,
        CancellationToken ct = default)
    {
        var texte = message.Trim();

        // Détection d'un besoin hors périmètre (incident, assistance, équipement, habilitation)
        if (Contient(texte, "incident", "panne", "assistance", "aide", "équipement", "ordinateur", "habilitation", "accès"))
        {
            var reponseHorsPerimetre = new ReponseAssistant(
                "Votre demande semble relever d'un autre dispositif (incident, assistance, équipement ou habilitation). " +
                "Le guichet des demandes d'évolution du SI ne traite que les évolutions : nouvelles fonctionnalités, " +
                "améliorations d'applications, nouveaux services numériques, automatisations ou évolutions techniques. " +
                "Je vous oriente vers le dispositif approprié sans créer de demande d'évolution.",
                null,
                [],
                ["Demande hors périmètre du guichet"]);

            return Task.FromResult(reponseHorsPerimetre);
        }

        // Premier message : demande initiale vague → poser des questions ciblées
        if (string.IsNullOrWhiteSpace(ficheCourante.Synthese))
        {
            var ficheInitiale = new VersionFiche
            {
                Id = Guid.NewGuid(),
                DemandeId = demande.Id,
                NumeroVersion = ficheCourante.NumeroVersion + 1,
                Source = SourceVersion.Ia,
                DateCreation = DateTime.UtcNow,
                AccepteeParAgent = false,
                ApplicationOuService = "Nouveau service",
                IntituleCourt = "Demande d'évolution (à préciser)",
                Synthese = texte,
                PointsAPreciser = "Application ou service concerné ; problème actuel ; objectif ; bénéficiaires ; bénéfices attendus ; échéance."
            };

            var reponseInitiale = new ReponseAssistant(
                "J'ai bien noté votre demande. Pour constituer une fiche exploitable, j'ai besoin de précisions :",
                ficheInitiale,
                [
                    "Quel est le problème ou la difficulté rencontrée aujourd'hui ?",
                    "Quel résultat attendez-vous concrètement ?",
                    "Qui sont les bénéficiaires (services, agents) ?",
                    "Quels bénéfices attendez-vous (temps, qualité, conformité) ?",
                    "Y a-t-il une échéance ou des contraintes particulières ?"
                ],
                ["Application ou service concerné", "Problème actuel", "Objectif", "Bénéficiaires", "Bénéfices attendus", "Échéance"]);

            return Task.FromResult(reponseInitiale);
        }

        // Message de complément : enrichir la fiche sans écraser les modifications manuelles
        var ficheEnrichie = new VersionFiche
        {
            Id = Guid.NewGuid(),
            DemandeId = demande.Id,
            NumeroVersion = ficheCourante.NumeroVersion + 1,
            Source = SourceVersion.Ia,
            DateCreation = DateTime.UtcNow,
            AccepteeParAgent = false,
            ApplicationOuService = ficheCourante.ApplicationOuService,
            IntituleCourt = ficheCourante.IntituleCourt,
            Synthese = ficheCourante.Synthese,
            SituationActuelle = ficheCourante.SituationActuelle ?? texte,
            DifficultesRencontrees = ficheCourante.DifficultesRencontrees,
            ExempleRepresentatif = ficheCourante.ExempleRepresentatif,
            ResultatRecherche = ficheCourante.ResultatRecherche,
            CriteresDeReussite = ficheCourante.CriteresDeReussite,
            SolutionEnvisagee = ficheCourante.SolutionEnvisagee,
            UtilisateursConcernes = ficheCourante.UtilisateursConcernes,
            ProcessusMetierConcerne = ficheCourante.ProcessusMetierConcerne,
            BeneficesAttendus = ficheCourante.BeneficesAttendus,
            EcheanceSouhaitee = ficheCourante.EcheanceSouhaitee,
            Contraintes = ficheCourante.Contraintes,
            PiecesComplementaires = ficheCourante.PiecesComplementaires,
            PointsAPreciser = "Vérifiez les informations ci-dessus avant de soumettre."
        };

        var reponseEnrichie = new ReponseAssistant(
            "J'ai intégré votre précision dans la fiche. Vous pouvez la modifier directement ou continuer le dialogue.",
            ficheEnrichie,
            [],
            ["Vérification finale de la fiche avant soumission"]);

        return Task.FromResult(reponseEnrichie);
    }

    private static bool Contient(string texte, params string[] mots)
        => mots.Any(m => texte.Contains(m, StringComparison.OrdinalIgnoreCase));
}
