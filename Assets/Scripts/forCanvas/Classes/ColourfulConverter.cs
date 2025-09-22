using Colourful;
using System.Drawing;
using UnityEngine;

public class ColourfulConverter
{
    private static ColourfulConverter instance;

    public static ColourfulConverter Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new ColourfulConverter();
            }

            return instance;
        }
    }

    public LabColor ConvertRGBtoLab(Color32 color32)
    {
        var rgb = new RGBColor(color32.r / 255.0, color32.g / 255.0, color32.b / 255.0);

        var converter = new ConverterBuilder()
            .FromRGB(RGBWorkingSpaces.sRGB)
            .ToLab(Illuminants.D65)
            .Build();

        LabColor lab = converter.Convert(rgb);

        return lab;
    }

    public LChabColor ConvertLabToLCh(LabColor lab)
    {
        var converter = new ConverterBuilder()
            .FromLab(Illuminants.D65)
            .ToLChab(Illuminants.D65)
            .Build();

        LChabColor lch = converter.Convert(lab);

        return lch;
    }


}
