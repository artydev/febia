using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Siccrf.Guichet.Api.Domaine;
using Siccrf.Guichet.Api.Infrastructure;

namespace Siccrf.Guichet.Api.Services;

/// <summary>Résultat d'une authentification.</summary>
public record ResultatAuthentification(
    string Jeton,
    DateTime Expiration,
    Guid AgentId,
    string NomComplet,
    string Profil);

/// <summary>
/// Authentification simulée : vérifie l'identifiant/mot de passe (seed fictif)
/// et émet un JWT contenant l'identité et le rôle de l'agent.
/// </summary>
public class AuthService(GuichetDbContext db, IOptions<OptionsJwt> optionsJwt)
{
    public async Task<ResultatAuthentification?> SeConnecterAsync(string identifiant, string motDePasse, CancellationToken ct = default)
    {
        var agent = await db.Agents
            .SingleOrDefaultAsync(a => a.Identifiant == identifiant, ct);

        if (agent is null || !MotDePasseHash.Verifier(motDePasse, agent.MotDePasseHash))
        {
            return null;
        }

        var options = optionsJwt.Value;
        var maintenant = DateTime.UtcNow;
        var expiration = maintenant.AddMinutes(options.DureeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, agent.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, agent.Identifiant),
            new(ClaimTypes.Name, agent.Identifiant),
            new(ClaimTypes.Role, agent.Profil.ToString()),
            new("profil", agent.Profil.ToString()),
            new("nom_complet", agent.NomComplet)
        };

        var cle = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Cle));
        var credentials = new SigningCredentials(cle, SecurityAlgorithms.HmacSha256);

        var jeton = new JwtSecurityToken(
            issuer: options.Emetteur,
            audience: options.Audience,
            claims: claims,
            notBefore: maintenant,
            expires: expiration,
            signingCredentials: credentials);

        var ecriture = new JwtSecurityTokenHandler().WriteToken(jeton);

        return new ResultatAuthentification(ecriture, expiration, agent.Id, agent.NomComplet, agent.Profil.ToString());
    }
}
