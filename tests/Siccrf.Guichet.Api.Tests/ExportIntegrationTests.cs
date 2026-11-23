using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Siccrf.Guichet.Api.Contrats;

namespace Siccrf.Guichet.Api.Tests;

/// <summary>
/// Tests d'intégration de l'export de fiche (incrément 6) :
/// Word (.docx) et PDF, droits identiques à la consultation.
/// </summary>
public class ExportIntegrationTests : TestsApiBase
{
    public ExportIntegrationTests(GuichetApiFactory factory) : base(factory) { }

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

    [Fact]
    public async Task Exporter_en_docx_retourne_un_document_word_valide()
    {
        var demandeId = await CreerDemandeAsync("agent.ecommerce", "Besoin d'export de la fiche en Word.");

        var jeton = await SeConnecterAsync("agent.ecommerce");
        var client = CreerClientAuthentifie(jeton);

        var reponse = await client.GetAsync($"/api/demandes/{demandeId}/export?format=docx");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            reponse.Content.Headers.ContentType!.MediaType);
        Assert.Contains("attachment", reponse.Content.Headers.ContentDisposition!.DispositionType);
        Assert.EndsWith(".docx", reponse.Content.Headers.ContentDisposition.FileName);

        var octets = await reponse.Content.ReadAsByteArrayAsync();
        Assert.True(octets.Length > 0, "Le document Word ne doit pas être vide.");
        // Un .docx est une archive ZIP : signature "PK".
        Assert.Equal((byte)'P', octets[0]);
        Assert.Equal((byte)'K', octets[1]);
    }

    [Fact]
    public async Task Exporter_en_pdf_retourne_un_document_pdf_valide()
    {
        var demandeId = await CreerDemandeAsync("agent.ecommerce", "Besoin d'export de la fiche en PDF.");

        var jeton = await SeConnecterAsync("agent.ecommerce");
        var client = CreerClientAuthentifie(jeton);

        var reponse = await client.GetAsync($"/api/demandes/{demandeId}/export?format=pdf");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("application/pdf", reponse.Content.Headers.ContentType!.MediaType);
        Assert.EndsWith(".pdf", reponse.Content.Headers.ContentDisposition!.FileName);

        var octets = await reponse.Content.ReadAsByteArrayAsync();
        Assert.True(octets.Length > 0, "Le document PDF ne doit pas être vide.");
        // Signature PDF : "%PDF".
        Assert.Equal((byte)'%', octets[0]);
        Assert.Equal((byte)'P', octets[1]);
        Assert.Equal((byte)'D', octets[2]);
        Assert.Equal((byte)'F', octets[3]);
    }

    [Fact]
    public async Task Exporter_avec_un_format_inconnu_retourne_400()
    {
        var demandeId = await CreerDemandeAsync("agent.ecommerce", "Besoin d'export.");

        var jeton = await SeConnecterAsync("agent.ecommerce");
        var client = CreerClientAuthentifie(jeton);

        var reponse = await client.GetAsync($"/api/demandes/{demandeId}/export?format=xlsx");

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task Un_agent_ne_peut_pas_exporter_la_fiche_d_un_autre()
    {
        var demandeId = await CreerDemandeAsync("agent.ecommerce", "Besoin d'export.");

        var jetonPilotage = await SeConnecterAsync("agent.pilotage");
        var clientPilotage = CreerClientAuthentifie(jetonPilotage);

        var reponse = await clientPilotage.GetAsync($"/api/demandes/{demandeId}/export?format=pdf");

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    [Fact]
    public async Task Exporter_une_demande_inexistante_retourne_404()
    {
        var jeton = await SeConnecterAsync("agent.ecommerce");
        var client = CreerClientAuthentifie(jeton);

        var reponse = await client.GetAsync($"/api/demandes/{Guid.NewGuid()}/export?format=docx");

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }
}
