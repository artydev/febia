using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Siccrf.Guichet.Api.Domaine;
using Siccrf.Guichet.Api.Services;

namespace Siccrf.Guichet.Api.Tests;

/// <summary>
/// Tests de l'assistant IA réel et de l'analyse de la réponse du modèle.
/// Aucun appel réseau : un gestionnaire HTTP factice simule le service d'IA.
/// </summary>
public class AssistantOpenAiTests
{
    private sealed class GestionnaireFactice(Func<HttpRequestMessage, HttpResponseMessage> reponse) : HttpMessageHandler
    {
        public string? DernierCorps { get; private set; }

        public int NombreAppels { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            NombreAppels++;
            DernierCorps = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return reponse(request);
        }
    }

    private static HttpResponseMessage EnveloppeOpenAi(string contenuModele)
    {
        var json = JsonSerializer.Serialize(new { choices = new[] { new { message = new { content = contenuModele } } } });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static AssistantOpenAi CreerAssistant(GestionnaireFactice gestionnaire, string url = "https://ia.exemple.test/v1/chat/completions")
    {
        var options = Options.Create(new OptionsAssistantOpenAi { Url = url, CleApi = "cle-de-test", Modele = "modele-test" });
        return new AssistantOpenAi(options, new HttpClient(gestionnaire));
    }

    private static Demande NouvelleDemande() => new()
    {
        Id = Guid.NewGuid(),
        Numero = "EB-2026-0042",
        Bureau = "Bureau E1",
        Agent = new Agent
        {
            Id = Guid.NewGuid(),
            Identifiant = "agent.test",
            Nom = "Martin",
            Prenom = "Claire",
            Bureau = "Bureau E1",
            Courriel = "claire.martin@dgccrf.finances.gouv.fr",
            MotDePasseHash = "hash-secret"
        }
    };

    private static VersionFiche FicheCourante(Demande demande, SourceVersion source = SourceVersion.Ia, string? synthese = null) => new()
    {
        Id = Guid.NewGuid(),
        DemandeId = demande.Id,
        NumeroVersion = 1,
        Source = source,
        DateCreation = DateTime.UtcNow,
        Synthese = synthese
    };

    [Fact]
    public async Task Une_reponse_json_valide_produit_une_proposition_de_fiche()
    {
        var demande = NouvelleDemande();
        var courante = FicheCourante(demande);
        var gestionnaire = new GestionnaireFactice(_ => EnveloppeOpenAi("""
            {
              "horsPerimetre": false,
              "message": "J'ai compris votre besoin.",
              "fiche": {
                "intituleCourt": "Contrôle des plates-formes e-commerce",
                "synthese": "Contrôler les plates-formes e-commerce à distance.",
                "situationActuelle": null,
                "beneficesAttendus": "Gain de temps"
              },
              "questionsComplementaires": ["Quel volume de contrôles ?", "Quelle échéance ?"],
              "pointsAPreciser": ["Volumétrie"]
            }
            """));

        var reponse = await CreerAssistant(gestionnaire).TraiterMessageAsync(demande, courante, "Je dois contrôler les plates-formes.");

        Assert.Equal("J'ai compris votre besoin.", reponse.Message);
        Assert.NotNull(reponse.FicheProposee);
        Assert.Equal("Contrôler les plates-formes e-commerce à distance.", reponse.FicheProposee!.Synthese);
        Assert.Equal("Gain de temps", reponse.FicheProposee.BeneficesAttendus);
        Assert.Null(reponse.FicheProposee.SituationActuelle); // null du modèle = inchangée
        Assert.Equal(SourceVersion.Ia, reponse.FicheProposee.Source);
        Assert.Equal(2, reponse.FicheProposee.NumeroVersion);
        Assert.Equal(demande.Id, reponse.FicheProposee.DemandeId);
        Assert.False(reponse.FicheProposee.AccepteeParAgent);
        Assert.Equal(2, reponse.QuestionsComplementaires.Count);
        Assert.Contains("Volumétrie", reponse.PointsAPreciser);
    }

    [Fact]
    public async Task Un_json_entoure_de_balises_markdown_est_accepte()
    {
        var demande = NouvelleDemande();
        var gestionnaire = new GestionnaireFactice(_ => EnveloppeOpenAi(
            "Voici ma réponse :\n```json\n{\"message\":\"Bonjour\",\"fiche\":{\"synthese\":\"Une synthèse\"}}\n```"));

        var reponse = await CreerAssistant(gestionnaire).TraiterMessageAsync(demande, FicheCourante(demande), "Besoin");

        Assert.Equal("Bonjour", reponse.Message);
        Assert.Equal("Une synthèse", reponse.FicheProposee?.Synthese);
    }

    [Fact]
    public async Task Une_reponse_non_json_ne_modifie_pas_la_fiche()
    {
        var demande = NouvelleDemande();
        var gestionnaire = new GestionnaireFactice(_ => EnveloppeOpenAi("Désolé, je ne peux pas répondre en JSON."));

        var reponse = await CreerAssistant(gestionnaire).TraiterMessageAsync(demande, FicheCourante(demande), "Besoin");

        Assert.Null(reponse.FicheProposee);
        Assert.Equal("Désolé, je ne peux pas répondre en JSON.", reponse.Message);
        Assert.NotEmpty(reponse.PointsAPreciser);
    }

    [Fact]
    public async Task Les_valeurs_a_renseigner_ou_vides_du_modele_sont_ignorees()
    {
        var demande = NouvelleDemande();
        var gestionnaire = new GestionnaireFactice(_ => EnveloppeOpenAi(
            "{\"message\":\"Ok\",\"fiche\":{\"synthese\":\"À renseigner\",\"contraintes\":\"  \",\"echeanceSouhaitee\":\"null\"}}"));

        var reponse = await CreerAssistant(gestionnaire).TraiterMessageAsync(demande, FicheCourante(demande), "Besoin");

        Assert.Null(reponse.FicheProposee); // aucune information exploitable : pas de nouvelle version
    }

    [Fact]
    public async Task Une_demande_hors_perimetre_ne_propose_aucune_fiche()
    {
        var demande = NouvelleDemande();
        var gestionnaire = new GestionnaireFactice(_ => EnveloppeOpenAi(
            "{\"horsPerimetre\":true,\"message\":\"Contactez l'assistance.\",\"fiche\":{\"synthese\":\"ne doit pas être retenue\"}}"));

        var reponse = await CreerAssistant(gestionnaire).TraiterMessageAsync(demande, FicheCourante(demande), "Mon ordinateur est en panne");

        Assert.Null(reponse.FicheProposee);
        Assert.Equal("Contactez l'assistance.", reponse.Message);
    }

    [Fact]
    public async Task Une_rubrique_saisie_a_la_main_n_est_jamais_remplacee_par_l_ia()
    {
        var demande = NouvelleDemande();
        var courante = FicheCourante(demande, SourceVersion.Manuelle, synthese: "SYNTHESE_MANUELLE");
        var gestionnaire = new GestionnaireFactice(_ => EnveloppeOpenAi(
            "{\"message\":\"Ok\",\"fiche\":{\"synthese\":\"Synthèse proposée par l'IA\",\"resultatRecherche\":\"Résultat proposé\"}}"));

        var reponse = await CreerAssistant(gestionnaire).TraiterMessageAsync(demande, courante, "Besoin");

        Assert.NotNull(reponse.FicheProposee);
        Assert.Equal("SYNTHESE_MANUELLE", reponse.FicheProposee!.Synthese); // protégée
        Assert.Equal("Résultat proposé", reponse.FicheProposee.ResultatRecherche); // rubrique vide : complétée
    }

    [Fact]
    public async Task Une_erreur_429_du_service_ne_leve_pas_d_exception()
    {
        var demande = NouvelleDemande();
        var gestionnaire = new GestionnaireFactice(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        var reponse = await CreerAssistant(gestionnaire).TraiterMessageAsync(demande, FicheCourante(demande), "Besoin");

        Assert.Null(reponse.FicheProposee);
        Assert.Contains("indisponible", reponse.Message);
        Assert.Contains("429", reponse.Message);
    }

    [Fact]
    public async Task Une_enveloppe_de_reponse_inattendue_est_geree()
    {
        var demande = NouvelleDemande();
        var gestionnaire = new GestionnaireFactice(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"erreur\":\"format inattendu\"}", Encoding.UTF8, "application/json")
        });

        var reponse = await CreerAssistant(gestionnaire).TraiterMessageAsync(demande, FicheCourante(demande), "Besoin");

        Assert.Null(reponse.FicheProposee);
        Assert.Contains("indisponible", reponse.Message);
    }

