using Colourful;
using UnityEngine;

/// <summary>
/// Classe di utilità statica per conversioni tra spazi colore.
/// MODIFICATA: Ottimizzata per gestire direttamente lo spazio Lineare di Unity ed evitare
/// la doppia conversione (Lineare->Gamma->Lineare) che causa perdita di precisione.
/// </summary>
public static class ColourfulConverter
{
    // --- Convertitori Colore Predefiniti ---

    // 1. Pipeline Standard (sRGB Input)
    private static readonly IColorConverter<RGBColor, LinearRGBColor> rgbToLinearRGBConverter =
        new ConverterBuilder()
            .FromRGB(RGBWorkingSpaces.sRGB)
            .ToLinearRGB()
            .Build();

    // 2. Pipeline diretta (RGB <-> LMS)
    private static readonly IColorConverter<LinearRGBColor, XYZColor> linearRGBToXYZConverter =
        new ConverterBuilder()
            .FromLinearRGB()
            .ToXYZ(Illuminants.D65)
            .Build();

    private static readonly IColorConverter<XYZColor, LMSColor> xyzToLMSConverter =
        new ConverterBuilder()
            .FromXYZ(Illuminants.D65)
            .ToLMS(Illuminants.D65)
            .Build();

    // 3. Pipeline Inversa (LMS -> RGB)
    private static readonly IColorConverter<LMSColor, XYZColor> lmsToXYZConverter =
        new ConverterBuilder()
            .FromLMS(Illuminants.D65)
            .ToXYZ(Illuminants.D65)
            .Build();

    private static readonly IColorConverter<XYZColor, LinearRGBColor> xyzToLinearRGBConverter =
        new ConverterBuilder()
            .FromXYZ(Illuminants.D65)
            .ToLinearRGB()
            .Build();

    private static readonly IColorConverter<LinearRGBColor, RGBColor> linearRGBToRgbConverter =
            new ConverterBuilder()
                .FromLinearRGB()
                .ToRGB(RGBWorkingSpaces.sRGB)
                .Build();

    // 4. Lab Converters
    private static readonly IColorConverter<RGBColor, LabColor> rgbToLabConverter =
       new ConverterBuilder()
           .FromRGB(RGBWorkingSpaces.sRGB)
           .ToLab(Illuminants.D65)
           .Build();

    private static readonly IColorConverter<LabColor, LChabColor> labToLChabConverter =
       new ConverterBuilder()
           .FromLab(Illuminants.D65)
           .ToLChab(Illuminants.D65)
           .Build();

    // --- METODI OTTIMIZZATI PER UNITY (Lineare/Gamma Agnostic) ---

    /// <summary>
    /// Converte direttamente un UnityEngine.Color in LMSColor nel modo più preciso possibile.
    /// Se Unity è in Linear Space, salta la conversione sRGB per evitare perdita di dati.
    /// </summary>
    public static LMSColor UnityToLms(Color unityColor)
    {
        // Caso 1: Progetto in Linear Space (VR/Pico 4)
        // I valori (r,g,b) sono già fisicamente lineari. Li passiamo direttamente a LinearRGBColor.
        if (QualitySettings.activeColorSpace == ColorSpace.Linear)
        {
            var linearColor = new LinearRGBColor(unityColor.r, unityColor.g, unityColor.b);
            var xyzColor = linearRGBToXYZConverter.Convert(linearColor);
            return xyzToLMSConverter.Convert(xyzColor);
        }
        // Caso 2: Progetto in Gamma Space (Monitor Standard/Legacy)
        // I valori sono sRGB. Usiamo la pipeline standard.
        else
        {
            var srgbColor = new RGBColor(unityColor.r, unityColor.g, unityColor.b);
            var linearColor = rgbToLinearRGBConverter.Convert(srgbColor);
            var xyzColor = linearRGBToXYZConverter.Convert(linearColor);
            return xyzToLMSConverter.Convert(xyzColor);
        }
    }

    /// <summary>
    /// Converte un LMSColor in UnityEngine.Color, rispettando lo spazio colore attivo.
    /// </summary>
    public static Color LmsToUnity(LMSColor lmsColor, float alpha = 1.0f)
    {
        // 1. LMS -> XYZ
        var xyzColor = lmsToXYZConverter.Convert(lmsColor);

        // 2. XYZ -> Linear RGB (Valori fisici della luce)
        var linearRgb = xyzToLinearRGBConverter.Convert(xyzColor);

        // Caso 1: Progetto in Linear Space (VR/Pico 4)
        // Possiamo usare direttamente i valori lineari.
        if (QualitySettings.activeColorSpace == ColorSpace.Linear)
        {
            // Clamping manuale per sicurezza, ma manteniamo i dati lineari
            float r = Mathf.Clamp01((float)linearRgb.R);
            float g = Mathf.Clamp01((float)linearRgb.G);
            float b = Mathf.Clamp01((float)linearRgb.B);
            return new Color(r, g, b, alpha);
        }
        // Caso 2: Progetto in Gamma Space
        // Dobbiamo convertire in sRGB prima di visualizzare.
        else
        {
            var srgb = linearRGBToRgbConverter.Convert(linearRgb);
            float r = Mathf.Clamp01((float)srgb.R);
            float g = Mathf.Clamp01((float)srgb.G);
            float b = Mathf.Clamp01((float)srgb.B);
            return new Color(r, g, b, alpha);
        }
    }

    // --- Metodi Legacy / Helper ---

    public static RGBColor UnityColorToRgbColor(Color unityColor)
    {
        Color colorInSRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear) ? unityColor.gamma : unityColor;
        return new RGBColor((double)colorInSRGB.r, (double)colorInSRGB.g, (double)colorInSRGB.b);
    }

    public static Color RgbColorToUnityColor(RGBColor rgbColor, float alpha)
    {
        float r = Mathf.Clamp01((float)rgbColor.R);
        float g = Mathf.Clamp01((float)rgbColor.G);
        float b = Mathf.Clamp01((float)rgbColor.B);
        Color colorInSRGB = new Color(r, g, b, alpha);
        return (QualitySettings.activeColorSpace == ColorSpace.Linear) ? colorInSRGB.linear : colorInSRGB;
    }

    public static LabColor ConvertUnityColorToLab(Color unityColor)
    {
        // Per Lab si passa solitamente da sRGB standard, quindi usiamo il metodo legacy
        RGBColor rgbColor = UnityColorToRgbColor(unityColor);
        return rgbToLabConverter.Convert(rgbColor);
    }

    public static LChabColor ConvertLabToLChab(LabColor lab)
    {
        return labToLChabConverter.Convert(lab);
    }

    public static Vector3 LmsToVector3(LMSColor lms)
    {
        return new Vector3((float)lms.L, (float)lms.M, (float)lms.S);
    }

    public static LMSColor Vector3ToLmsColor(Vector3 vec)
    {
        return new LMSColor((double)vec.x, (double)vec.y, (double)vec.z);
    }
}