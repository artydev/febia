using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siccrf.Guichet.Api.Contrats;
using Siccrf.Guichet.Api.Services;

namespace Siccrf.Guichet.Api.Controllers;

/// <summary>
/// Endpoints des demandes d'évolution : création, dialogue IA, édition de fiche,
/// acceptation de proposition, consultation, liste selon le profil.
/// Tous les endpoints exigent un JWT valide (incrément 4).
/// </summary>
[ApiController]
[Route("api/demandes")]
[Authorize]
public class DemandesController(DemandeService demandes, ExportFicheService exportFiche) : ControllerBase
{
    /// <summary>Crée une demande (brouillon) à partir du premier message de l'agent.</summary>
    [HttpPost]
    public async Task<ActionResult<CreationDemandeDto>> Creer([FromBody] CreerDemandeRequest requete, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requete.Message))
        {
            return BadRequest(new { erreur = "Le message initial est obligatoire." });
        }

        var creation = await demandes.CreerDemandeAsync(AgentCourant(), requete.Message, ct);
        return CreatedAtAction(nameof(Consulter), new { id = creation.Demande.Id }, creation);
    }

    /// <summary>Envoie un message dans le dialogue d'une demande.</summary>
    [HttpPost("{id:guid}/dialogue")]
    public async Task<ActionResult<ReponseDialogueDto>> EnvoyerMessage(Guid id, [FromBody] EnvoyerMessageRequest requete, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requete.Message))
        {
            return BadRequest(new { erreur = "Le message est obligatoire." });
        }

        try
        {
            var reponse = await demandes.EnvoyerMessageAsync(id, AgentCourant(), requete.Message, ct);
            return Ok(reponse);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { erreur = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { erreur = ex.Message });
        }
    }

    /// <summary>Édite manuellement la fiche d'une demande (nouvelle version).</summary>
    [HttpPut("{id:guid}/fiche")]
    public async Task<ActionResult<FicheDto>> EditerFiche(Guid id, [FromBody] EditerFicheRequest requete, CancellationToken ct)
    {
        try
        {
            var fiche = await demandes.EditerFicheAsync(id, AgentCourant(), requete, ct);
            return Ok(fiche);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { erreur = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { erreur = ex.Message });
        }
    }

    /// <summary>Accepte explicitement une proposition de fiche faite par l'IA.</summary>
    [HttpPost("{id:guid}/accepter-proposition")]
    public async Task<ActionResult<FicheDto>> AccepterProposition(Guid id, [FromBody] AccepterPropositionRequest requete, CancellationToken ct)
    {
        try
        {
            var fiche = await demandes.AccepterPropositionAsync(id, AgentCourant(), requete.VersionId, ct);
            return Ok(fiche);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { erreur = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { erreur = ex.Message });
        }
    }

    /// <summary>Consulte une demande (droits vérifiés côté serveur).</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DemandeDto>> Consulter(Guid id, CancellationToken ct)
    {
        try
        {
            var demande = await demandes.ConsulterDemandeAsync(id, AgentCourant(), ct);
            return Ok(demande);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { erreur = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { erreur = ex.Message });
        }
    }

    /// <summary>
    /// Exporte la fiche courante d'une demande au format Word (.docx) ou PDF.
    /// Droits identiques à la consultation : agent → ses demandes ; chef de service → périmètre ; XA/SICCRF → toutes.
    /// </summary>
    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> Exporter(Guid id, [FromQuery] string format = "docx", CancellationToken ct = default)
    {
        var formatExport = format.ToLowerInvariant() switch
        {
            "docx" => ExportFicheService.FormatExport.Docx,
            "pdf" => ExportFicheService.FormatExport.Pdf,
            _ => (ExportFicheService.FormatExport?)null
        };

        if (formatExport is null)
        {
            return BadRequest(new { erreur = "Format d'export inconnu. Utilisez 'docx' ou 'pdf'." });
        }

        try
        {
            var demande = await demandes.ConsulterDemandeAsync(id, AgentCourant(), ct);
            var fiche = demande.FicheCourante
                ?? throw new InvalidOperationException("La demande n'a pas de fiche courante à exporter.");

            var contenu = exportFiche.Generer(demande, fiche, formatExport.Value);
            var nomFichier = $"fiche-{demande.Numero}.{ExportFicheService.Extension(formatExport.Value)}";

            return File(contenu, ExportFicheService.MimeType(formatExport.Value), nomFichier);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { erreur = ex.Message });
        }
    }

    /// <summary>Liste des demandes selon le profil : agent → siennes ; chef de service → périmètre ; XA/SICCRF → toutes.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DemandeDto>>> Lister(CancellationToken ct)
    {
        var demandesListe = await demandes.ListerDemandesAsync(AgentCourant(), ct);
        return Ok(demandesListe);
    }

    /// <summary>Identité de l'agent courant, extraite du JWT (claim Sub).</summary>
    private Guid AgentCourant()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
