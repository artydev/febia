using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Siccrf.Guichet.Api.Contrats;
using Siccrf.Guichet.Api.Infrastructure;

namespace Siccrf.Guichet.Api.Tests;

/// <summary>
/// Fabrique d'application pour les tests d'intégration : base SQLite temporaire
/// (fichier unique par fabrique), seed exécuté au démarrage.
/// </summary>
public class GuichetApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"guichet-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Guichet", $"Data Source={_dbPath}");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch (IOException)
        {
            // Fichier temporaire encore verrouillé : ignoré (nettoyage OS ultérieur).
        }
    }
}

/// <summary>Base commune des tests d'intégration : connexion JWT et clients authentifiés.</summary>
public abstract class TestsApiBase : IClassFixture<GuichetApiFactory>
{
    protected readonly GuichetApiFactory Factory;
    protected readonly HttpClient Client;

    protected TestsApiBase(GuichetApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    /// <summary>Se connecte (login démo) et retourne le jeton JWT.</summary>
    protected async Task<string> SeConnecterAsync(string identifiant, string motDePasse = "demo")
    {
        var requete = new LoginRequest(identifiant, motDePasse);
        var reponse = await Client.PostAsJsonAsync("/api/auth/login", requete);
        reponse.EnsureSuccessStatusCode();
        var login = await reponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        return login!.Jeton;
    }

    /// <summary>Crée un client HTTP avec l'en-tête Authorization Bearer.</summary>
    protected HttpClient CreerClientAuthentifie(string jeton)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jeton);
        return client;
    }

    protected Guid IdAgent(string identifiant)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GuichetDbContext>();
        return db.Agents.Single(a => a.Identifiant == identifiant).Id;
    }
}

public class DemandesIntegrationTests : TestsApiBase
{
    public DemandesIntegrationTests(GuichetApiFactory factory) : base(factory) { }

    private async Task<(Guid DemandeId, Guid? PropositionId)> CreerDemandeAsync(string identifiant, string message)
    {
        var jeton = await SeConnecterAsync(identifiant);
        var client = CreerClientAuthentifie(jeton);

        var requete = new CreerDemandeRequest(message);
        var reponse = await client.PostAsJsonAsync("/api/demandes", requete);
        reponse.EnsureSuccessStatusCode();

        var creation = await reponse.Content.ReadFromJsonAsync<CreationDemandeDto>();
        Assert.NotNull(creation);
        return (creation!.Demande.Id, creation.Dialogue.FicheProposee?.Id);
    }

    [Fact]
    public async Task Creer_une_demande_retourne_la_demande_et_les_questions_de_l_IA()
    {
        var jeton = await SeConnecterAsync("agent.ecommerce");
        var client = CreerClientAuthentifie(jeton);

        var requete = new CreerDemandeRequest("Nous devons contrôler les plates-formes e-commerce et réaliser des prélèvements à distance.");
        var reponse = await client.PostAsJsonAsync("/api/demandes", requete);

        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        var creation = await reponse.Content.ReadFromJsonAsync<CreationDemandeDto>();
        Assert.NotNull(creation);
        Assert.StartsWith("EB-", creation!.Demande.Numero);
        Assert.Equal("Brouillon", creation.Demande.Statut);
        Assert.NotEmpty(creation.Dialogue.QuestionsComplementaires);
        Assert.NotNull(creation.Dialogue.FicheProposee);
        Assert.Contains("plates-formes e-commerce", creation.Dialogue.FicheProposee!.Synthese);
    }

    [Fact]
    public async Task Editer_la_fiche_cree_une_nouvelle_version_manuelle()
    {
        var (demandeId, _) = await CreerDemandeAsync("agent.ecommerce", "Besoin initial vague.");

        var jeton = await SeConnecterAsync("agent.ecommerce");
        var client = CreerClientAuthentifie(jeton);

        var requete = new EditerFicheRequest(
            ApplicationOuService: "Application de contrôle",
            IntituleCourt: "Contrôle e-commerce",
            Synthese: "Synthèse saisie manuellement par l'agent.",
            SituationActuelle: null,
            DifficultesRencontrees: null,
            ExempleRepresentatif: null,
            ResultatRecherche: null,
            CriteresDeReussite: null,
            SolutionEnvisagee: null,
            UtilisateursConcernes: null,
            ProcessusMetierConcerne: null,
            BeneficesAttendus: null,
            EcheanceSouhaitee: null,
            Contraintes: null,
            PiecesComplementaires: null);

        var reponse = await client.PutAsJsonAsync($"/api/demandes/{demandeId}/fiche", requete);
        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);

