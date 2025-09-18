using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum AxisVerdict { None, Probable_RG, Probable_BY, Inconclusive }

public class ColorTestEvaluator
{
    public static int Default_Tmin = 16;   // Minimum total TES threshold
    public static int Default_Amin = 8;    // Minimum TPES threshold on the dominant axis
    public static float Default_Pdom = 0.55f; // Percent dominance (60%)

    public static AxisVerdict DecideAxisImpairment(TesResult r,
        int Tmin = -1, int Amin = -1, float Pdom = -1f)
    {
        if (Tmin < 0) Tmin = Default_Tmin;
        if (Amin < 0) Amin = Default_Amin;
        if (Pdom < 0f) Pdom = Default_Pdom;

        if (r == null) return AxisVerdict.Inconclusive;

        int tes = r.TotalTES;
        int rg = r.TPES_RG;
        int by = r.TPES_BY;
        int sumAxes = rg + by;

        // No errors: nothing to report
        if (tes == 0 || sumAxes == 0) return AxisVerdict.None;

        // If TES is below threshold, it is probably noise / distraction
        if (tes < Tmin) return AxisVerdict.None;

        float pctRG = (100f * rg) / Math.Max(1, sumAxes); // percent
        float pctBY = (100f * by) / Math.Max(1, sumAxes);

        // absolute verification on the dominant axis + relative dominance
        if (rg >= Amin && pctRG >= (Pdom * 100f)) return AxisVerdict.Probable_RG;
        if (by >= Amin && pctBY >= (Pdom * 100f)) return AxisVerdict.Probable_BY;

        // Other cases: there are errors but not strong enough or non-dominant
        return AxisVerdict.Inconclusive;
    }

    public static string VerdictToMessage(AxisVerdict v)
    {
        switch (v)
        {
            case AxisVerdict.None:
                return "Nessun problema evidente: punteggio troppo basso per segnalare un deficit";
            case AxisVerdict.Probable_RG:
                return "Indicazione: possibile deficit sull'asse Rosso�Verde (protan/deutan)";
            case AxisVerdict.Probable_BY:
                return "Indicazione: possibile deficit sull'asse Blu�Giallo (tritan-like)";
            case AxisVerdict.Inconclusive:
            default:
                return "errori presenti ma non � possibile classificare un asse.";
        }
    }
}


