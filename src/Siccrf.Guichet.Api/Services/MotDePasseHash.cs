using System.Security.Cryptography;

namespace Siccrf.Guichet.Api.Services;

/// <summary>
/// Hachage de mot de passe PBKDF2 (démonstrateur, données fictives).
/// Format stocké : itérations.sel.hash (base64).
/// </summary>
public static class MotDePasseHash
{
    private const int Iterations = 100_000;
    private const int TailleSel = 16;
    private const int TailleHash = 32;

    public static string Creer(string motDePasse)
    {
        var sel = RandomNumberGenerator.GetBytes(TailleSel);
        var hash = Rfc2898DeriveBytes.Pbkdf2(motDePasse, sel, Iterations, HashAlgorithmName.SHA256, TailleHash);
        return $"{Iterations}.{Convert.ToBase64String(sel)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verifier(string motDePasse, string stocke)
    {
        var parties = stocke.Split('.');
        if (parties.Length != 3)
        {
            return false;
        }

        var iterations = int.Parse(parties[0]);
        var sel = Convert.FromBase64String(parties[1]);
        var hashAttendu = Convert.FromBase64String(parties[2]);
        var hashCalcule = Rfc2898DeriveBytes.Pbkdf2(motDePasse, sel, iterations, HashAlgorithmName.SHA256, hashAttendu.Length);

        return CryptographicOperations.FixedTimeEquals(hashAttendu, hashCalcule);
    }
}
