using Siccrf.Guichet.Api.Domaine;

namespace Siccrf.Guichet.Api.Services;

/// <summary>Réponse de l'assistant IA à un message de dialogue.</summary>
public record ReponseAssistant(
    string Message,
    VersionFiche? FicheProposee,
    IReadOnlyList<string> QuestionsComplementaires,
    IReadOnlyList<string> PointsAPreciser);

/// <summary>
/// Service d'assistance IA. Deux implémentations :
/// <see cref="AssistantSimule"/> (défaut, sans clé API) et <see cref="AssistantOpenAi"/> (réel, par configuration).
/// </summary>
public interface IAssistantService
{
    /// <summary>
    /// Traite un message de l'agent dans le cadre d'une demande et retourne une réponse
    /// (message, fiche proposée, questions complémentaires, points à préciser).
    /// L'IA ne complète jamais par invention les informations absentes.
    /// </summary>
    Task<ReponseAssistant> TraiterMessageAsync(Demande demande, VersionFiche ficheCourante, string message, CancellationToken ct = default);
}
