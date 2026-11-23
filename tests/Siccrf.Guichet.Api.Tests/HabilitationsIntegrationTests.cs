using System.Net;
using System.Net.Http.Json;
using Siccrf.Guichet.Api.Contrats;

namespace Siccrf.Guichet.Api.Tests;

public class HabilitationsIntegrationTests : TestsApiBase
{
    public HabilitationsIntegrationTests(GuichetApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Le_login_retourne_un_jeton_avec_le_profil()
    {
        var jeton = await SeConnecterAsync("agent.ecommerce");
        Assert.False(string.IsNullOrWhiteSpace(jeton));
    }

    [Fact]
    public async Task Le_login_echoue_avec_un_mot_de_passe_incorrect()
    {
        var requete = new LoginRequest("agent.ecommerce", "mauvais");
        var reponse = await Client.PostAsJsonAsync("/api/auth/login", requete);
        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    [Fact]
    public async Task Un_endpoint_protege_retourne_401_sans_jeton()
    {
        var reponse = await Client.GetAsync("/api/demandes");
        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    [Fact]
    public async Task Un_agent_voit_uniquement_ses_propres_demandes()
    {
        // L'agent e-commerce crée une demande
        var jetonEcommerce = await SeConnecterAsync("agent.ecommerce");
        var clientEcommerce = CreerClientAuthentifie(jetonEcommerce);
        var requete = new CreerDemandeRequest("Besoin initial vague.");
        var reponseCreation = await clientEcommerce.PostAsJsonAsync("/api/demandes", requete);
        reponseCreation.EnsureSuccessStatusCode();

        // L'agent pilotage ne voit pas la demande de l'autre
        var jetonPilotage = await SeConnecterAsync("agent.pilotage");
        var clientPilotage = CreerClientAuthentifie(jetonPilotage);
        var reponseListe = await clientPilotage.GetAsync("/api/demandes");
        Assert.Equal(HttpStatusCode.OK, reponseListe.StatusCode);

        var liste = await reponseListe.Content.ReadFromJsonAsync<List<DemandeDto>>();
        Assert.NotNull(liste);
        Assert.DoesNotContain(liste!, d => d.Bureau == "Bureau E1" && d.Demandeur.Contains("Martin"));
    }

    [Fact]
    public async Task Le_bureau_XA_voit_toutes_les_demandes()
    {
        // L'agent e-commerce crée et soumet une demande (un brouillon resterait privé)
        await CreerDemandeSoumiseAsync();

        // Le bureau XA voit toutes les demandes (y compris celle de l'agent e-commerce)
        var jetonXa = await SeConnecterAsync("bureau.xa");
        var clientXa = CreerClientAuthentifie(jetonXa);
        var reponseListe = await clientXa.GetAsync("/api/demandes");
        Assert.Equal(HttpStatusCode.OK, reponseListe.StatusCode);

        var liste = await reponseListe.Content.ReadFromJsonAsync<List<DemandeDto>>();
        Assert.NotNull(liste);
        Assert.Contains(liste!, d => d.Bureau == "Bureau E1" && d.Demandeur.Contains("Martin"));
    }

    [Fact]
    public async Task Le_chef_de_service_voit_les_demandes_de_son_perimetre_dans_la_liste()
    {
        var demandeId = await CreerDemandeSoumiseAsync();

        var jetonChef = await SeConnecterAsync("chef.service");
        var clientChef = CreerClientAuthentifie(jetonChef);

        var reponse = await clientChef.GetAsync("/api/demandes");
        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);

        var liste = await reponse.Content.ReadFromJsonAsync<List<DemandeDto>>();
        Assert.NotNull(liste);
        Assert.Contains(liste!, d => d.Id == demandeId);
    }

    [Fact]
    public async Task Le_bureau_XA_peut_consulter_une_demande_soumise_qui_ne_lui_appartient_pas()
    {
        var demandeId = await CreerDemandeSoumiseAsync();

        var jetonXa = await SeConnecterAsync("bureau.xa");
        var clientXa = CreerClientAuthentifie(jetonXa);
        var reponse = await clientXa.GetAsync($"/api/demandes/{demandeId}");
        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }

    [Fact]
    public async Task Le_brouillon_d_un_agent_reste_prive_meme_pour_XA_et_SICCRF()
    {
        var jetonEcommerce = await SeConnecterAsync("agent.ecommerce");
        var clientEcommerce = CreerClientAuthentifie(jetonEcommerce);
        var reponseCreation = await clientEcommerce.PostAsJsonAsync("/api/demandes", new CreerDemandeRequest("Besoin initial vague."));
        reponseCreation.EnsureSuccessStatusCode();
        var creation = await reponseCreation.Content.ReadFromJsonAsync<CreationDemandeDto>();
        Assert.NotNull(creation);

        foreach (var identifiant in new[] { "bureau.xa", "siccrf" })
        {
            var client = CreerClientAuthentifie(await SeConnecterAsync(identifiant));
            var reponse = await client.GetAsync($"/api/demandes/{creation!.Demande.Id}");
            Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);

            var liste = await (await client.GetAsync("/api/demandes")).Content.ReadFromJsonAsync<List<DemandeDto>>();
            Assert.DoesNotContain(liste!, d => d.Id == creation!.Demande.Id);
        }
    }

    private async Task<Guid> CreerDemandeSoumiseAsync()
    {
        var jetonEcommerce = await SeConnecterAsync("agent.ecommerce");
        var clientEcommerce = CreerClientAuthentifie(jetonEcommerce);

        var requete = new CreerDemandeRequest("Besoin initial vague.");
        var reponseCreation = await clientEcommerce.PostAsJsonAsync("/api/demandes", requete);
        reponseCreation.EnsureSuccessStatusCode();
        var creation = await reponseCreation.Content.ReadFromJsonAsync<CreationDemandeDto>();
        Assert.NotNull(creation);

        var soumission = new SoumettreRequest(Confirme: true);
        var reponseSoumission = await clientEcommerce.PostAsJsonAsync($"/api/demandes/{creation!.Demande.Id}/soumettre", soumission);
        reponseSoumission.EnsureSuccessStatusCode();

        return creation.Demande.Id;
    }
}
