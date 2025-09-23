using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ResultTestCalculator : MonoBehaviour
{
    [Header("Parent della griglia")]
    public Transform GridManager;       // contiene tutti i cubi Row1_… Row4_…

    [Header("UI Containers")]
    public GameObject OutputContainer;

    private Dictionary<int, List<Transform>> rowTilesMap = new();
    private readonly int[] startCaps = { 1, 11, 21, 31 };
    private string[] axisCache;

    void Start()
    {
        SetTestMode(true);
    }

    public void CalculateOutputTest()
    {
        PopulateRowTilesMap();
        var result = CalculateTotalErrorScore();
        result.Verdict = ColorTestEvaluator.DecideAxisImpairment(result);
        result.VerdictMessage = ColorTestEvaluator.VerdictToMessage(result.Verdict);
        ComputeSeverityMetrics(result);
        FindObjectOfType<TesReportUI>().ShowReport(result);
        SetTestMode(false);
    }

    private void SetTestMode(bool isTestMode)
    {
        OutputContainer.SetActive(!isTestMode);
    }

    private void PopulateRowTilesMap()
    {
        rowTilesMap.Clear();
        // prendo tutti i cubi che iniziano con "Row"
        var allCubes = GridManager
            .Cast<Transform>()
            .Where(t => t.name.StartsWith("Row"))
            .ToList();

        // raggruppa per riga (1…4)
        foreach (var cube in allCubes)
        {
            int rowIndex = GetRowIndex(cube.name);
            if (rowIndex < 1 || rowIndex > 4) continue;

            if (!rowTilesMap.ContainsKey(rowIndex))
                rowTilesMap[rowIndex] = new List<Transform>();

            rowTilesMap[rowIndex].Add(cube);
        }

        // ordina ciascuna riga in base alla posizione X
        foreach (var kv in rowTilesMap)
            kv.Value.Sort((a, b) => a.localPosition.x.CompareTo(b.localPosition.x));

        // inizializza cache assi: 4 righe × 10 elementi ciascuna
        int perRow = rowTilesMap.Values.First().Count;
        axisCache = new string[4 * perRow];
    }

    private int GetRowIndex(string name)
    {
        // nome tipo "Row3_Tile5" o "Row2_Start"
        var prefix = name.Split('_')[0];    // "Row3"
        return int.TryParse(prefix.Substring(3), out int idx) ? idx : -1;
    }

    private TesResult CalculateTotalErrorScore()
    {
        var result = new TesResult { TileErrors = new List<TileErrorRecord>() };
        int totalSum = 0, tp_rg = 0, tp_by = 0;

        foreach (var kv in rowTilesMap)
        {
            int rowIndex = kv.Key;
            var rowTiles = kv.Value;
            int m = rowTiles.Count;     // sempre 10
            int baseline = 2, maxCE = 2 * (m - 1);

            int[] ids = PopulateCapIDArray(rowIndex, rowTiles);

            for (int j = 1; j < m - 1; j++)
            {
                int CEj = Mathf.Abs(ids[j] - ids[j - 1])
                        + Mathf.Abs(ids[j] - ids[j + 1]);
                int Err = CEj - baseline;

                var (axis, color) = CalculateTileToAxis(
                    rowIndex, rowTiles, j, ref Err, ref tp_rg, ref tp_by
                );

                ColorConverter.GetHueChroma(color, out double hue, out double chroma);

                if (Err > 0)
                {
                    result.TileErrors.Add(new TileErrorRecord
                    {
                        RowIndex = rowIndex,
                        Pos = j,
                        CapID = ids[j],
                        Color = color,
                        CEj = CEj,
                        Err = Err,
                        PctOfMax = 100 * Err / (float)maxCE,
                        Axis = axis,
                        HueDeg = hue,
                        Chroma = chroma
                    });
                }

                totalSum += Err;
            }
        }

        return CalculateAndGetTesResult(ref result, totalSum, tp_rg, tp_by);
    }

    private int GetNamePosition(string name, int m)
    {
        if (name.EndsWith("_Start")) return 0;
        if (name.EndsWith("_End")) return m - 1;

        // "Tile3" → idx = 3
        var part = name.Split('_').Last();   // "Tile3"
        if (part.StartsWith("Tile") &&
            int.TryParse(part.Substring(4), out int idx))
        {
            return idx;
        }

        return 0;
    }

    private int[] PopulateCapIDArray(int rowIndex, List<Transform> rowTiles)
    {
        int m = rowTiles.Count;
        int[] ids = new int[m];
        int baseCap = startCaps[rowIndex - 1];
        for (int j = 0; j < m; j++)
        {
            int pos = GetNamePosition(rowTiles[j].name, m);
            ids[j] = baseCap + pos;
        }
        return ids;
    }

    private (string axis, Color color) CalculateTileToAxis(
        int rowIndex,
        List<Transform> rowTiles,
        int j,
        ref int Err,
        ref int tp_rg,
        ref int tp_by
    )
    {
        int perRow = rowTilesMap[rowIndex].Count;
        int globalIdx = (rowIndex - 1) * perRow + j;   // shift per 1-based rowIndex
        string axis = axisCache[globalIdx];

        Color c = rowTiles[j].GetComponent<Renderer>().material.color;

        if (axis == null)
        {
            axis = ColorConverter.Instance.AssignAxis(c);
            axisCache[globalIdx] = axis;
        }

        if (axis == "RG") tp_rg += Err;
        else if (axis == "BY") tp_by += Err;

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

    /*public void PrintRowMaps()
    {
        Debug.Log("\n\n=== Input Row Map ===");
        foreach (var (row, tiles) in inputRowMap)
            Debug.Log($"{row.name} -> [{string.Join(", ", tiles.Select(t => t.name))}]");

        Debug.Log("=== Correct Row Map ===");
        foreach (var (row, tiles) in correctRowMap)
            Debug.Log($"{row.name} -> [{string.Join(", ", tiles.Select(t => t.name))}]");
    }*/
}