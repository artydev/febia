using System.Text.Json;
using Siccrf.Guichet.Api.Domaine;

namespace Siccrf.Guichet.Api.Services;

/// <summary>
/// Analyse la réponse textuelle d'un modèle de langage (JSON attendu) et la transforme en
/// <see cref="ReponseAssistant"/>. Tolérante : JSON entouré de texte ou de balises markdown,
/// valeurs nulles ou « à renseigner » ignorées, réponse illisible → aucune modification de la fiche.
/// Règles métier appliquées ici :
/// - une valeur vide ou nulle du modèle signifie « rubrique inchangée » (jamais d'invention) ;
/// - une rubrique saisie à la main par l'agent n'est jamais remplacée par une proposition de l'IA ;
/// - une réponse hors périmètre ne propose aucune fiche.
/// </summary>
public static class AnalyseReponseModele
{
    private const int LongueurMaxRubrique = 4000;
    private const int LongueurMaxElementListe = 500;
    private const int NombreMaxElementsListe = 10;

    private static readonly HashSet<string> ValeursVides = new(StringComparer.OrdinalIgnoreCase)
    {
        "null", "n/a", "na", "none", "aucun", "inconnu",
        "à renseigner", "a renseigner", "non renseigné", "non renseigne"
    };

    /// <summary>Rubriques de la fiche que l'IA peut proposer (clé JSON, lecture, écriture).</summary>
    private static readonly (string Cle, Func<VersionFiche, string?> Lire, Action<VersionFiche, string?> Ecrire)[] Rubriques =
    [
        ("applicationOuService", v => v.ApplicationOuService, (v, s) => v.ApplicationOuService = s),
        ("intituleCourt", v => v.IntituleCourt, (v, s) => v.IntituleCourt = s),
        ("synthese", v => v.Synthese, (v, s) => v.Synthese = s),
        ("situationActuelle", v => v.SituationActuelle, (v, s) => v.SituationActuelle = s),
        ("difficultesRencontrees", v => v.DifficultesRencontrees, (v, s) => v.DifficultesRencontrees = s),
        ("exempleRepresentatif", v => v.ExempleRepresentatif, (v, s) => v.ExempleRepresentatif = s),
        ("resultatRecherche", v => v.ResultatRecherche, (v, s) => v.ResultatRecherche = s),
        ("criteresDeReussite", v => v.CriteresDeReussite, (v, s) => v.CriteresDeReussite = s),
        ("solutionEnvisagee", v => v.SolutionEnvisagee, (v, s) => v.SolutionEnvisagee = s),
        ("utilisateursConcernes", v => v.UtilisateursConcernes, (v, s) => v.UtilisateursConcernes = s),
        ("processusMetierConcerne", v => v.ProcessusMetierConcerne, (v, s) => v.ProcessusMetierConcerne = s),
        ("beneficesAttendus", v => v.BeneficesAttendus, (v, s) => v.BeneficesAttendus = s),
        ("echeanceSouhaitee", v => v.EcheanceSouhaitee, (v, s) => v.EcheanceSouhaitee = s),
        ("contraintes", v => v.Contraintes, (v, s) => v.Contraintes = s)
    ];

    /// <summary>Clés JSON des rubriques (utilisées pour décrire le format attendu dans le prompt).</summary>
    public static IReadOnlyList<string> ClesRubriques { get; } = Rubriques.Select(r => r.Cle).ToArray();

    public static ReponseAssistant Analyser(string contenuModele, Demande demande, VersionFiche ficheCourante)
    {
        var json = ExtraireJson(contenuModele);
        if (json is null)
        {
            return Illisible(contenuModele);
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var racine = doc.RootElement;
            if (racine.ValueKind != JsonValueKind.Object)
            {
                return Illisible(contenuModele);
            }

            var horsPerimetre = racine.TryGetProperty("horsPerimetre", out var hp) && hp.ValueKind == JsonValueKind.True;
            var message = Valeur(racine, "message");
            var questions = Liste(racine, "questionsComplementaires");
            var points = Liste(racine, "pointsAPreciser");

            if (horsPerimetre)
            {
                return new ReponseAssistant(
                    message ?? "Votre demande semble relever d'un autre dispositif (incident, assistance, équipement ou habilitation). " +
                               "Je vous oriente vers le dispositif approprié sans créer de demande d'évolution.",
                    null,
                    [],
                    points.Count > 0 ? points : ["Demande hors périmètre du guichet"]);
            }

            // La fiche est normalement dans l'objet « fiche » ; on tolère les rubriques à la racine.
            var sourceFiche = racine.TryGetProperty("fiche", out var f) && f.ValueKind == JsonValueKind.Object ? f : racine;
            var proposee = ConstruireProposition(sourceFiche, demande, ficheCourante, points);

            return new ReponseAssistant(
                message ?? "J'ai analysé votre message.",
                proposee,
                questions,
                points);
        }
        catch (JsonException)
        {
            return Illisible(contenuModele);
        }
    }

