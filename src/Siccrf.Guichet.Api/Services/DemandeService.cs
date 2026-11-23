using Microsoft.EntityFrameworkCore;
using Siccrf.Guichet.Api.Contrats;
using Siccrf.Guichet.Api.Domaine;
using Siccrf.Guichet.Api.Infrastructure;

namespace Siccrf.Guichet.Api.Services;

/// <summary>
/// Service métier des demandes : création de brouillon, dialogue IA,
/// édition manuelle de la fiche (versionnée), acceptation des propositions IA.
/// Règle clé : les modifications manuelles ne sont jamais écrasées par l'IA.
/// </summary>
public class DemandeService(GuichetDbContext db, IAssistantService assistant)
{
    /// <summary>Crée une demande (brouillon) à partir du premier message de l'agent.</summary>
    public async Task<CreationDemandeDto> CreerDemandeAsync(Guid agentId, string message, CancellationToken ct = default)
    {
        var agent = await db.Agents.FindAsync([agentId], ct)
            ?? throw new InvalidOperationException("Agent inconnu.");

        var numero = await GenererNumeroAsync(ct);

        var demande = new Demande
        {
            Id = Guid.NewGuid(),
            Numero = numero,
            AgentId = agent.Id,
            Bureau = agent.Bureau,
            Statut = StatutDemande.Brouillon,
            Categorie = CategorieDemande.NouvelleFonctionnalite,
            DateCreation = DateTime.UtcNow
        };

        db.Demandes.Add(demande);
        await db.SaveChangesAsync(ct);

        // Fiche initiale vide (version 1) — le dialogue l'enrichit ensuite.
        // Synthese reste null pour que l'assistant pose ses questions ciblées dès le premier message.
        var ficheInitiale = new VersionFiche
        {
            Id = Guid.NewGuid(),
            DemandeId = demande.Id,
            NumeroVersion = 1,
            Source = SourceVersion.Ia,
            DateCreation = DateTime.UtcNow,
            AccepteeParAgent = false,
            Synthese = null
        };

        db.VersionsFiche.Add(ficheInitiale);
        await db.SaveChangesAsync(ct);

        demande.VersionCouranteId = ficheInitiale.Id;
        await db.SaveChangesAsync(ct);

        // Premier tour de dialogue : l'IA pose ses questions ciblées.
        var reponse = await assistant.TraiterMessageAsync(demande, ficheInitiale, message, ct);
        if (reponse.FicheProposee is not null)
        {
            db.VersionsFiche.Add(reponse.FicheProposee);
            await db.SaveChangesAsync(ct);
        }

        demande.VersionCourante = ficheInitiale;
        demande.Agent = agent;

        var dialogue = new ReponseDialogueDto(
            reponse.Message,
            reponse.FicheProposee?.VersDto(),
            reponse.QuestionsComplementaires,
            reponse.PointsAPreciser);

        return new CreationDemandeDto(demande.VersDto(), dialogue);
    }

    /// <summary>Traite un message de dialogue sur une demande existante.</summary>
    public async Task<ReponseDialogueDto> EnvoyerMessageAsync(Guid demandeId, Guid agentId, string message, CancellationToken ct = default)
    {
        var demande = await ChargerDemandeAsync(demandeId, ct);
        VerifierProprietaire(demande, agentId);
        VerifierBrouillon(demande);

        var ficheCourante = demande.VersionCourante
            ?? throw new InvalidOperationException("La demande n'a pas de fiche courante.");

        var reponse = await assistant.TraiterMessageAsync(demande, ficheCourante, message, ct);

        if (reponse.FicheProposee is not null)
        {
            db.VersionsFiche.Add(reponse.FicheProposee);
            await db.SaveChangesAsync(ct);
        }

        return new ReponseDialogueDto(
            reponse.Message,
            reponse.FicheProposee?.VersDto(),
            reponse.QuestionsComplementaires,
            reponse.PointsAPreciser);
    }

