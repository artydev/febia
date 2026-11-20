namespace Siccrf.Guichet.Api.Domaine;

/// <summary>Profil d'un agent dans le guichet.</summary>
public enum ProfilAgent
{
    Agent,
    ChefService,
    BureauXA,
    Siccrf
}

/// <summary>Statut d'une demande d'évolution.</summary>
public enum StatutDemande
{
    Brouillon,
    Soumise,
    EnInstruction,
    Validee,
    ACompleter,
    Refusee,
    Cloturee
}

/// <summary>Décision possible d'un valideur.</summary>
public enum TypeDecision
{
    Valider,
    DemanderComplements,
    Refuser
}

/// <summary>Catégorie d'une demande d'évolution.</summary>
public enum CategorieDemande
{
    NouvelleFonctionnalite,
    AmeliorationApplication,
    NouveauServiceNumerique,
    AutomatisationTaches,
    EvolutionTechnique,
    HorsPerimetre
}

/// <summary>Émetteur d'un avis (jamais fusionnés de façon opaque).</summary>
public enum EmetteurAvis
{
    BureauXA,
    Siccrf,
    Rssi,
    MissionDataIa
}

/// <summary>Type de notification (log simulé).</summary>
public enum TypeNotification
{
    Soumission,
    Decision,
    DemandeComplements,
    Information
}

/// <summary>Source d'une version de fiche.</summary>
public enum SourceVersion
{
    Ia,
    Manuelle
}