    /// <summary>
    /// Construit une nouvelle version proposée à partir de la fiche courante et des valeurs du modèle.
    /// Retourne null si le modèle n'apporte aucune modification exploitable.
    /// </summary>
    private static VersionFiche? ConstruireProposition(
        JsonElement sourceFiche,
        Demande demande,
        VersionFiche courante,
        IReadOnlyList<string> points)
    {
        var proposee = new VersionFiche
        {
            Id = Guid.NewGuid(),
            DemandeId = demande.Id,
            NumeroVersion = courante.NumeroVersion + 1,
            Source = SourceVersion.Ia,
            DateCreation = DateTime.UtcNow,
            AccepteeParAgent = false,
            PiecesComplementaires = courante.PiecesComplementaires
        };

        var modifie = false;
        foreach (var (cle, lire, ecrire) in Rubriques)
        {
            var actuelle = lire(courante);
            var retenue = actuelle;
            var proposition = Valeur(sourceFiche, cle);

            if (proposition is not null && !string.Equals(proposition, actuelle, StringComparison.Ordinal))
            {
                // Une rubrique saisie à la main n'est jamais remplacée par l'IA.
                var protegee = courante.Source == SourceVersion.Manuelle && !string.IsNullOrWhiteSpace(actuelle);
                if (!protegee)
                {
                    retenue = proposition;
                    modifie = true;
                }
            }

            ecrire(proposee, retenue);
        }

        if (!modifie)
        {
            return null;
        }

        proposee.PointsAPreciser = points.Count > 0 ? string.Join(" ; ", points) : courante.PointsAPreciser;
        return proposee;
    }

    /// <summary>Réponse non exploitable : on affiche le texte brut et on ne modifie pas la fiche.</summary>
    private static ReponseAssistant Illisible(string contenuModele)
    {
        var texte = (contenuModele ?? string.Empty).Trim();
        if (texte.Length > 2000)
        {
            texte = texte[..2000] + "…";
        }

        if (texte.Length == 0)
        {
            texte = "L'assistant n'a pas fourni de réponse exploitable.";
        }

        return new ReponseAssistant(
            texte,
            null,
            [],
            ["La réponse de l'IA n'était pas au format attendu : la fiche n'a pas été modifiée."]);
    }

    /// <summary>Isole l'objet JSON (du premier « { » au dernier « } »), même entouré de texte ou de balises markdown.</summary>
    private static string? ExtraireJson(string? contenu)
    {
        if (string.IsNullOrWhiteSpace(contenu))
        {
            return null;
        }

        var debut = contenu.IndexOf('{');
        var fin = contenu.LastIndexOf('}');
        return debut >= 0 && fin > debut ? contenu[debut..(fin + 1)] : null;
    }

    private static string? Valeur(JsonElement objet, string nom)
    {
        foreach (var propriete in objet.EnumerateObject())
        {
            if (propriete.Name.Equals(nom, StringComparison.OrdinalIgnoreCase))
            {
                return Nettoyer(VersTexte(propriete.Value), LongueurMaxRubrique);
            }
        }

        return null;
    }

    private static List<string> Liste(JsonElement objet, string nom)
    {
        var resultat = new List<string>();
        foreach (var propriete in objet.EnumerateObject())
        {
            if (!propriete.Name.Equals(nom, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (propriete.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in propriete.Value.EnumerateArray())
                {
                    var texte = Nettoyer(VersTexte(element), LongueurMaxElementListe);
                    if (texte is not null && resultat.Count < NombreMaxElementsListe)
                    {
                        resultat.Add(texte);
                    }
                }
            }
            else
            {
                var texte = Nettoyer(VersTexte(propriete.Value), LongueurMaxElementListe);
                if (texte is not null)
                {
                    resultat.Add(texte);
                }
            }
        }

        return resultat;
    }

    private static string? VersTexte(JsonElement valeur) => valeur.ValueKind switch
    {
        JsonValueKind.String => valeur.GetString(),
        JsonValueKind.Number => valeur.GetRawText(),
        JsonValueKind.Array => string.Join(
            " ; ",
            valeur.EnumerateArray().Select(VersTexte).Where(s => !string.IsNullOrWhiteSpace(s))),
        _ => null
    };

    private static string? Nettoyer(string? texte, int longueurMax)
    {
        if (string.IsNullOrWhiteSpace(texte))
        {
            return null;
        }

        texte = texte.Trim();
        if (ValeursVides.Contains(texte))
        {
            return null;
        }

        return texte.Length > longueurMax ? texte[..longueurMax] : texte;
    }
}

