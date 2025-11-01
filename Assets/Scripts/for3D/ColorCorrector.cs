using System.Collections.Generic;
using Colourful; // Necessario per LMSColor, LabColor etc.
using Colourful.Spaces; // Necessario per RGBColor
using UnityEngine;

/// <summary>
/// Una classe di utilità per simulare e correggere i colori
/// per gli utenti con dicromia (mancanza di un tipo di cono),
/// basata su trasformazioni nello spazio colore LMS.
/// </summary>
public static class ColorCorrector
{
    public enum DichromacyType
    {
        Protanopia, // Assenza coni L (Rosso)
        Deuteranopia, // Assenza coni M (Verde)
        Tritanopia  // Assenza coni S (Blu)
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
    /// Simula come un colore viene percepito da un utente con una specifica dicromia.
    /// Segue la pipeline: UnityColor -> sRGB -> Lineare RGB -> XYZ -> LMS -> Simula -> LMS -> XYZ -> Lineare RGB -> sRGB -> UnityColor
    /// </summary>
    /// <param name="originalColor">Il colore originale (UnityEngine.Color).</param>
    /// <param name="type">Il tipo di dicromia da simulare.</param>
    /// <returns>Il colore simulato (UnityEngine.Color).</returns>
    public static Color SimulateDichromacy(Color originalColor, DichromacyType type)
    {
        // Converte da Unity Color a Colourful RGBColor (sRGB)
        RGBColor rgbColor = ColourfulConverter.UnityColorToRgbColor(originalColor);

        // Converte in LMS
        LMSColor lmsColor = ColourfulConverter.ConvertRgbToLms(rgbColor);

        // Applica la matrice di simulazione LMS
        double[,] simMatrix = GetSimulationMatrixLMS(type);
        LMSColor simulatedLms = ApplyLmsMatrix(lmsColor, simMatrix);

        // Riconverte in RGB (sRGB)
        RGBColor simulatedRgb = ColourfulConverter.ConvertLmsToRgb(simulatedLms);

        // Converte di nuovo in Unity Color
        return ColourfulConverter.RgbColorToUnityColor(simulatedRgb, originalColor.a);
    }

    /// <summary>
    /// Corregge un colore per renderlo più distinguibile per un utente con dicromia.
    /// Implementa l'algoritmo di Fidaner et al. (proiezione errore su asse percepibile).
    /// </summary>
    public static Color CorrectColor(Color originalColor, DichromacyType type)
    {
        // 0. Converti originale UnityColor in RGBColor (sRGB)
        RGBColor originalRgb = ColourfulConverter.UnityColorToRgbColor(originalColor);

        // 1. Converti originale in LMS
        LMSColor originalLms = ColourfulConverter.ConvertRgbToLms(originalRgb);

        // 2. Simula come appare il colore in LMS
        double[,] simMatrix = GetSimulationMatrixLMS(type);
        LMSColor simulatedLms = ApplyLmsMatrix(originalLms, simMatrix);

        // 3. Calcola l'errore nello spazio LMS (usando Vector3 per facilità di calcolo)
        Vector3 originalLmsVec = ColourfulConverter.LmsToVector3(originalLms);
        Vector3 simulatedLmsVec = ColourfulConverter.LmsToVector3(simulatedLms);
        Vector3 errorLms = originalLmsVec - simulatedLmsVec;

        // 4. Applica la correzione (Metodo Fidaner et al.)
        Vector3 correctedLmsVec = ApplyFidanerCorrection(originalLmsVec, errorLms, type);
        LMSColor correctedLms = ColourfulConverter.Vector3ToLmsColor(correctedLmsVec);

        // 5. Riconverti il colore corretto LMS in sRGB
        RGBColor correctedRgb = ColourfulConverter.ConvertLmsToRgb(correctedLms);

        // 6. Converte sRGB in Unity Color (gestendo spazio lineare/gamma)
        return ColourfulConverter.RgbColorToUnityColor(correctedRgb, originalColor.a);
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
    public static Dictionary<string, Color> GetNewTileColorDic(Dictionary<string, Color> originalTileColorDic, DichromacyType type)
    {
        Dictionary<string, Color> newTileColorDic = new Dictionary<string, Color>();
        foreach (var kvp in originalTileColorDic)
        {
            Color correctedColor = CorrectColor(kvp.Value, type);
            newTileColorDic[kvp.Key] = correctedColor;
        }
        return newTileColorDic;
    }


    // --- Funzioni Private Helper ---

    /// <summary>
    /// Restituisce la matrice di simulazione LMS corretta per il tipo di dicromia.
    /// </summary>
    private static double[,] GetSimulationMatrixLMS(DichromacyType type)
    {
        switch (type)
        {
            case DichromacyType.Protanopia: return ProtanopiaLMSMatrix;
            case DichromacyType.Deuteranopia: return DeuteranopiaLMSMatrix;
            case DichromacyType.Tritanopia: return TritanopiaLMSMatrix;
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
    /// Applica la correzione di Fidaner et al.
    /// Proietta l'errore sulla linea di confusione e lo sposta sull'asse di luminanza (L).
    /// Per Tritanopia, lo sposta sull'asse M (come suggerito da alcune varianti).
    /// </summary>
    private static Vector3 ApplyFidanerCorrection(Vector3 originalLms, Vector3 errorLms, DichromacyType type)
    {
        // Assi di proiezione dell'errore (approssimati per semplicità)
        // Questi definiscono la direzione in cui l'informazione viene persa
        Vector3 confusionAxis = Vector3.zero;
        Vector3 shiftAxis = Vector3.zero; // Asse su cui spostare l'errore

        switch (type)
        {
            case DichromacyType.Protanopia:
                confusionAxis = new Vector3(0.0f, 1.0f, -1.0f).normalized; // Asse M-S approssimato
                shiftAxis = new Vector3(1.0f, 0.0f, 0.0f); // Asse L
                break;
            case DichromacyType.Deuteranopia:
                confusionAxis = new Vector3(1.0f, 0.0f, -1.0f).normalized; // Asse L-S approssimato
                shiftAxis = new Vector3(1.0f, 0.0f, 0.0f); // Asse L
                break;
            case DichromacyType.Tritanopia:
                confusionAxis = new Vector3(1.0f, -1.0f, 0.0f).normalized; // Asse L-M approssimato
                shiftAxis = new Vector3(0.0f, 1.0f, 0.0f); // Asse M
                break;
        }

        float projectionMagnitude = Vector3.Dot(errorLms, confusionAxis);
        // Vector3 projectedError = projectionMagnitude * confusionAxis; // Non usato nella versione semplificata
        Vector3 shiftedError = projectionMagnitude * shiftAxis;

        // Aggiunge l'errore spostato al colore originale LMS
        Vector3 correctedLms = originalLms + shiftedError; // Semplificato: aggiunge solo lo shift

        // Evita valori negativi per semplicità (potrebbe richiedere clamping/gamut mapping migliore)
        correctedLms.x = Mathf.Max(0, correctedLms.x);
        correctedLms.y = Mathf.Max(0, correctedLms.y);
        correctedLms.z = Mathf.Max(0, correctedLms.z);

        return correctedLms;
    }
}