    /// <summary>
    /// Édition manuelle de la fiche : crée une nouvelle version (source Manuelle).
    /// Les champs non fournis conservent leur valeur courante.
    /// </summary>
    public async Task<FicheDto> EditerFicheAsync(Guid demandeId, Guid agentId, EditerFicheRequest requete, CancellationToken ct = default)
    {
        var demande = await ChargerDemandeAsync(demandeId, ct);
        VerifierProprietaire(demande, agentId);
        VerifierBrouillon(demande);

        var courante = demande.VersionCourante
            ?? throw new InvalidOperationException("La demande n'a pas de fiche courante.");

        var nouvelle = new VersionFiche
        {
            Id = Guid.NewGuid(),
            DemandeId = demande.Id,
            NumeroVersion = courante.NumeroVersion + 1,
            Source = SourceVersion.Manuelle,
            DateCreation = DateTime.UtcNow,
            AccepteeParAgent = true,
            ApplicationOuService = requete.ApplicationOuService ?? courante.ApplicationOuService,
            IntituleCourt = requete.IntituleCourt ?? courante.IntituleCourt,
            Synthese = requete.Synthese ?? courante.Synthese,
            SituationActuelle = requete.SituationActuelle ?? courante.SituationActuelle,
            DifficultesRencontrees = requete.DifficultesRencontrees ?? courante.DifficultesRencontrees,
            ExempleRepresentatif = requete.ExempleRepresentatif ?? courante.ExempleRepresentatif,
            ResultatRecherche = requete.ResultatRecherche ?? courante.ResultatRecherche,
            CriteresDeReussite = requete.CriteresDeReussite ?? courante.CriteresDeReussite,
            SolutionEnvisagee = requete.SolutionEnvisagee ?? courante.SolutionEnvisagee,
            UtilisateursConcernes = requete.UtilisateursConcernes ?? courante.UtilisateursConcernes,
            ProcessusMetierConcerne = requete.ProcessusMetierConcerne ?? courante.ProcessusMetierConcerne,
            BeneficesAttendus = requete.BeneficesAttendus ?? courante.BeneficesAttendus,
            EcheanceSouhaitee = requete.EcheanceSouhaitee ?? courante.EcheanceSouhaitee,
            Contraintes = requete.Contraintes ?? courante.Contraintes,
            PiecesComplementaires = requete.PiecesComplementaires ?? courante.PiecesComplementaires,
            PointsAPreciser = courante.PointsAPreciser
        };

        db.VersionsFiche.Add(nouvelle);
        await db.SaveChangesAsync(ct);

        demande.VersionCouranteId = nouvelle.Id;
        await db.SaveChangesAsync(ct);

        return nouvelle.VersDto();
    }

    /// <summary>Accepte explicitement une proposition de fiche faite par l'IA.</summary>
    public async Task<FicheDto> AccepterPropositionAsync(Guid demandeId, Guid agentId, Guid versionId, CancellationToken ct = default)
    {
        var demande = await ChargerDemandeAsync(demandeId, ct);
        VerifierProprietaire(demande, agentId);
        VerifierBrouillon(demande);

        var version = await db.VersionsFiche
            .SingleOrDefaultAsync(v => v.Id == versionId && v.DemandeId == demandeId, ct)
            ?? throw new InvalidOperationException("Version de fiche introuvable pour cette demande.");

        version.AccepteeParAgent = true;
        demande.VersionCouranteId = version.Id;
        await db.SaveChangesAsync(ct);

        return version.VersDto();
    }

    /// <summary>Consulte une demande (droits vérifiés côté serveur).</summary>
    public async Task<DemandeDto> ConsulterDemandeAsync(Guid demandeId, Guid agentId, CancellationToken ct = default)
    {
        var demande = await ChargerDemandeAsync(demandeId, ct);
        await VerifierAccesAsync(demande, agentId, ct);
        return demande.VersDto();
    }

