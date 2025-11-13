using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

[Serializable]
public enum PageSection
{
    Header,
    Summary,
    TileDetails,
    TopProblems,
    Interpretation,
    CustomText
}

[Serializable]
public class PageDefinition
{
    [Tooltip("Nome descrittivo della pagina")]
    public string pageName = "Page";

    [Tooltip("Sezioni da includere in questa pagina (verranno concatenate nell'ordine specificato)")]
    public List<PageSection> sections = new() { PageSection.Header, PageSection.Summary };

    [TextArea(3, 6), Tooltip("Testo libero (usato solo se la sezione CustomText è selezionata)")]
    public string customText = "";
}

public class TesReportUI : MonoBehaviour
{
    public GameObject filterButton;
    private TextMeshProUGUI filterButtonText;

    public GameObject GridManager;

    [Header("UI")]
    public TextMeshProUGUI reportText;         // canvas principale
    public TextMeshProUGUI reportTextFilter;   // canvas filtrata

    [Header("Formatting (Inspector-editable)")]
    public Color axisColorRG = new(0.9f, 0.23f, 0.2f);
    public Color axisColorBY = new(0.2f, 0.6f, 0.9f);
    public Color axisColorNeutral = Color.gray;
    public Color summaryColor = Color.black;

    [Header("Pages (define exactly which sections you want per page)")]
    public List<PageDefinition> Pages = new();

    // runtime: due array/distinte pagine
    private string[] renderedPagesMain;
    private string[] renderedPagesFilter;

    private int currentPageMain;
    private int currentPageFilter;

    // Public API

    // Mostra solo report principale (nessun filtro)
    public void ShowReport(TesResult result, int startPage = 0)
    {
        if (reportText == null)
        {
            Debug.LogError("[TesReportUI_CustomPages] reportText non assegnato!");
            return;
        }

        // aggiorna stato del bottone filtro (se presente)
        UpdateFilterButtonLabel();

        // costruisci solo le pagine principali
        renderedPagesMain = BuildRenderedPagesForResult(result);
        // reset filtro
        renderedPagesFilter = null;

        currentPageMain = Mathf.Clamp(startPage, 0, (renderedPagesMain?.Length ?? 1) - 1);
        currentPageFilter = 0;

        ShowMainPage(currentPageMain);

        // se esiste, pulisco la canvas filtro (mostro messaggio)
        if (reportTextFilter != null)
            reportTextFilter.text = "<i>Filtro non applicato.</i>";
    }

    // Mostra report principale + report filtrato (quando l'utente conferma test con filtro)
    public void ShowReport(TesResult result, TesResult resultFilter, int startPage = 0)
    {
        if (reportText == null)
        {
            Debug.LogError("[TesReportUI_CustomPages] reportText non assegnato!");
            return;
        }

        // aggiorna stato del bottone filtro (se presente)
        UpdateFilterButtonLabel();

        renderedPagesMain = BuildRenderedPagesForResult(result);
        renderedPagesFilter = BuildRenderedPagesForResult(resultFilter);

        currentPageMain = Mathf.Clamp(startPage, 0, (renderedPagesMain?.Length ?? 1) - 1);
        currentPageFilter = Mathf.Clamp(startPage, 0, (renderedPagesFilter?.Length ?? 1) - 1);

        ShowMainPage(currentPageMain);
        ShowFilterPage(currentPageFilter);
    }

    private void UpdateFilterButtonLabel()
    {
        if (filterButton == null) return;
        filterButtonText = filterButton.GetComponentInChildren<TextMeshProUGUI>();
        bool isAffected = false;
        if (GridManager != null)
        {
            var gm = GridManager.GetComponent<GridManager>();
            if (gm != null)
                isAffected = gm.IsAffected();
        }

        if (filterButtonText != null)
        {
            filterButtonText.text = isAffected ? "Rimuovi filtro" : "Applica filtro";
        }
    }

    // Metodi per navigare le pagine principali
    public void ShowMainPage(int pageIndex)
    {
        if (renderedPagesMain == null || renderedPagesMain.Length == 0)
        {
            reportText.text = "<i>No report generated.</i>";
            return;
        }

        currentPageMain = Mathf.Clamp(pageIndex, 0, renderedPagesMain.Length - 1);
        reportText.text = renderedPagesMain[currentPageMain];
    }

    public void NextMainPage() => ShowMainPage(currentPageMain + 1);
    public void PrevMainPage() => ShowMainPage(currentPageMain - 1);

    // Metodi per navigare le pagine filtrate (se presenti)
    public void ShowFilterPage(int pageIndex)
    {
        if (reportTextFilter == null)
            return;

        if (renderedPagesFilter == null || renderedPagesFilter.Length == 0)
        {
            reportTextFilter.text = "<i>No report generated (filtro).</i>";
            return;
        }

        currentPageFilter = Mathf.Clamp(pageIndex, 0, renderedPagesFilter.Length - 1);
        reportTextFilter.text = renderedPagesFilter[currentPageFilter];
    }

    public void NextFilterPage() => ShowFilterPage(currentPageFilter + 1);
    public void PrevFilterPage() => ShowFilterPage(currentPageFilter - 1);

    // proprietà utili
    public int PageCountMain => renderedPagesMain?.Length ?? 0;
    public int PageCountFilter => renderedPagesFilter?.Length ?? 0;
    public int CurrentPageIndexMain => currentPageMain;
    public int CurrentPageIndexFilter => currentPageFilter;

