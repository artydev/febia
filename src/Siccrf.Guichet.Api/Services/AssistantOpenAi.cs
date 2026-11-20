using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Siccrf.Guichet.Api.Domaine;

namespace Siccrf.Guichet.Api.Services;

/// <summary>Configuration de l'assistant IA réel (OpenAI ou Azure OpenAI).</summary>
public class OptionsAssistantOpenAi
{
    public const string Section = "AssistantOpenAi";

    /// <summary>URL de l'API (ex. https://api.openai.com/v1/chat/completions ou endpoint Azure).</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Clé API (jamais en dur dans le code ; uniquement via configuration utilisateur).</summary>
    public string CleApi { get; set; } = string.Empty;

    public string Modele { get; set; } = "gpt-4o-mini";
}

/// <summary>
/// Implémentation réelle de l'assistant IA via OpenAI / Azure OpenAI (chat completions).
/// Activée par configuration ; jamais requise pour la démonstration.
/// </summary>
public class AssistantOpenAi : IAssistantService
{
    private readonly OptionsAssistantOpenAi _options;
    private readonly HttpClient _http;

    public AssistantOpenAi(IOptions<OptionsAssistantOpenAi> options, HttpClient http)
    {
        _options = options.Value;
        _http = http;
    }

    public async Task<ReponseAssistant> TraiterMessageAsync(
        Demande demande,
        VersionFiche ficheCourante,
        string message,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.CleApi) || string.IsNullOrWhiteSpace(_options.Url))
        {
            throw new InvalidOperationException(
                "L'assistant OpenAI n'est pas configuré (CleApi/Url manquantes). Utilisez l'assistant simulé pour la démonstration.");
        }

        var systeme = """
            Tu es l'assistant du guichet intelligent des demandes d'évolution du SI de la DGCCRF.
            Tu aides un agent à constituer une fiche d'expression de besoin conforme au modèle SICCRF.
            Règles strictes :
            - Ne complète jamais par invention les informations absentes ; signale les points à préciser.
            - Distingue le besoin métier de la solution suggérée.
            - Les incidents, demandes d'assistance, d'équipement et d'habilitation sont hors périmètre : oriente sans créer de demande.
            - Réponds en JSON avec les champs : message, fiche (rubriques du modèle), questionsComplementaires, pointsAPreciser.
            """;

        var contenu = new
        {
            model = _options.Modele,
            messages = new object[]
            {
                new { role = "system", content = systeme },
                new { role = "user", content = $"Demande n° {demande.Numero}. Fiche courante : {JsonSerializer.Serialize(ficheCourante)}. Message de l'agent : {message}" }
            },
            temperature = 0.2
        };

        using var requete = new HttpRequestMessage(HttpMethod.Post, _options.Url);
        requete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.CleApi);
        requete.Content = new StringContent(JsonSerializer.Serialize(contenu), Encoding.UTF8, "application/json");

        using var reponse = await _http.SendAsync(requete, ct);
        reponse.EnsureSuccessStatusCode();

        var corps = await reponse.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(corps);
        var choix = doc.RootElement.GetProperty("choices")[0];
        var contenuAssistant = choix.GetProperty("message").GetProperty("content").GetString() ?? "{}";

        // L'implémentation réelle est volontairement simplifiée : le parsing complet
        // du JSON produit par le modèle sera affiné lors de l'incrément 2 (dialogue).
        return new ReponseAssistant(
            contenuAssistant,
            null,
            [],
            ["Implémentation réelle à affiner (incrément 2)"]);
    }
}