        var fiche = await reponse.Content.ReadFromJsonAsync<FicheDto>();
        Assert.NotNull(fiche);
        Assert.Equal(2, fiche!.NumeroVersion);
        Assert.Equal("Manuelle", fiche.Source);
        Assert.Equal("Synthèse saisie manuellement par l'agent.", fiche.Synthese);
    }

    [Fact]
    public async Task Le_dialogue_n_ecrase_pas_les_modifications_manuelles()
    {
        var (demandeId, _) = await CreerDemandeAsync("agent.ecommerce", "Besoin initial vague.");

        var jeton = await SeConnecterAsync("agent.ecommerce");
        var client = CreerClientAuthentifie(jeton);

        // Édition manuelle de la synthèse
        var edition = new EditerFicheRequest(
            ApplicationOuService: null, IntituleCourt: null, Synthese: "SYNTHESE_MANUELLE",
            SituationActuelle: null, DifficultesRencontrees: null, ExempleRepresentatif: null,
            ResultatRecherche: null, CriteresDeReussite: null, SolutionEnvisagee: null,
            UtilisateursConcernes: null, ProcessusMetierConcerne: null, BeneficesAttendus: null,
            EcheanceSouhaitee: null, Contraintes: null, PiecesComplementaires: null);
        await client.PutAsJsonAsync($"/api/demandes/{demandeId}/fiche", edition);

        // Nouveau message de dialogue
        var dialogue = new EnvoyerMessageRequest("Le problème actuel est un suivi manuel des contrôles.");
        var reponseDialogue = await client.PostAsJsonAsync($"/api/demandes/{demandeId}/dialogue", dialogue);
        Assert.Equal(HttpStatusCode.OK, reponseDialogue.StatusCode);

        // La fiche courante doit toujours être la version manuelle (non écrasée)
        var demande = await client.GetFromJsonAsync<DemandeDto>($"/api/demandes/{demandeId}");
        Assert.NotNull(demande);
        Assert.Equal("SYNTHESE_MANUELLE", demande!.FicheCourante!.Synthese);
        Assert.Equal("Manuelle", demande.FicheCourante.Source);
    }

    [Fact]
    public async Task Accepter_une_proposition_met_a_jour_la_version_courante()
    {
        var (demandeId, propositionId) = await CreerDemandeAsync("agent.ecommerce", "Besoin initial vague.");
        Assert.NotNull(propositionId);

        var jeton = await SeConnecterAsync("agent.ecommerce");
        var client = CreerClientAuthentifie(jeton);

        var requete = new AccepterPropositionRequest(propositionId!.Value);
        var reponse = await client.PostAsJsonAsync($"/api/demandes/{demandeId}/accepter-proposition", requete);
        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);

        var fiche = await reponse.Content.ReadFromJsonAsync<FicheDto>();
        Assert.NotNull(fiche);
        Assert.True(fiche!.AccepteeParAgent);
        Assert.Equal(propositionId, fiche.Id);

        var demande = await client.GetFromJsonAsync<DemandeDto>($"/api/demandes/{demandeId}");
        Assert.Equal(propositionId, demande!.FicheCourante!.Id);
    }

    [Fact]
    public async Task Un_agent_ne_peut_pas_acceder_a_la_demande_d_un_autre()
    {
        var (demandeId, _) = await CreerDemandeAsync("agent.ecommerce", "Besoin initial vague.");

        var jetonPilotage = await SeConnecterAsync("agent.pilotage");
        var clientPilotage = CreerClientAuthentifie(jetonPilotage);

        var reponse = await clientPilotage.GetAsync($"/api/demandes/{demandeId}");
        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }
}
