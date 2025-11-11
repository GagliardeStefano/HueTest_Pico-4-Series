using System.Collections.Generic;
using Colourful; // Necessario per LMSColor, LabColor etc.
using UnityEngine;

/// <summary>
/// Una classe di utilità per simulare e correggere i colori
/// per gli utenti con dicromia (mancanza di un tipo di cono),
/// basata su trasformazioni nello spazio colore LMS.
/// </summary>
public static class ColorCorrector
{
    public enum AnomalyType
    {
        Protanopia, // Assenza coni L (Rosso)
        Deuteranopia, // Assenza coni M (Verde)
        Tritanopia,  // Assenza coni S (Blu)
        Normal // Nessuna dicromia
    }

    // --- MATRICI DI SIMULAZIONE LMS (Viénot, Brettel and Mollon) ---
    // Operano direttamente nello spazio LMS
    private static readonly double[,] ProtanopiaLMSMatrix = {
        {0, 2.02344, -2.52581},
        {0, 1, 0},
        {0, 0, 1}
    };

    private static readonly double[,] DeuteranopiaLMSMatrix = {
        {1, 0, 0},
        {0.494207, 0, 1.24827},
        {0, 0, 1}
    };

    private static readonly double[,] TritanopiaLMSMatrix = {
        {1, 0, 0},
        {0, 1, 0},
        {-0.395913, 0.801109, 0}
    };

    // --- Funzioni Pubbliche ---

    /// <summary>
    /// Corregge un colore per renderlo più distinguibile per un utente con dicromia.
    /// Implementa l'algoritmo di Fidaner et al. (proiezione errore su asse percepibile).
    /// </summary>
    public static Color CorrectColor(Color originalColor, AnomalyType type)
    {
        if(type == AnomalyType.Normal)
        {
            return originalColor; // Nessuna correzione necessaria
        }
        LMSColor originalLms = ColourfulConverter.ConvertRgbToLms(ColourfulConverter.UnityColorToRgbColor(originalColor));
        //simula come il colore viene visto da un daltonico
        LMSColor simulatedLms = SimulateDichromacy(originalColor, type);

        //Calcola l'errore nello spazio LMS (usando Vector3 per facilità di calcolo)
        Vector3 originalLmsVec = ColourfulConverter.LmsToVector3(originalLms);
        Vector3 simulatedLmsVec = ColourfulConverter.LmsToVector3(simulatedLms);
        Vector3 errorLms = originalLmsVec - simulatedLmsVec;

        // 4. Applica la correzione (Metodo Fidaner)
        Vector3 correctedLmsVec = ApplyFidanerCorrection(originalLmsVec, errorLms, type);
        LMSColor correctedLms = ColourfulConverter.Vector3ToLmsColor(correctedLmsVec);

        // 5. Riconverti il colore corretto LMS in sRGB
        RGBColor correctedRgb = ColourfulConverter.ConvertLmsToRgb(correctedLms);

        // 6. Converte sRGB in Unity Color (gestendo spazio lineare/gamma)
        return ColourfulConverter.RgbColorToUnityColor(correctedRgb, originalColor.a);
    }
    /// <summary>
    /// Simula come un colore viene percepito da un utente con una specifica dicromia.
    /// Segue la pipeline: UnityColor -> sRGB -> Lineare RGB -> XYZ -> LMS -> Simula -> LMS -> XYZ -> Lineare RGB -> sRGB -> UnityColor
    /// </summary>
    /// <param name="originalColor">Il colore originale (UnityEngine.Color).</param>
    /// <param name="type">Il tipo di dicromia da simulare.</param>
    /// <returns>Il colore simulato (UnityEngine.Color).</returns>
    public static LMSColor SimulateDichromacy(Color originalColor, AnomalyType type)
    {
        // Converte da Unity Color a Colourful RGBColor (sRGB)
        RGBColor rgbColor = ColourfulConverter.UnityColorToRgbColor(originalColor);

        // Converte in LMS
        LMSColor lmsColor = ColourfulConverter.ConvertRgbToLms(rgbColor);

        // Applica la matrice di simulazione LMS
        double[,] simMatrix = GetSimulationMatrixLMS(type);
        LMSColor simulatedLms = ApplyLmsMatrix(lmsColor, simMatrix);

        return simulatedLms;
    }

