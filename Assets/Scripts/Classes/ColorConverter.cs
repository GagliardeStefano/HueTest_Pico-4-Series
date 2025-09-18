using Colourful;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorConverter
{
    public static ColorConverter instance;

    public static ColorConverter Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new ColorConverter();
            }

            return instance;
        }
    }

    public string AssignAxis(Color32 color32)
    {
        LabColor lab = ColourfulConverter.Instance.ConvertRGBtoLab(color32);

        string info = GetInfoAxis(lab);
        Debug.Log($"RGB: {color32} -> Lab:{lab}; ## {info}");

        if (Math.Abs(lab.a) >= Math.Abs(lab.b))
            return "RG";
        else
            return "BY";
    }

    public static void GetHueChroma(Color32 color32, out double hue, out double chroma)
    {
        LabColor lab = ColourfulConverter.Instance.ConvertRGBtoLab(color32);
        LChabColor lch = ColourfulConverter.Instance.ConvertLabToLCh(lab);

        Debug.Log($"Lab: {lab} -> LCh:{lch}");

        hue = lch.h;
        chroma = lch.C;
    }

    private string GetInfoAxis(LabColor lab)
    {
        if (Math.Abs(lab.a) >= Math.Abs(lab.b))
        {
            // Axis R-G
            if (lab.a > 0)
                return "RG (verso Rosso)";
            else
                return "RG (verso Verde)";
        }
        else
        {
            // Axis B-Y
            if (lab.b > 0)
                return "BY (verso Giallo)";
            else
                return "BY (verso Blu)";
        }
    }
}
