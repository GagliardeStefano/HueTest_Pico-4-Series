using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// --- MODIFICA 1: Aggiornato enum con verdetti specifici ---
public enum AxisVerdict
{
    None,
    Probable_RG,
    Probable_BY,
    Inconclusive,
    Protanopia,     // Verdetto raffinato
    Deuteranopia,   // Verdetto raffinato
}

public static class ColorTestEvaluator
{
    // Valori di default (puoi lasciarli o rimuoverli se non li usi più)
    public static int Default_Tmin = 16;   // Soglia minima TES
    public static int Default_Amin = 8;    // Soglia minima TPES sull'asse dominante
    public static float Default_Pdom = 0.55f; // Dominanza percentuale (55%)

    /// <summary>
    /// Decide l'asse di deficit primario (Logica di primo livello)
    /// </summary>
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

        // Nessun errore: niente da segnalare
        if (tes == 0 || sumAxes == 0) return AxisVerdict.None;

        // Se il TES è sotto la soglia, è probabile sia solo rumore
        if (tes < Tmin) return AxisVerdict.None;

        float pctRG = (100f * rg) / Math.Max(1, sumAxes);
        float pctBY = (100f * by) / Math.Max(1, sumAxes);

        // Verifica assoluta sull'asse dominante + dominanza relativa
        if (rg >= Amin && pctRG >= (Pdom * 100f)) return AxisVerdict.Probable_RG;
        if (by >= Amin && pctBY >= (Pdom * 100f)) return AxisVerdict.Probable_BY;

        // Altri casi: ci sono errori ma non abbastanza forti o non dominanti
        return AxisVerdict.Inconclusive;
    }

    // --- NUOVA FUNZIONE ---
    /// <summary>
    /// Raffina un verdetto 'Probable_RG' in Protanopia o Deuteranopia
    /// analizzando il "centro di massa" degli assi di confusione.
    /// </summary>
    public static AxisVerdict DistinguishRGDeficiency(TesResult result)
    {
        // Filtra solo gli errori significativi sull'asse RG
        var rgErrors = result.TileErrors
            .Where(e => e.Axis == "RG" && e.Err > 0)
            .ToList();

        // Se non ci sono abbastanza errori, non possiamo distinguere
        if (rgErrors.Count < 1)
        {
            return AxisVerdict.Probable_RG; // Restituisce il verdetto generico
        }

        // Calcoliamo il "centro di massa" degli errori di tonalità (HueDeg)
        // Usiamo un metodo vettoriale per gestire la natura circolare (0-360 gradi)
        double x_total = 0;
        double y_total = 0;
        double totalErrorWeight = 0;

        foreach (var error in rgErrors)
        {
            // Converte l'angolo di tonalità (HueDeg) in radianti
            double angleRad = error.HueDeg * Mathf.Deg2Rad;
            // Pesa l'errore per la sua gravità (Err)
            double weight = error.Err;

            x_total += Mathf.Cos((float)angleRad) * weight;
            y_total += Mathf.Sin((float)angleRad) * weight;
            totalErrorWeight += weight;
        }

        if (totalErrorWeight == 0) return AxisVerdict.Probable_RG;

        // Calcola l'angolo medio (il centro di confusione)
        double meanAngleRad = Mathf.Atan2((float)y_total, (float)x_total);
        double meanAngleDeg = meanAngleRad * Mathf.Rad2Deg;

        // Normalizza l'angolo a 0-360 gradi
        if (meanAngleDeg < 0)
        {
            meanAngleDeg += 360;
        }

        // --- Logica di Decisione ---
        // Nello spazio colore LCh, gli assi di confusione classici sono:
        // Asse Protan: confusione tra ~100° (giallo-verde) e il suo opposto ~280° (blu-viola).
        // Asse Deutan: confusione tra ~150° (verde) e il suo opposto ~330° (magenta).

        const float protanAxis1 = 100f;
        const float protanAxis2 = 280f;
        const float deutanAxis1 = 150f;
        const float deutanAxis2 = 330f;

        // Calcola la distanza angolare minima dall'asse Protan
        float distProtan1 = Mathf.Abs(Mathf.DeltaAngle((float)meanAngleDeg, protanAxis1));
        float distProtan2 = Mathf.Abs(Mathf.DeltaAngle((float)meanAngleDeg, protanAxis2));
        float minDistProtan = Mathf.Min(distProtan1, distProtan2);

        // Calcola la distanza angolare minima dall'asse Deutan
        float distDeutan1 = Mathf.Abs(Mathf.DeltaAngle((float)meanAngleDeg, deutanAxis1));
        float distDeutan2 = Mathf.Abs(Mathf.DeltaAngle((float)meanAngleDeg, deutanAxis2));
        float minDistDeutan = Mathf.Min(distDeutan1, distDeutan2);

        Debug.Log($"[DistinguishRG] Angolo medio confusione: {meanAngleDeg:F1}°. Distanza Protan: {minDistProtan:F1}°. Distanza Deutan: {minDistDeutan:F1}°.");

        // Il verdetto è il tipo di asse a cui il centro degli errori è più vicino.
        if (minDistProtan < minDistDeutan)
        {
            return AxisVerdict.Protanopia;
        }
        else
        {
            return AxisVerdict.Deuteranopia;
        }
    }


    // --- MODIFICA 2: Aggiornato VerdictToMessage ---
    /// <summary>
    /// Converte l'enum del verdetto in un messaggio leggibile.
    /// </summary>
    public static string VerdictToMessage(AxisVerdict v)
    {
        switch (v)
        {
            case AxisVerdict.None:
                return "Nessun problema evidente: punteggio troppo basso per segnalare un deficit.";

            case AxisVerdict.Probable_RG:
                // Questo ora serve come fallback se la distinzione non è chiara
                return "Indicazione: possibile deficit sull'asse Rosso-Verde (Protan/Deutan).";

            case AxisVerdict.Probable_BY:
                return "Indicazione: possibile deficit sull'asse Blu-Giallo (Tritan).";

            case AxisVerdict.Inconclusive:
                return "Inconcludente: sono presenti errori ma non sono concentrati su un asse specifico.";

            // Nuovi messaggi per i verdetti raffinati
            case AxisVerdict.Protanopia:
                return "Deficit rilevato: tipo Protan (asse Rosso-Verde).";

            case AxisVerdict.Deuteranopia:
                return "Deficit rilevato: tipo Deutan (asse Rosso-Verde).";

            default:
                return "Risultato non determinato.";
        }
    }
}