using Microsoft.AspNetCore.Mvc;
using Siccrf.Guichet.Api.Contrats;
using Siccrf.Guichet.Api.Services;

namespace Siccrf.Guichet.Api.Controllers;

/// <summary>Authentification simulée : connexion et émission du JWT.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    /// <summary>Connexion : retourne un JWT avec le rôle de l'agent.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest requete, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requete.Identifiant) || string.IsNullOrWhiteSpace(requete.MotDePasse))
        {
            return BadRequest(new { erreur = "Identifiant et mot de passe sont obligatoires." });
        }

        var resultat = await auth.SeConnecterAsync(requete.Identifiant, requete.MotDePasse, ct);
        if (resultat is null)
        {
            return Unauthorized(new { erreur = "Identifiant ou mot de passe incorrect." });
        }

        return Ok(new LoginResponse(
            resultat.Jeton,
            resultat.Expiration,
            resultat.AgentId,
            resultat.NomComplet,
            resultat.Profil));
    }
}
