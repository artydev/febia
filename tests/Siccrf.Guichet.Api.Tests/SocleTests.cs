using Microsoft.EntityFrameworkCore;
using Siccrf.Guichet.Api.Domaine;
using Siccrf.Guichet.Api.Infrastructure;

namespace Siccrf.Guichet.Api.Tests;

public class SocleTests
{
    private static GuichetDbContext CreerContexte()
    {
        var options = new DbContextOptionsBuilder<GuichetDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        var db = new GuichetDbContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public void Le_seed_initialise_les_donnees_fictives()
    {
        using var db = CreerContexte();
        SeedDonnees.Initialiser(db);

        Assert.True(db.Agents.Any());
        Assert.True(db.Responsables.Any());
        Assert.True(db.Rattachements.Any());
        Assert.True(db.Demandes.Any());
        Assert.True(db.VersionsFiche.Any());
        Assert.True(db.ChangementsStatut.Any());
        Assert.True(db.Notifications.Any());
    }

    [Fact]
    public void Le_seed_cree_la_demande_ecommerce_du_scenario()
    {
        using var db = CreerContexte();
        SeedDonnees.Initialiser(db);

        var demande = db.Demandes
            .Include(d => d.VersionCourante)
            .Single(d => d.Numero == "EB-2027-0001");

        Assert.Equal(StatutDemande.Brouillon, demande.Statut);
        Assert.NotNull(demande.VersionCourante);
        Assert.Contains("plates-formes e-commerce", demande.VersionCourante!.Synthese);
    }

    [Fact]
    public void Le_seed_est_idempotent()
    {
        using var db = CreerContexte();
        SeedDonnees.Initialiser(db);
        SeedDonnees.Initialiser(db);

        Assert.Equal(5, db.Agents.Count());
        Assert.Equal(2, db.Demandes.Count());
    }
}
