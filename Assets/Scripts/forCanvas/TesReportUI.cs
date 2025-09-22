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

    [TextArea(3, 6), Tooltip("Testo libero (usato solo se la sezione CustomText � selezionata)")]
    public string customText = "";
}

public class TesReportUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI reportText;

    [Header("Formatting (Inspector-editable)")]
    public Color axisColorRG = new(0.9f, 0.23f, 0.2f);
    public Color axisColorBY = new(0.2f, 0.6f, 0.9f);
    public Color axisColorNeutral = Color.gray;
    public Color summaryColor = Color.black;

    [Header("Pages (define exactly which sections you want per page)")]
    public List<PageDefinition> Pages = new();

    // runtime
    private string[] renderedPages;
    private int currentPage;

    // Public API: Call this to generate pages from the TesResult and display the first one
    public void ShowReport(TesResult result, int startPage = 0)
    {
        if (reportText == null)
        {
            Debug.LogError("[TesReportUI_CustomPages] reportText non assegnato!");
            return;
        }

        BuildRenderedPages(result);
        ShowPage(startPage);
    }

    public void ShowPage(int pageIndex)
    {
        if (renderedPages == null || renderedPages.Length == 0)
        {
            reportText.text = "<i>No report generated.</i>";
            return;
        }
        currentPage = Mathf.Clamp(pageIndex, 0, renderedPages.Length - 1);
        reportText.text = renderedPages[currentPage];
    }

    public void NextPage() => ShowPage(currentPage + 1);
    public void PrevPage() => ShowPage(currentPage - 1);
    public int PageCount => renderedPages?.Length ?? 0;
    public int CurrentPageIndex => currentPage;

    // builds all pages in memory, one per PageDefinition
    private void BuildRenderedPages(TesResult r)
    {
        var resultPages = new List<string>();

        foreach (var pageDef in Pages)
        {
            var sb = new StringBuilder();

            // each section is built with the corresponding function
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

        // fallback: if you have no pages defined, create one with summary
        if (resultPages.Count == 0)
        {
            resultPages.Add(BuildHeader() + "\n\n" + BuildSummary(r));
        }

        renderedPages = resultPages.ToArray();
        currentPage = 0;
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
            sb.AppendFormat("<b>Risultato:</b> {0}\n", WrapColor(r.VerdictMessage, vCol));
        }


        return sb.ToString();
    }

    private string BuildTileDetails(TesResult r)
    {
        if (r.TileErrors == null || r.TileErrors.Count == 0)
            return "<i>Nessun errore sui tasselli</i>\n";

        var sb = new StringBuilder();
        sb.AppendLine("<b>I 10 principali errori (ordinati per gravit�):</b>");

        int i = 0;
        foreach (var t in r.TileErrors)
        {
            if (i >= 10) break;

            string axisColored = WrapColor(t.Axis, AxisToColor(t.Axis));
            sb.AppendLine($"Row {t.RowIndex + 1} Pos {t.Pos} Cap {t.CapID} Color {ColorToHex(t.Color)} {axisColored} Hue={t.HueDeg:F0}� C={t.Chroma:F1} CE={t.CEj} Err={t.Err} Severity {t.TileSeverityPct:F1}%");

            i++;
        }
        return sb.ToString();
    }

    private string BuildTopProblems(TesResult r)
    {
        if (r.TileErrors == null || r.TileErrors.Count == 0)
            return "<i>Nessun errore sui tasselli</i>\n";

        var sb = new StringBuilder();
        sb.AppendLine("<b>I colori pi� problematici:</b>");
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
            return WrapColor("Tutto bene: non sono stati rilevati errori significativi", summaryColor) + "\n";
        if (r.PctRG > r.PctBY * 1.2f)
            return WrapColor("compromissione dell'asse rosso-verde", axisColorRG) + "\n";
        if (r.PctBY > r.PctRG * 1.2f)
            return WrapColor("compromissione dell'asse blu-giallo", axisColorBY) + "\n";
        return WrapColor("Errori misti o inconcludenti", axisColorNeutral) + "\n";
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
