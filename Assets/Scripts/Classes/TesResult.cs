using System;
using System.Collections.Generic;

// overall result
[Serializable]
public class TesResult
{
    public int TotalTES;
    public int TPES_RG; // Total Partial Error Score for the red–green axis
    public int TPES_BY; // Total Partial Error Score for the blue–yellow axis
    public float PctRG; // Percentage of errors due to the red-green axis
    public float PctBY; // Percentage of errors due to the blue-yellow axis
    public List<TileErrorRecord> TileErrors; // descending ordinate by Err

    public int MaxPossibleRG;
    public int MaxPossibleBY;
    public float SeverityRGpct;   // TPES_RG / MaxPossibleRG * 100
    public float SeverityBYpct;   // TPES_BY / MaxPossibleBY * 100
    public float TES_norm_pct;    // TotalTES / MaxPossibleTotal * 100

    public AxisVerdict Verdict;
    public string VerdictMessage;
}