    /// <summary>
    /// Liste les demandes selon le profil :
    /// agent → ses propres demandes ; chef de service → demandes de son périmètre ;
    /// BureauXA / SICCRF → toutes les demandes.
    /// </summary>
    public async Task<IReadOnlyList<DemandeDto>> ListerDemandesAsync(Guid agentId, CancellationToken ct = default)
    {
        var agent = await db.Agents.FindAsync([agentId], ct)
            ?? throw new InvalidOperationException("Agent inconnu.");

        IQueryable<Demande> requete = db.Demandes
            .Include(d => d.Agent)
            .Include(d => d.Valideur)
            .Include(d => d.VersionCourante);

        switch (agent.Profil)
        {
            case ProfilAgent.Agent:
                requete = requete.Where(d => d.AgentId == agentId);
                break;

            case ProfilAgent.ChefService:
                // Périmètre : demandes dont le valideur est le responsable rattaché au chef de service.
                var responsableIds = await db.Responsables
                    .Where(r => r.AgentId == agentId)
                    .Select(r => r.Id)
                    .ToListAsync(ct);
                requete = requete.Where(d => d.ValideurId != null && responsableIds.Contains(d.ValideurId.Value));
                break;

            case ProfilAgent.BureauXA:
            case ProfilAgent.Siccrf:
                // Toutes les demandes soumises ; les brouillons des autres restent privés.
                requete = requete.Where(d => d.Statut != StatutDemande.Brouillon || d.AgentId == agentId);
                break;

            default:
                throw new UnauthorizedAccessException("Profil non géré pour la liste des demandes.");
        }

        var demandes = await requete
            .OrderByDescending(d => d.DateCreation)
            .ToListAsync(ct);

        return demandes.Select(d => d.VersDto()).ToList();
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

    /// <summary>
    /// Vérifie l'accès à une demande selon le profil :
    /// propriétaire, chef de service du périmètre, BureauXA ou SICCRF.
    /// </summary>
    private async Task VerifierAccesAsync(Demande demande, Guid agentId, CancellationToken ct)
    {
        if (demande.AgentId == agentId)
        {
            return;
        }

        var agent = await db.Agents.FindAsync([agentId], ct)
            ?? throw new UnauthorizedAccessException("Agent inconnu.");

        switch (agent.Profil)
        {
            case ProfilAgent.BureauXA:
            case ProfilAgent.Siccrf:
                if (demande.Statut != StatutDemande.Brouillon)
                {
                    return;
                }
                break; // un brouillon reste privé à son auteur

            case ProfilAgent.ChefService:
                var estValideur = await db.Responsables
                    .AnyAsync(r => r.Id == demande.ValideurId && r.AgentId == agentId, ct);
                if (estValideur)
                {
                    return;
                }
                break;
        }

        throw new UnauthorizedAccessException("Vous n'êtes pas autorisé à accéder à cette demande.");
    }

    private static void VerifierBrouillon(Demande demande)
    {
        if (demande.Statut != StatutDemande.Brouillon && demande.Statut != StatutDemande.ACompleter)
        {
            throw new InvalidOperationException("La demande n'est plus un brouillon : le dialogue et l'édition sont fermés.");
        }
    }

    private async Task<string> GenererNumeroAsync(CancellationToken ct)
    {
        var annee = DateTime.UtcNow.Year;
        var prefixe = $"EB-{annee}-";
        var dernier = await db.Demandes
            .Where(d => d.Numero.StartsWith(prefixe))
            .OrderByDescending(d => d.Numero)
            .Select(d => d.Numero)
            .FirstOrDefaultAsync(ct);

        var rang = 1;
        if (dernier is not null && int.TryParse(dernier[prefixe.Length..], out var dernierRang))
        {
            rang = dernierRang + 1;
        }

        return $"{prefixe}{rang:D4}";
    }
}
