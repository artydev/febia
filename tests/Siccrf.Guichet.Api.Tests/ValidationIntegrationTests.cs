using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Siccrf.Guichet.Api.Contrats;
using Siccrf.Guichet.Api.Domaine;
using Siccrf.Guichet.Api.Infrastructure;

namespace Siccrf.Guichet.Api.Tests;

public class ValidationIntegrationTests : TestsApiBase
{
    public ValidationIntegrationTests(GuichetApiFactory factory) : base(factory) { }

    private async Task<Guid> CreerDemandeAsync(string identifiant, string message)
    {
        var jeton = await SeConnecterAsync(identifiant);
        var client = CreerClientAuthentifie(jeton);

        var requete = new CreerDemandeRequest(message);
        var reponse = await client.PostAsJsonAsync("/api/demandes", requete);
        reponse.EnsureSuccessStatusCode();

        var creation = await reponse.Content.ReadFromJsonAsync<CreationDemandeDto>();
        Assert.NotNull(creation);
        return creation!.Demande.Id;
    }

    private async Task SoumettreAsync(string identifiant, Guid demandeId)
    {
        var jeton = await SeConnecterAsync(identifiant);
        var client = CreerClientAuthentifie(jeton);

        var requete = new SoumettreRequest(Confirme: true);
        var reponse = await client.PostAsJsonAsync($"/api/demandes/{demandeId}/soumettre", requete);
        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }

    [Fact]
    public async Task Soumettre_une_demande_change_le_statut_et_notifie_le_valideur()
    {
        var demandeId = await CreerDemandeAsync("agent.ecommerce", "Besoin initial vague.");
        await SoumettreAsync("agent.ecommerce", demandeId);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GuichetDbContext>();

        var demande = db.Demandes.Single(d => d.Id == demandeId);
        Assert.Equal(StatutDemande.Soumise, demande.Statut);
        Assert.NotNull(demande.DateSoumission);
        Assert.NotNull(demande.ValideurId);

        Assert.Contains(db.ChangementsStatut, c => c.DemandeId == demandeId && c.StatutApres == StatutDemande.Soumise);
        Assert.Contains(db.Notifications, n => n.DemandeId == demandeId && n.Type == TypeNotification.Soumission);
    }

    [Fact]
    public async Task La_soumission_requiert_une_confirmation_explicite()
    {
        var demandeId = await CreerDemandeAsync("agent.ecommerce", "Besoin initial vague.");

        var jeton = await SeConnecterAsync("agent.ecommerce");
        var client = CreerClientAuthentifie(jeton);

        var requete = new SoumettreRequest(Confirme: false);
        var reponse = await client.PostAsJsonAsync($"/api/demandes/{demandeId}/soumettre", requete);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GuichetDbContext>();
        Assert.Equal(StatutDemande.Brouillon, db.Demandes.Single(d => d.Id == demandeId).Statut);
    }

    [Fact]
    public async Task Le_chef_de_service_voit_les_demandes_soumises_de_son_perimetre()
    {
        var demandeId = await CreerDemandeAsync("agent.ecommerce", "Besoin initial vague.");
        await SoumettreAsync("agent.ecommerce", demandeId);

        var jetonChef = await SeConnecterAsync("chef.service");
        var clientChef = CreerClientAuthentifie(jetonChef);

        var reponse = await clientChef.GetAsync("/api/validations/en-attente");
        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);

