using System;
using UnityEngine;

// records for each tile
[Serializable]
public class TileErrorRecord
{
    public int RowIndex;
    public int Pos;
    public int CapID;
    public Color32 Color;
    public int CEj;      // raw CE
    public int Err;      // CEj - baseline (2)
    public float PctOfMax; // normalized with respect to maxCE (2*(m-1))
    public float TileSeverityPct; // Err / ErrMaxPerPos * 100

    public string Axis; // "RG", "BY"
    public double HueDeg; // hue angle(LCh)
    public double Chroma; // chroma (LCh)
}