    /// <summary>
    /// Calcola la distanza percettiva tra due colori usando la formula Delta E CIE76 nello spazio colore CIELAB.
    /// </summary>
    public static float CalculatePerceptualDistance(Color c1, Color c2)
    {
        // Usa ColourfulConverter per la conversione
        LabColor lab1 = ColourfulConverter.ConvertUnityColorToLab(c1);
        LabColor lab2 = ColourfulConverter.ConvertUnityColorToLab(c2);

        float deltaL = (float)(lab1.L - lab2.L);
        float deltaA = (float)(lab1.a - lab2.a);
        float deltaB = (float)(lab1.b - lab2.b);

        return Mathf.Sqrt(deltaL * deltaL + deltaA * deltaA + deltaB * deltaB);
    }

    /// <summary>
    /// Genera un nuovo dizionario di colori corretti per il tipo di daltonismo specificato.
    /// </summary>
    public static Dictionary<string, Color> GetNewTileColorDic(Dictionary<string, Color> originalTileColorDic, AnomalyType type)
    {
        Dictionary<string, Color> newTileColorDic = new Dictionary<string, Color>();
        foreach (var color in originalTileColorDic)
        {
            Color correctedColor = CorrectColor(color.Value, type);
            newTileColorDic[color.Key] = correctedColor;
        }
        return newTileColorDic;
    }


    // --- Funzioni Private Helper ---

    /// <summary>
    /// Restituisce la matrice di simulazione LMS corretta per il tipo di dicromia.
    /// </summary>
    private static double[,] GetSimulationMatrixLMS(AnomalyType type)
    {
        switch (type)
        {
            case AnomalyType.Protanopia: return ProtanopiaLMSMatrix;
            case AnomalyType.Deuteranopia: return DeuteranopiaLMSMatrix;
            case AnomalyType.Tritanopia: return TritanopiaLMSMatrix;
            default:
                // Matrice identità se il tipo non è riconosciuto
                return new double[,] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };
        }
    }

    /// <summary>
    /// Applica una matrice 3x3 a un vettore LMSColor.
    /// </summary>
    private static LMSColor ApplyLmsMatrix(LMSColor color, double[,] matrix)
    {
        double l = color.L;
        double m = color.M;
        double s = color.S;

        double newL = matrix[0, 0] * l + matrix[0, 1] * m + matrix[0, 2] * s;
        double newM = matrix[1, 0] * l + matrix[1, 1] * m + matrix[1, 2] * s;
        double newS = matrix[2, 0] * l + matrix[2, 1] * m + matrix[2, 2] * s;

        return new LMSColor(newL, newM, newS);
    }

    /// <summary>
    /// Applica la correzione di Fidaner et al
    /// per Protanopia/Deuteranopia, implementa la logica di spostamento dell'errore sull'asse S (crominanza blu-giallo)
    /// Per Tritanopia, applica la logica inversa (sposta errore S su asse L-M).
    /// </summary>
    private static Vector3 ApplyFidanerCorrection(Vector3 originalLms, Vector3 errorLms, AnomalyType type)
    {
        // Fattore di correzione. 1.0 è comune, ma può essere sintonizzato (es. 0.7-1.0)
        float correctionFactor = 1.0f;

        Vector3 correctedLms;

        switch (type)
        {
            case AnomalyType.Protanopia:
            case AnomalyType.Deuteranopia:
                // Sposta l'errore L-M (errorLms.x - errorLms.y) sull'asse S.
                // S_corretto = S_originale + (Errore_L - Errore_M) * Fattore

                float errorLM = errorLms.x - errorLms.y;

                correctedLms.x = originalLms.x; // L originale
                correctedLms.y = originalLms.y; // M originale
                correctedLms.z = originalLms.z + (errorLM * correctionFactor); // S corretto

                break;

            case AnomalyType.Tritanopia:
                // il deficit è sull'asse S, quindi l'errore è errorLms.z.
                // Spostiamo questo errore sull'asse L-M (rosso-verde) per renderlo visibile.

                float errorS = errorLms.z;

                // Aggiungiamo l'errore a L (più rosso) e sottraiamo da M (meno verde)
                // per creare una distinzione cromatica sull'asse L-M.
                // Il fattore 0.7 attenua la correzione per evitare colori troppo innaturali.
                float shiftAmount = errorS * correctionFactor * 0.7f;

                correctedLms.x = originalLms.x + shiftAmount;
                correctedLms.y = originalLms.y - shiftAmount;
                correctedLms.z = originalLms.z; // Canale S originale
                break;

            default:
                correctedLms = originalLms;
                break;
        }

        // Evita valori negativi (clamping)
        correctedLms.x = Mathf.Max(0, correctedLms.x);
        correctedLms.y = Mathf.Max(0, correctedLms.y);
        correctedLms.z = Mathf.Max(0, correctedLms.z);

        return correctedLms;
    }
}