    [Fact]
    public async Task Sans_configuration_aucun_appel_n_est_emis()
    {
        var demande = NouvelleDemande();
        var gestionnaire = new GestionnaireFactice(_ => EnveloppeOpenAi("{}"));

        var reponse = await CreerAssistant(gestionnaire, url: "").TraiterMessageAsync(demande, FicheCourante(demande), "Besoin");

        Assert.Equal(0, gestionnaire.NombreAppels);
        Assert.Null(reponse.FicheProposee);
        Assert.Contains("pas configuré", reponse.Message);
    }

    [Fact]
    public async Task La_requete_n_envoie_ni_donnees_d_agent_ni_cle_dans_le_corps()
    {
        var demande = NouvelleDemande();
        var courante = FicheCourante(demande, synthese: "SYNTHESE_COURANTE");
        var gestionnaire = new GestionnaireFactice(_ => EnveloppeOpenAi("{\"message\":\"Ok\"}"));

        await CreerAssistant(gestionnaire).TraiterMessageAsync(demande, courante, "Message de l'agent");

        Assert.NotNull(gestionnaire.DernierCorps);
        Assert.Contains("SYNTHESE_COURANTE", gestionnaire.DernierCorps);
        Assert.Contains("Message de l'agent", gestionnaire.DernierCorps);
        Assert.Contains("EB-2026-0042", gestionnaire.DernierCorps);
        Assert.DoesNotContain("Martin", gestionnaire.DernierCorps);
        Assert.DoesNotContain("claire.martin", gestionnaire.DernierCorps);
        Assert.DoesNotContain("hash-secret", gestionnaire.DernierCorps);
        Assert.DoesNotContain("cle-de-test", gestionnaire.DernierCorps);
    }
}