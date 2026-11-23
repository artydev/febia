using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Siccrf.Guichet.Api.Contrats;
using WordDocument = DocumentFormat.OpenXml.Wordprocessing.Document;
using QuestDocument = QuestPDF.Fluent.Document;

namespace Siccrf.Guichet.Api.Services;

/// <summary>
/// Génère un document (Word .docx ou PDF) à partir d'une fiche d'expression de besoin.
/// Incrément 6 : export de la fiche courante d'une demande.
/// </summary>
public class ExportFicheService
{
    /// <summary>Formats d'export pris en charge.</summary>
    public enum FormatExport
    {
        Docx,
        Pdf
    }

    /// <summary>Type MIME associé à chaque format.</summary>
    public static string MimeType(FormatExport format) => format switch
    {
        FormatExport.Docx => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        FormatExport.Pdf => "application/pdf",
        _ => "application/octet-stream"
    };

    /// <summary>Extension de fichier associée à chaque format.</summary>
    public static string Extension(FormatExport format) => format switch
    {
        FormatExport.Docx => "docx",
        FormatExport.Pdf => "pdf",
        _ => "bin"
    };

    /// <summary>
    /// Génère le contenu binaire de l'export de la fiche.
    /// </summary>
    public byte[] Generer(DemandeDto demande, FicheDto fiche, FormatExport format)
    {
        return format switch
        {
            FormatExport.Docx => GenererDocx(demande, fiche),
            FormatExport.Pdf => GenererPdf(demande, fiche),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Format d'export inconnu.")
        };
    }

    // --- Word (.docx) ---

    private static byte[] GenererDocx(DemandeDto demande, FicheDto fiche)
    {
        using var flux = new MemoryStream();
        using (var document = WordprocessingDocument.Create(flux, WordprocessingDocumentType.Document))
        {
            var partie = document.AddMainDocumentPart();
            partie.Document = new WordDocument();
            var corps = new Body();

            corps.Append(Titre("Fiche d'expression de besoin"));
            corps.Append(ParagrapheMeta($"Demande : {demande.Numero}"));
            corps.Append(ParagrapheMeta($"Statut : {demande.Statut}"));
            corps.Append(ParagrapheMeta($"Demandeur : {demande.Demandeur}"));
            corps.Append(ParagrapheMeta($"Bureau : {demande.Bureau}"));
            corps.Append(ParagrapheMeta($"Version de la fiche : {fiche.NumeroVersion} ({fiche.Source})"));
            corps.Append(ParagrapheMeta($"Date de création : {fiche.DateCreation:dd/MM/yyyy HH:mm} UTC"));

            corps.Append(new Paragraph(new Run(new Break())));

            foreach (var (libelle, valeur) in ChampsFiche(fiche))
            {
                corps.Append(ParagrapheChamp(libelle, valeur));
            }

            partie.Document.Append(corps);
            partie.Document.Save();
        }

        return flux.ToArray();
    }

    private static Paragraph Titre(string texte)
    {
        var run = new Run(new Text(texte))
        {
            RunProperties = new RunProperties(new Bold(), new FontSize { Val = "32" })
        };
        return new Paragraph(run) { ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines { After = "240" }) };
    }

    private static Paragraph ParagrapheMeta(string texte)
    {
        var run = new Run(new Text(texte))
        {
            RunProperties = new RunProperties(new FontSize { Val = "22" })
        };
        return new Paragraph(run);
    }

    private static Paragraph ParagrapheChamp(string libelle, string? valeur)
    {
        var paragraphe = new Paragraph
        {
            ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines { After = "120" })
        };

        var runLibelle = new Run(new Text(libelle))
        {
            RunProperties = new RunProperties(new Bold(), new FontSize { Val = "22" })
        };
        paragraphe.Append(runLibelle);

        if (!string.IsNullOrWhiteSpace(valeur))
        {
            paragraphe.Append(new Run(new Text(" : ")));
            paragraphe.Append(new Run(new Text(valeur)) { RunProperties = new RunProperties(new FontSize { Val = "22" }) });
        }
        else
        {
            paragraphe.Append(new Run(new Text(" : —")) { RunProperties = new RunProperties(new FontSize { Val = "22" }) });
        }

        return paragraphe;
    }

    // --- PDF ---

    private static byte[] GenererPdf(DemandeDto demande, FicheDto fiche)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return QuestDocument.Create(conteneur =>
        {
            conteneur.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(style => style.FontSize(11).FontColor(Colors.Black));

                page.Header().Column(colonne =>
                {
                    colonne.Item().Text("Fiche d'expression de besoin")
                        .FontSize(18).Bold().FontColor(Colors.Blue.Darken3);
                    colonne.Item().Text($"Demande {demande.Numero} — {demande.Statut}")
                        .FontSize(11).FontColor(Colors.Grey.Darken2);
                });

                page.Content().Column(colonne =>
                {
                    colonne.Spacing(6);
                    colonne.Item().Text($"Demandeur : {demande.Demandeur} — Bureau : {demande.Bureau}")
                        .FontSize(10).FontColor(Colors.Grey.Darken2);
                    colonne.Item().Text($"Version de la fiche : {fiche.NumeroVersion} ({fiche.Source}) — créée le {fiche.DateCreation:dd/MM/yyyy HH:mm} UTC")
                        .FontSize(10).FontColor(Colors.Grey.Darken2);
                    colonne.Item().PaddingTop(8);

                    foreach (var (libelle, valeur) in ChampsFiche(fiche))
                    {
                        colonne.Item().Text(t =>
                        {
                            t.Span(libelle).Bold();
                            t.Span(" : ");
                            t.Span(string.IsNullOrWhiteSpace(valeur) ? "—" : valeur);
                        });
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Document généré par le guichet intelligent SICCRF — démonstrateur");
                    t.CurrentPageNumber();
                });
            });
        }).GeneratePdf();
    }

    // --- Champs communs ---

    /// <summary>Liste ordonnée des champs de la fiche avec leur libellé.</summary>
    private static IEnumerable<(string Libelle, string? Valeur)> ChampsFiche(FicheDto fiche)
    {
        yield return ("Application ou service concerné", fiche.ApplicationOuService);
        yield return ("Intitulé court", fiche.IntituleCourt);
        yield return ("Synthèse", fiche.Synthese);
        yield return ("Situation actuelle", fiche.SituationActuelle);
        yield return ("Difficultés rencontrées", fiche.DifficultesRencontrees);
        yield return ("Exemple représentatif", fiche.ExempleRepresentatif);
        yield return ("Résultat recherché", fiche.ResultatRecherche);
        yield return ("Critères de réussite", fiche.CriteresDeReussite);
        yield return ("Solution envisagée", fiche.SolutionEnvisagee);
        yield return ("Utilisateurs concernés", fiche.UtilisateursConcernes);
        yield return ("Processus métier concerné", fiche.ProcessusMetierConcerne);
        yield return ("Bénéfices attendus", fiche.BeneficesAttendus);
        yield return ("Échéance souhaitée", fiche.EcheanceSouhaitee);
        yield return ("Contraintes", fiche.Contraintes);
        yield return ("Pièces complémentaires", fiche.PiecesComplementaires);
        yield return ("Points à préciser", fiche.PointsAPreciser);
    }
}
