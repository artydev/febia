using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siccrf.Guichet.Api.Contrats;
using Siccrf.Guichet.Api.Services;

namespace Siccrf.Guichet.Api.Controllers;

/// <summary>Endpoints du circuit de validation : soumission, file du chef de service, décisions.</summary>
[ApiController]
[Route("api")]
[Authorize]
public class ValidationsController(ValidationService validations) : ControllerBase
{
    /// <summary>Soumet une demande (confirmation explicite requise).</summary>
    [HttpPost("demandes/{id:guid}/soumettre")]
    public async Task<ActionResult<DemandeDto>> Soumettre(Guid id, [FromBody] SoumettreRequest requete, CancellationToken ct)
    {
        try
        {
            var demande = await validations.SoumettreAsync(id, AgentCourant(), requete.Confirme, ct);
            return Ok(demande);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erreur = ex.Message });
        }
    }

    /// <summary>File des demandes en attente de validation (chef de service).</summary>
    [HttpGet("validations/en-attente")]
    [Authorize(Roles = "ChefService")]
    public async Task<ActionResult<IReadOnlyList<DemandeEnAttenteDto>>> EnAttente(CancellationToken ct)
    {
        var file = await validations.FileEnAttenteAsync(AgentCourant(), ct);
        return Ok(file);
    }

    /// <summary>Décision d'un valideur (valider / à compléter / refuser).</summary>
    [HttpPost("validations/{demandeId:guid}/decision")]
    [Authorize(Roles = "ChefService")]
    public async Task<ActionResult<ValidationDto>> Decider(Guid demandeId, [FromBody] DecisionRequest requete, CancellationToken ct)
    {
        try
        {
            var validation = await validations.DeciderAsync(demandeId, AgentCourant(), requete, ct);
            return Ok(validation);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erreur = ex.Message });
        }
    }

    /// <summary>Identité de l'agent courant, extraite du JWT (claim Sub).</summary>
    private Guid AgentCourant()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