    // builds all pages in memory for a single TesResult and returns them (no UI side-effects)
    private string[] BuildRenderedPagesForResult(TesResult r)
    {
        var resultPages = new List<string>();

        foreach (var pageDef in Pages)
        {
            var sb = new StringBuilder();

            foreach (var sec in pageDef.sections)
            {
                switch (sec)
                {
                    case PageSection.Header:
                        sb.Append(BuildHeader());
                        sb.AppendLine();
                        break;
                    case PageSection.Summary:
                        sb.Append(BuildSummary(r));
                        sb.AppendLine();
                        break;
                    case PageSection.TileDetails:
                        sb.Append(BuildTileDetails(r));
                        sb.AppendLine();
                        break;
                    case PageSection.TopProblems:
                        sb.Append(BuildTopProblems(r));
                        sb.AppendLine();
                        break;
                    case PageSection.Interpretation:
                        sb.Append(BuildInterpretation(r));
                        sb.AppendLine();
                        break;
                    case PageSection.CustomText:
                        sb.Append(pageDef.customText ?? "");
                        sb.AppendLine();
                        break;
                }
            }

            resultPages.Add(sb.ToString().TrimEnd());
        }

        // fallback: se non ci sono pagine definite
        if (resultPages.Count == 0)
        {
            resultPages.Add(BuildHeader() + "\n\n" + BuildSummary(r));
        }

        return resultPages.ToArray();
    }

    #region Section builders

    private string BuildHeader() => "<b>Farnsworth-style Test Report</b>\n";

    private string BuildSummary(TesResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<b>Total TES:</b> {r.TotalTES}");
        sb.AppendLine($"<b>TPES R-G:</b> {r.TPES_RG}   |   <b>TPES B-Y:</b> {r.TPES_BY}");
        sb.AppendLine($"<b>Pct R-G:</b> {r.PctRG:F1}%   |   <b>Pct B-Y:</b> {r.PctBY:F1}%");
        sb.AppendLine($"<b>Tile errors recorded:</b> {r.TileErrors?.Count ?? 0}");
        sb.AppendLine();
        sb.AppendLine("<b>Severity (normalized)</b>:");
        sb.AppendLine($"TES normalized: {r.TES_norm_pct:F1}%");
        sb.AppendLine($"R-G severity: {r.SeverityRGpct:F1}% of axis max ({r.MaxPossibleRG} max)");
        sb.AppendLine($"B-Y severity: {r.SeverityBYpct:F1}% of axis max ({r.MaxPossibleBY} max)");
        sb.AppendLine();


        if (!string.IsNullOrEmpty(r.VerdictMessage))
        {
            var vCol = r.Verdict switch
            {
                AxisVerdict.Probable_RG => axisColorRG,
                AxisVerdict.Probable_BY => axisColorBY,
                AxisVerdict.Inconclusive => axisColorNeutral,
                _ => Color.green,
            };
            sb.AppendLine();
            sb.AppendFormat("<b>Risultato:</b> {0}\n", WrapColor(r.VerdictMessage, (Color32)vCol));
        }


        return sb.ToString();
    }

    private string BuildTileDetails(TesResult r)
    {
        if (r.TileErrors == null || r.TileErrors.Count == 0)
            return "<i>Nessun errore sui tasselli</i>\n";

        var sb = new StringBuilder();
        sb.AppendLine("<b>I 10 principali errori (ordinati per gravità):</b>");

        int i = 0;
        foreach (var t in r.TileErrors)
        {
            if (i >= 10) break;

            string axisColored = WrapColor(t.Axis, AxisToColor(t.Axis));
            sb.AppendLine($"Row {t.RowIndex + 1} Pos {t.Pos} Cap {t.CapID} Color {ColorToHex(t.Color)} {axisColored} Hue={t.HueDeg:F0}% C={t.Chroma:F1} CE={t.CEj} Err={t.Err} Severity {t.TileSeverityPct:F1}%");

            i++;
        }
        return sb.ToString();
    }

    private string BuildTopProblems(TesResult r)
    {
        if (r.TileErrors == null || r.TileErrors.Count == 0)
            return "<i>Nessun errore sui tasselli</i>\n";

        var sb = new StringBuilder();
        sb.AppendLine("<b>I colori più problematici:</b>");
        foreach (var (t, i) in r.TileErrors.Take(5).Select((t, i) => (t, i)))
        {
            string axisColored = WrapColor(t.Axis, AxisToColor(t.Axis));
            sb.AppendLine(
                $"{i + 1}. Row {t.RowIndex + 1} Pos {t.Pos} Cap {t.CapID} Color {ColorToHex(t.Color)} Axis {axisColored} Err {t.Err} Severity {t.TileSeverityPct:F1}%"
            );
        }
        return sb.ToString();
    }

    private string BuildInterpretation(TesResult r)
    {
        if (r.TotalTES == 0)
            return WrapColor("Tutto bene: non sono stati rilevati errori significativi", (Color32)summaryColor) + "\n";
        if (r.PctRG > r.PctBY * 1.2f)
            return WrapColor("compromissione dell'asse rosso-verde", (Color32)axisColorRG) + "\n";
        if (r.PctBY > r.PctRG * 1.2f)
            return WrapColor("compromissione dell'asse blu-giallo", (Color32)axisColorBY) + "\n";
        return WrapColor("Errori misti o inconcludenti", (Color32)axisColorNeutral) + "\n";
    }

    #endregion

    #region helpers

    private Color32 AxisToColor(string axis)
    {
        return axis switch
        {
            "RG" => (Color32)axisColorRG,
            "BY" => (Color32)axisColorBY,
            _ => (Color32)axisColorNeutral,
        };
    }

    private string WrapColor(string text, Color32 c)
    {
        return $"<color={ColorToHex(c)}>{text}</color>";
    }

    private static string ColorToHex(Color32 color)
    {
        return $"#{color.r:X2}{color.g:X2}{color.b:X2}";
    }

    #endregion
}