        var file = await reponse.Content.ReadFromJsonAsync<List<DemandeEnAttenteDto>>();
        Assert.NotNull(file);
        Assert.Contains(file!, d => d.Id == demandeId);
    }

    [Fact]
    public async Task Un_agent_simple_ne_peut_pas_consulter_la_file_de_validation()
    {
        var jetonAgent = await SeConnecterAsync("agent.ecommerce");
        var clientAgent = CreerClientAuthentifie(jetonAgent);

        var reponse = await clientAgent.GetAsync("/api/validations/en-attente");
        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    [Fact]
    public async Task Valider_une_demande_change_le_statut_et_trace_la_decision()
    {
        var demandeId = await CreerDemandeAsync("agent.ecommerce", "Besoin initial vague.");
        await SoumettreAsync("agent.ecommerce", demandeId);

        var jetonChef = await SeConnecterAsync("chef.service");
        var clientChef = CreerClientAuthentifie(jetonChef);

        var requete = new DecisionRequest(Decision: "Valider", Commentaire: null);
        var reponse = await clientChef.PostAsJsonAsync($"/api/validations/{demandeId}/decision", requete);
        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);

        var validation = await reponse.Content.ReadFromJsonAsync<ValidationDto>();
        Assert.NotNull(validation);
        Assert.Equal("Valider", validation!.Decision);
        Assert.Equal("Soumise", validation.StatutAvant);
        Assert.Equal("Validee", validation.StatutApres);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GuichetDbContext>();
        Assert.Equal(StatutDemande.Validee, db.Demandes.Single(d => d.Id == demandeId).Statut);
        Assert.Contains(db.Validations, v => v.DemandeId == demandeId && v.Decision == TypeDecision.Valider);
    }

    [Fact]
    public async Task Demander_des_complements_oblige_a_une_motivation()
    {
        var demandeId = await CreerDemandeAsync("agent.ecommerce", "Besoin initial vague.");
        await SoumettreAsync("agent.ecommerce", demandeId);

        var jetonChef = await SeConnecterAsync("chef.service");
        var clientChef = CreerClientAuthentifie(jetonChef);

        var requete = new DecisionRequest(Decision: "DemanderComplements", Commentaire: null);
        var reponse = await clientChef.PostAsJsonAsync($"/api/validations/{demandeId}/decision", requete);
        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task Apres_un_retour_pour_complement_l_agent_peut_editer_et_resoumettre()
    {
        var demandeId = await CreerDemandeAsync("agent.ecommerce", "Besoin initial vague.");
        await SoumettreAsync("agent.ecommerce", demandeId);

        // Le chef demande des compléments (avec motivation)
        var jetonChef = await SeConnecterAsync("chef.service");
        var clientChef = CreerClientAuthentifie(jetonChef);
        var decision = new DecisionRequest(Decision: "DemanderComplements", Commentaire: "Merci de préciser le périmètre.");
        var reponseDecision = await clientChef.PostAsJsonAsync($"/api/validations/{demandeId}/decision", decision);
        Assert.Equal(HttpStatusCode.OK, reponseDecision.StatusCode);

        // L'agent peut éditer la fiche (statut ACompleter)
        var jetonAgent = await SeConnecterAsync("agent.ecommerce");
        var clientAgent = CreerClientAuthentifie(jetonAgent);
        var edition = new EditerFicheRequest(
            ApplicationOuService: null, IntituleCourt: null, Synthese: "Synthèse complétée après retour.",
            SituationActuelle: null, DifficultesRencontrees: null, ExempleRepresentatif: null,
            ResultatRecherche: null, CriteresDeReussite: null, SolutionEnvisagee: null,
            UtilisateursConcernes: null, ProcessusMetierConcerne: null, BeneficesAttendus: null,
            EcheanceSouhaitee: null, Contraintes: null, PiecesComplementaires: null);
        var reponseEdition = await clientAgent.PutAsJsonAsync($"/api/demandes/{demandeId}/fiche", edition);
        Assert.Equal(HttpStatusCode.OK, reponseEdition.StatusCode);

        // L'agent peut resoumettre
        await SoumettreAsync("agent.ecommerce", demandeId);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GuichetDbContext>();
        Assert.Equal(StatutDemande.Soumise, db.Demandes.Single(d => d.Id == demandeId).Statut);
    }

    [Fact]
    public async Task Le_valideur_notifie_est_le_compte_qui_decide()
    {
        var demandeId = await CreerDemandeAsync("agent.ecommerce", "Besoin initial vague.");
        await SoumettreAsync("agent.ecommerce", demandeId);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GuichetDbContext>();

        var demande = db.Demandes.Single(d => d.Id == demandeId);
        var valideur = db.Responsables.Single(r => r.Id == demande.ValideurId);
        var compteValideur = db.Agents.Single(a => a.Id == valideur.AgentId);

        Assert.Equal("chef.service", compteValideur.Identifiant);
        var notification = db.Notifications.Single(n => n.DemandeId == demandeId && n.Type == TypeNotification.Soumission);
        Assert.Equal(compteValideur.Courriel, notification.Destinataire);
    }

    [Fact]
    public async Task Un_chef_de_service_ne_peut_pas_valider_sa_propre_demande()
    {
        var demandeId = await CreerDemandeAsync("chef.service", "Besoin initial vague.");
        await SoumettreAsync("chef.service", demandeId);

        var requete = new DecisionRequest(Decision: "Valider", Commentaire: null);

        // Son propre compte est refusé, et la demande n'apparaît pas dans sa file
        var clientChef = CreerClientAuthentifie(await SeConnecterAsync("chef.service"));
        var refus = await clientChef.PostAsJsonAsync($"/api/validations/{demandeId}/decision", requete);
        Assert.Equal(HttpStatusCode.Forbidden, refus.StatusCode);

        var file = await clientChef.GetFromJsonAsync<List<DemandeEnAttenteDto>>("/api/validations/en-attente");
        Assert.DoesNotContain(file!, d => d.Id == demandeId);

        // Le responsable hiérarchique (sous-direction) peut valider
        var clientSousDirecteur = CreerClientAuthentifie(await SeConnecterAsync("sous.directeur"));
        var ok = await clientSousDirecteur.PostAsJsonAsync($"/api/validations/{demandeId}/decision", requete);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [Fact]
    public async Task Un_chef_de_service_d_un_autre_perimetre_ne_peut_pas_decider()
    {
        var demandeId = await CreerDemandeAsync("agent.ecommerce", "Besoin initial vague.");
        await SoumettreAsync("agent.ecommerce", demandeId);

        var clientAutreChef = CreerClientAuthentifie(await SeConnecterAsync("chef.service2"));
        var reponse = await clientAutreChef.PostAsJsonAsync(
            $"/api/validations/{demandeId}/decision", new DecisionRequest("Valider", null));
        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    [Fact]
    public async Task Un_agent_simple_ne_peut_pas_decider()
    {
        var demandeId = await CreerDemandeAsync("agent.ecommerce", "Besoin initial vague.");
        await SoumettreAsync("agent.ecommerce", demandeId);

        var jetonAgent = await SeConnecterAsync("agent.ecommerce");
        var clientAgent = CreerClientAuthentifie(jetonAgent);

        var requete = new DecisionRequest(Decision: "Valider", Commentaire: null);
        var reponse = await clientAgent.PostAsJsonAsync($"/api/validations/{demandeId}/decision", requete);
        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }
}
