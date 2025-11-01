using Colourful;
using UnityEngine;

/// <summary>
/// Classe di utilità statica per conversioni tra spazi colore
/// usando la libreria Colourful e gestendo UnityEngine.Color.
/// </summary>
public static class ColourfulConverter
{
    // --- Convertitori Colore Predefiniti ---
    // Usano la libreria Colourful per le conversioni sRGB <-> Lineare <-> XYZ <-> LMS <-> Lab
    private static readonly IColorConverter<RGBColor, LinearRGBColor> rgbToLinearRGBConverter =
        new ConverterBuilder()
            .FromRGB(RGBWorkingSpaces.sRGB)
            .ToLinearRGB()
            .Build();

    private static readonly IColorConverter<LinearRGBColor, XYZColor> linearRGBToXYZConverter =
        new ConverterBuilder()
            .FromLinearRGB()
            .ToXYZ(Illuminants.D65)
            .Build();

    private static readonly IColorConverter<XYZColor, LMSColor> xyzToLMSConverter =
        new ConverterBuilder()
            .FromXYZ(Illuminants.D65)
            .ToLMS(LMSTransformationMatrix.Bradford) // XYZ -> LMS
            .Build();

    private static readonly IColorConverter<LMSColor, RGBColor> lmsToRgbConverter =
        new ConverterBuilder()
            .FromLMS(LMSTransformationMatrix.Bradford) // LMS -> XYZ
            .ToXYZ(Illuminants.D65)
            .ToLinearRGB()                   // XYZ -> Lineare RGB
            .ToRGB(RGBWorkingSpaces.sRGB)    // Lineare RGB -> sRGB
            .Build();

    private static readonly IColorConverter<RGBColor, LabColor> rgbToLabConverter =
       new ConverterBuilder()
           .FromRGB(RGBWorkingSpaces.sRGB) // Assume input sRGB
           .ToLab(Illuminants.D65)          // sRGB -> Lab
           .Build();

    private static readonly IColorConverter<LabColor, LChabColor> labToLChabConverter =
       new ConverterBuilder()
           .FromLab(Illuminants.D65)
           .ToLChab(Illuminants.D65)
           .Build();

    // --- Conversioni Principali ---

    /// <summary>
    /// Converte un UnityEngine.Color (sRGB o Lineare a seconda delle impostazioni Unity)
    /// in un Colourful.RGBColor (sRGB).
    /// </summary>
    public static RGBColor UnityColorToRgbColor(Color unityColor)
    {
        // Gestisce lo spazio colore lineare di Unity
        Color colorInSRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear) ? unityColor.gamma : unityColor;
        return new RGBColor((double)colorInSRGB.r, (double)colorInSRGB.g, (double)colorInSRGB.b);
    }

    /// <summary>
    /// Converte un Colourful.RGBColor (sRGB) in un UnityEngine.Color,
    /// tenendo conto dello spazio colore attivo in Unity (Lineare o Gamma).
    /// </summary>
    public static Color RgbColorToUnityColor(RGBColor rgbColor, float alpha)
    {
        // Clampa i valori RGB a [0, 1] prima di creare il colore Unity
        float r = Mathf.Clamp01((float)rgbColor.R);
        float g = Mathf.Clamp01((float)rgbColor.G);
        float b = Mathf.Clamp01((float)rgbColor.B);
        Color colorInSRGB = new Color(r, g, b, alpha);

        // Se Unity è in lineare, riconverti
        return (QualitySettings.activeColorSpace == ColorSpace.Linear) ? colorInSRGB.linear : colorInSRGB;
    }

    /// <summary>
    /// Converte un Colourful.RGBColor (sRGB) in Colourful.LMSColor.
    /// </summary>
    public static LMSColor ConvertRgbToLms(RGBColor rgbColor)
    {
        return rgbToLmsConverter.Convert(rgbColor);
    }

    /// <summary>
    /// Converte un Colourful.LMSColor in Colourful.RGBColor (sRGB).
    /// </summary>
    public static RGBColor ConvertLmsToRgb(LMSColor lmsColor)
    {
        return lmsToRgbConverter.Convert(lmsColor);
    }

    /// <summary>
    /// Converte un UnityEngine.Color (gestendo lo spazio colore) in Colourful.LabColor.
    /// </summary>
    public static LabColor ConvertUnityColorToLab(Color unityColor)
    {
        RGBColor rgbColor = UnityColorToRgbColor(unityColor);
        return rgbToLabConverter.Convert(rgbColor);
    }

    /// <summary>
    /// Converte un Colourful.LabColor in Colourful.LChabColor.
    /// </summary>
    public static LChabColor ConvertLabToLChab(LabColor lab)
    {
        return labToLChabConverter.Convert(lab);
    }

    // --- Utility Vector3 <-> LMSColor ---

    /// <summary>
    /// Converte un Colourful.LMSColor in UnityEngine.Vector3.
    /// </summary>
    public static Vector3 LmsToVector3(LMSColor lms)
    {
        return new Vector3((float)lms.L, (float)lms.M, (float)lms.S);
    }

    /// <summary>
    /// Converte un UnityEngine.Vector3 in Colourful.LMSColor.
    /// </summary>
    public static LMSColor Vector3ToLmsColor(Vector3 vec)
    {
        return new LMSColor((double)vec.x, (double)vec.y, (double)vec.z);
    }
}