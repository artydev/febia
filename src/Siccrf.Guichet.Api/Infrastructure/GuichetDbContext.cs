using Microsoft.EntityFrameworkCore;
using Siccrf.Guichet.Api.Domaine;

namespace Siccrf.Guichet.Api.Infrastructure;

public class GuichetDbContext(DbContextOptions<GuichetDbContext> options) : DbContext(options)
{
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Responsable> Responsables => Set<Responsable>();
    public DbSet<Rattachement> Rattachements => Set<Rattachement>();
    public DbSet<Demande> Demandes => Set<Demande>();
    public DbSet<VersionFiche> VersionsFiche => Set<VersionFiche>();
    public DbSet<Validation> Validations => Set<Validation>();
    public DbSet<Avis> Avis => Set<Avis>();
    public DbSet<ChangementStatut> ChangementsStatut => Set<ChangementStatut>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Agent
        modelBuilder.Entity<Agent>(e =>
        {
            e.HasIndex(a => a.Identifiant).IsUnique();
            e.Property(a => a.Identifiant).HasMaxLength(50);
            e.Property(a => a.Nom).HasMaxLength(100);
            e.Property(a => a.Prenom).HasMaxLength(100);
            e.Property(a => a.Bureau).HasMaxLength(100);
        });

        // Responsable
        modelBuilder.Entity<Responsable>(e =>
        {
            e.Property(r => r.Nom).HasMaxLength(100);
            e.Property(r => r.Prenom).HasMaxLength(100);
            e.Property(r => r.Service).HasMaxLength(100);
            e.Property(r => r.Courriel).HasMaxLength(200);
            e.HasOne(r => r.Agent)
                .WithMany()
                .HasForeignKey(r => r.AgentId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(r => r.AgentId).IsUnique();
        });

        // Rattachement
        modelBuilder.Entity<Rattachement>(e =>
        {
            e.HasIndex(r => r.AgentId).IsUnique(); // un seul valideur par agent
            e.HasOne(r => r.Agent)
                .WithMany(a => a.Rattachements)
                .HasForeignKey(r => r.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Responsable)
                .WithMany(r => r.Rattachements)
                .HasForeignKey(r => r.ResponsableId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(r => r.Service).HasMaxLength(100);
        });

        // Demande
        modelBuilder.Entity<Demande>(e =>
        {
            e.HasIndex(d => d.Numero).IsUnique();
            e.Property(d => d.Numero).HasMaxLength(20);
            e.Property(d => d.Bureau).HasMaxLength(100);
            e.HasOne(d => d.Agent)
                .WithMany(a => a.Demandes)
                .HasForeignKey(d => d.AgentId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.Valideur)
                .WithMany()
                .HasForeignKey(d => d.ValideurId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.VersionCourante)
                .WithMany()
                .HasForeignKey(d => d.VersionCouranteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // VersionFiche
        modelBuilder.Entity<VersionFiche>(e =>
        {
            e.HasOne(v => v.Demande)
                .WithMany(d => d.Versions)
                .HasForeignKey(v => v.DemandeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(v => v.ApplicationOuService).HasMaxLength(200);
            e.Property(v => v.IntituleCourt).HasMaxLength(200);
        });

        // Validation
        modelBuilder.Entity<Validation>(e =>
        {
            e.HasOne(v => v.Demande)
                .WithMany(d => d.Validations)
                .HasForeignKey(v => v.DemandeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(v => v.Auteur)
                .WithMany()
                .HasForeignKey(v => v.AuteurId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Avis
        modelBuilder.Entity<Avis>(e =>
        {
            e.HasOne(a => a.Demande)
                .WithMany(d => d.Avis)
                .HasForeignKey(a => a.DemandeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Auteur)
                .WithMany()
                .HasForeignKey(a => a.AuteurId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ChangementStatut
        modelBuilder.Entity<ChangementStatut>(e =>
        {
            e.HasOne(c => c.Demande)
                .WithMany(d => d.ChangementsStatut)
                .HasForeignKey(c => c.DemandeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Auteur)
                .WithMany()
                .HasForeignKey(c => c.AuteurId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Notification
        modelBuilder.Entity<Notification>(e =>
        {
            e.HasOne(n => n.Demande)
                .WithMany(d => d.Notifications)
                .HasForeignKey(n => n.DemandeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(n => n.Destinataire).HasMaxLength(200);
        });
    }
}
