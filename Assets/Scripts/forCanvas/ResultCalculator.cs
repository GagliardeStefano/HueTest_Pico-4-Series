using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ResultCalculator : MonoBehaviour
{
    [Header("user sequence")]
    private Dictionary<RectTransform, List<RectTransform>> inputRowMap = new();

    [Header("correct sequence")]
    private Dictionary<RectTransform, List<RectTransform>> correctRowMap = new();

    public RectTransform[] rows;

    private readonly int[] startCaps = { 1, 11, 21, 31 };

    public GameObject GridHolder;
    private HueTestGridBuilder builder;

    public GameObject TestContainer;
    public GameObject OutputContainer;


    void Start()
    {
        builder = GridHolder.GetComponent<HueTestGridBuilder>();
        SetTestMode(true);
    }

    public void CalculateOutputTest()
    {
        PopulateDictionaries();

        var tesResult = CalculateTotalErrorScore();
        tesResult.Verdict = ColorTestEvaluator.DecideAxisImpairment(tesResult);
        tesResult.VerdictMessage = ColorTestEvaluator.VerdictToMessage(tesResult.Verdict);

        ComputeSeverityMetrics(tesResult);

        var ui = FindObjectOfType<TesReportUI>();
        SetTestMode(false);
        ui.ShowReport(tesResult);

    }

    public void ResetTest()
    {
        builder.BuildGrid();
        SetTestMode(true);
    }

    private void SetTestMode(bool isTestMode) // switch between the test container and the output container
    {
        TestContainer.SetActive(isTestMode);
        OutputContainer.SetActive(!isTestMode);
    }

    private void PopulateDictionaries()
    {
        inputRowMap.Clear();
        correctRowMap.Clear();

        foreach (var row in rows)
        {
            var inputTiles = row.Cast<Transform>()
                                .Where(child => child.name.StartsWith($"{row.name}_"))
                                .Cast<RectTransform>()
                                .ToList();

            inputRowMap[row] = inputTiles;
            correctRowMap[row] = GetSortedTilesByCorrectOrder(inputTiles);
        }

        PrintRowMaps();
    }
    private List<RectTransform> GetSortedTilesByCorrectOrder(List<RectTransform> tiles)
    {
        int m = tiles.Count;
        return tiles.OrderBy(t => GetSortKey(t.name, m)).ToList();
    }

    private int GetSortKey(string tileName, int m)
    {
        if (tileName.Contains("_Start")) return 0;
        if (tileName.Contains("_End")) return m - 1;

        var parts = tileName.Split('_');
        return int.TryParse(parts[^1], out int idx) ? idx : m;
    }

    public TesResult CalculateTotalErrorScore()
    {
        var result = new TesResult
        {
            TileErrors = new List<TileErrorRecord>()
        };

        int m = 10; // tiles
        int baseline = 2;
        int maxCE = 2 * (m - 1); // = 18
        int totalSum = 0;
        int tp_rg = 0;
        int tp_by = 0;

        var axisCache = new string[rows.Length * m];


        for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            var row = rows[rowIndex];
            var inputTiles = inputRowMap[row];
            var correctTiles = correctRowMap[row];

            // CapID for tile
            int[] ids = PopulateCapIDArray(rowIndex, correctTiles, inputTiles);

            // sum CEj with j = 1, ..., m
            for (int j = 1; j < m - 1; j++)
            {
                int CEj = Mathf.Abs(ids[j] - ids[j - 1]) + Mathf.Abs(ids[j] - ids[j + 1]);
                int Err = CEj - baseline; // baseline = 2 with ordinal CapID
                Debug.Log($"riga: {row.name}, pos: {j} = |{ids[j]} - {ids[j - 1]}| + |{ids[j]} - {ids[j + 1]}| = {Mathf.Abs(ids[j] - ids[j - 1]) + Mathf.Abs(ids[j] - ids[j + 1])}");

                (string axis, Color32 c) = CalculateTileToAxisAssignment(rowIndex, row, j, ref axisCache, ref Err, ref tp_rg, ref tp_by);
                ColorConverter.GetHueChroma(c, out double hue, out double chroma);

                if (Err > 0)
                {
                    var rec = new TileErrorRecord
                    {
                        RowIndex = rowIndex,
                        Pos = j,
                        CapID = ids[j],
                        Color = c,
                        CEj = CEj,
                        Err = Err,
                        PctOfMax = 100 * Err / (float)maxCE,
                        Axis = axis,
                        HueDeg = hue,
                        Chroma = chroma
                    };
                    result.TileErrors.Add(rec);
                }

                totalSum += Err;

                Debug.Log($"riga: {row.name}, pos: {j}, cap: {ids[j]}, CEj={CEj}, Err={Err}, axis={axis}");
            }
        }

        return CalculateAndGetTesResult(ref result, totalSum, tp_rg, tp_by);
    }

    public int[] PopulateCapIDArray(int rowIndex, List<RectTransform> correctTiles, List<RectTransform> inputTiles, int m = 10)
    {
        int startCap = startCaps[rowIndex];
        int[] ids = new int[m];

        for (int j = 0; j < m; j++)
        {
            int correctIndex = correctTiles.IndexOf(inputTiles[j]);
            ids[j] = startCap + correctIndex;
        }

        return ids;
    }

    public (string, Color32) CalculateTileToAxisAssignment(int rowIndex, RectTransform row, int tilePos, ref string[] axisCache, ref int Err, ref int tp_rg, ref int tp_by, int m = 10)
    {
        int globalColorIndex = rowIndex * m + tilePos;
        string axis = axisCache[globalColorIndex];

        RectTransform tile = inputRowMap[row][tilePos];
        Color32 c = tile.GetComponent<Image>().color;

        if (axis == null)
        {
            axis = ColorConverter.Instance.AssignAxis(c); // return "RG", "BY"
            axisCache[globalColorIndex] = axis;
        }

        switch (axis)
        {
            case "RG": tp_rg += Err; break;
            case "BY": tp_by += Err; break;
            default: break;
        }

        return (axis, c);
    }

    public TesResult CalculateAndGetTesResult(ref TesResult result, int totalSum, int tp_rg, int tp_by)
    {
        // TES and TPES
        result.TotalTES = totalSum;
        result.TPES_RG = tp_rg;
        result.TPES_BY = tp_by;

        //distribution of errors on the axes
        int sumAxes = tp_rg + tp_by;
        if (sumAxes > 0)
        {
            result.PctRG = 100f * tp_rg / sumAxes;
            result.PctBY = 100f * tp_by / sumAxes;
        }
        else { result.PctRG = result.PctBY = 0; }

        result.TileErrors = result.TileErrors.OrderByDescending(t => t.Err).ToList();

        return result;
    }

    // user axis severity
    public void ComputeSeverityMetrics(TesResult result, int m = 10, int baseline = 2)
    {
        if (result == null) return;

        int CEjMax = 2 * (m - 1);             // m=10 => 18
        int ErrMaxPerPos = CEjMax - baseline; // m=10 => 16

        int rgPositions = 0;
        int byPositions = 0;
        int neutralPositions = 0;

        List<TileErrorRecord> tilesError = result.TileErrors;

        for (int i = 0; i < tilesError.Count; i++)
        {
            TileErrorRecord tileErr = result.TileErrors[i];

            string axis = tileErr.Axis;

            if (axis == "RG") rgPositions++;
            else if (axis == "BY") byPositions++;
            else neutralPositions++;
        }

        // Max possible per axis
        int maxRG = rgPositions * ErrMaxPerPos;
        int maxBY = byPositions * ErrMaxPerPos;
        int maxTotal = (rgPositions + byPositions + neutralPositions) * ErrMaxPerPos;

        result.MaxPossibleRG = maxRG;
        result.MaxPossibleBY = maxBY;
        result.TES_norm_pct = maxTotal > 0 ? 100f * result.TotalTES / (float)maxTotal : 0f;
        result.SeverityRGpct = maxRG > 0 ? 100f * result.TPES_RG / (float)maxRG : 0f;
        result.SeverityBYpct = maxBY > 0 ? 100f * result.TPES_BY / (float)maxBY : 0f;

        if (result.TileErrors != null)
        {
            foreach (var t in result.TileErrors)
            {
                t.TileSeverityPct = ErrMaxPerPos > 0 ? 100f * t.Err / (float)ErrMaxPerPos : 0f;
            }
        }
    }

    public void PrintRowMaps()
    {
        Debug.Log("\n\n=== Input Row Map ===");
        foreach (var (row, tiles) in inputRowMap)
            Debug.Log($"{row.name} -> [{string.Join(", ", tiles.Select(t => t.name))}]");

        Debug.Log("=== Correct Row Map ===");
        foreach (var (row, tiles) in correctRowMap)
            Debug.Log($"{row.name} -> [{string.Join(", ", tiles.Select(t => t.name))}]");
    }

    // Update is called once per frame
    void Update()
    {

    }
}
