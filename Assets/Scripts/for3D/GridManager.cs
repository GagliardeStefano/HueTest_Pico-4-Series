using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GridManager : MonoBehaviour
{
    [Header("Configurazione Dati")]
    public TextAsset colorCsv; // Trascina qui il file Farnsworth_Unity_Colors.csv

    [Header("Configurazione Righe Standard")]
    // Definisce la lunghezza delle righe: { Riga1, Riga2, Riga3, Riga4 }
    // Totale 43 colori: 11 + 10 + 11 + 11
    public int[] standardRowLengths = new int[] { 11, 10, 11, 11 };

    [Header("Prefab")]
    public GameObject cubePrefab;

    [Header("Dimensioni griglia")]
    public float spacing = 1.25f;
    public float yOffset = 0.01f;
    public float cubeSize = 0.2f;
    public float thickness = 0.01f;

    [Header("Outline Settings")]
    public float outlineWidth = 3f;

    private List<List<GameObject>> tilesByRow;
    public TextMeshProUGUI TextProgressPage;
    public TextMeshProUGUI TextTitle;

    private string titleTest = "\r\nGUIDA: Disponi i colori in base alla tonalità in ogni riga. Il primo e l'ultimo colore sono fissi.\r\nClicca su \"VERIFICA\" per vedere il risultato.\r\n";

    // Dizionario per accesso tramite chiave "Riga-Colonna"
    private Dictionary<string, Color> tileColorDict = new Dictionary<string, Color>();
    // Lista lineare ordinata per la generazione (conterrà le chiavi nell'ordine di creazione della griglia)
    private List<string> _linearKeys = new List<string>();

    private Dictionary<string, Color> tileColorCorrectedDict = new Dictionary<string, Color>();
    public Dictionary<string, Vector3> InitialTilePositions;

    private bool isAffectedByColorDeficiency = false;
    public GameObject ResultCalculator;

    public int pageNumTutorial = 0;

    public void Start()
    {
        LoadColorsFromCSV();
        GenerateTutorialGrid();
        InitialTilePositions = GetMovableTilePositions();
        SwitchScene.Instance.ShowCanvasTutorial();
    }

    // Lettura dei colori dal file
    void LoadColorsFromCSV()
    {
        if (colorCsv == null)
        {
            Debug.LogError("CSV File non assegnato in GridManager!");
            return;
        }

        tileColorDict.Clear();
        _linearKeys.Clear();

        string[] lines = colorCsv.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        // Configuriamo il parser per riempire le righe partendo dalla 4 (Fondo) a risalire alla 1 (Cima)
        // Questo perché generiamo la griglia dal basso, e leggiamo il file dal fondo.
        int currentRow = 4;
        int currentCountInRow = 0;

        // L'indice dell'array standardRowLengths è 0-based: 0=Riga1, 3=Riga4.
        // Quindi per Riga4 useremo index 3.
        int standardArrIndex = 3;

        // Iteriamo il file AL CONTRARIO (dal fondo verso l'inizio)
        // Saltiamo la riga 0 che è l'header
        for (int i = lines.Length - 1; i > 0; i--)
        {
            string[] values = lines[i].Split(',');
            if (values.Length < 8) continue;

            try
            {
                // Parsing colori (R_Linear, G_Linear, B_Linear)
                float r = float.Parse(values[5], CultureInfo.InvariantCulture);
                float g = float.Parse(values[6], CultureInfo.InvariantCulture);
                float b = float.Parse(values[7], CultureInfo.InvariantCulture);
                Color colorLinear = new Color(r, g, b, 1.0f);

                // Generiamo la chiave semantica "Riga-Colonna"
                currentCountInRow++;
                string key = $"{currentRow}-{currentCountInRow}";

                // Salviamo nel dizionario
                tileColorDict[key] = colorLinear;

                // Aggiungiamo alla lista lineare.
                // Nota: Siccome generiamo la griglia partendo dalla Riga 4, e stiamo leggendo i colori della Riga 4,
                // l'ordine di _linearKeys sarà [ColoriRiga4, ColoriRiga3, ...]. Perfetto per il loop di generazione.
                _linearKeys.Add(key);

                // Gestione cambio riga (a risalire: 4 -> 3 -> 2 -> 1)
                if (standardArrIndex >= 0)
                {
                    if (currentCountInRow >= standardRowLengths[standardArrIndex])
                    {
                        currentRow--;           // Passa alla riga sopra (es. da 4 a 3)
                        standardArrIndex--;     // Passa all'indice array precedente
                        currentCountInRow = 0;  // Reset contatore colonne
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Errore parsing riga CSV {i}: {e.Message}");
            }
        }

        Debug.Log($"Caricati {_linearKeys.Count} colori (Dal fondo).");
    }

    public void ResetGrid()
    {
        SwitchScene.Instance.ShowCanvasHueTest();

        if (isAffectedByColorDeficiency)
            TextTitle.text = "Filtro Applicato." + titleTest;
        else
            TextTitle.text = "Test Finale." + titleTest;

        GenerateGrid(standardRowLengths, 1);

        ShuffleTilesByRow();
        InitialTilePositions = GetMovableTilePositions();
    }

    public void GenerateTutorialGrid()
    {
        pageNumTutorial++;

        switch (pageNumTutorial)
        {
            case 1:
                // CASE 1: Griglia Custom (Invariata)
                transform.position = new Vector3(-3.38f, 7.76f, -0.5f);
                string[] tutorialColors = { "4-1", "4-4", "4-7", "4-10", "3-3", "3-6", "3-9" };
                GenerateCustomGrid(1, tutorialColors);
                TextProgressPage.text = "1/2";
                break;

            case 2:
                // CASE 2: Griglia 2 righe (11 e 10), prendendo i colori ogni 2
                transform.position = new Vector3(-5.64f, 7.2f, -0.5f);
                // Generiamo una griglia con le lunghezze della riga 4 e riga 3 (le prime due generate)
                // Riga 4 = 11 cols, Riga 3 = 11 cols (dal config sopra: 11, 10, 11, 11 -> R4=11, R3=11)
                // Aspetta, R3 è 11 o 10? 
                // CSV: R1(11), R2(10), R3(11), R4(11). -> Sì.
                // Quindi per il tutorial generiamo le prime due righe "dal basso": 11 e 11.
                GenerateGrid(new int[] { 11, 11 }, 2);
                TextProgressPage.text = "2/2";
                break;

            default:
                // DEFAULT: Test completo
                transform.position = new Vector3(-5.64f, 5.69f, -0.5f);
                SwitchScene.Instance.ShowCanvasHueTest();
                GenerateGrid(standardRowLengths, 1);
                break;
        }

        ShuffleTilesByRow();
        InitialTilePositions = GetMovableTilePositions();
    }


    // --- GENERAZIONE GRIGLIA ---
    public void GenerateGrid(int[] rowsConfig, int jump)
    {
        ClearAllRows();

        if (cubePrefab == null) return;

        tilesByRow = new List<List<GameObject>>();
        for (int i = 0; i < rowsConfig.Length; i++)
        {
            tilesByRow.Add(new List<GameObject>());
        }

        int globalListIndex = 0;

        for (int row = 0; row < rowsConfig.Length; row++)
        {
            int colCount = rowsConfig[row];

            for (int col = 0; col < colCount; col++)
            {
                int targetIndex = globalListIndex * jump;
                if (targetIndex >= _linearKeys.Count) targetIndex = _linearKeys.Count - 1;

                Vector3 localPos = new Vector3(col * spacing, yOffset, (-row * spacing) - 0.2f);
                GameObject tile = Instantiate(cubePrefab);
                tile.transform.SetParent(transform, worldPositionStays: false);
                tile.transform.localPosition = localPos;

                tile.transform.localRotation = Quaternion.identity;
                tile.transform.localScale = new Vector3(cubeSize, thickness, cubeSize);

                if (targetIndex >= 0 && targetIndex < _linearKeys.Count)
                {
                    string key = _linearKeys[targetIndex];
                    SetColor(ref tile, ref key);
                }

                // Naming
                // row=0 -> Row 4 (Totale righe - row)
                // Assumendo 4 righe totali:
                int visualRowIndex = 4 - row;

                bool isFirst = col == 0;
                bool isLast = col == colCount - 1;

                if (isFirst) tile.name = $"Row{visualRowIndex}_Start";
                else if (isLast) tile.name = $"Row{visualRowIndex}_End";
                else
                {
                    tile.name = $"Row{visualRowIndex}_Tile{col}";
                    tile.AddComponent<DirectTileMovement>();
                    if (tile.GetComponent<XRGrabInteractable>() == null)
                        tile.AddComponent<XRGrabInteractable>();
                    tilesByRow[row].Add(tile);
                }

                globalListIndex++;
            }
        }
    }

    private void GenerateCustomGrid(int rows, string[] colorKeys)
    {
        ClearAllRows();
        if (cubePrefab == null) return;

        tilesByRow = new List<List<GameObject>>();
        for (int i = 0; i < rows; i++) tilesByRow.Add(new List<GameObject>());

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < colorKeys.Length; col++)
            {
                Vector3 localPos = new Vector3(col * spacing, yOffset, (-row * spacing) - 0.2f);
                GameObject tile = Instantiate(cubePrefab);
                tile.transform.SetParent(transform, worldPositionStays: false);
                tile.transform.localPosition = localPos;

                tile.transform.localRotation = Quaternion.identity;
                tile.transform.localScale = new Vector3(cubeSize, thickness, cubeSize);

                string colorKey = colorKeys[col];
                SetColor(ref tile, ref colorKey);

                if (col == 0) tile.name = $"Tutorial_Row{row + 1}_Start";
                else if (col == colorKeys.Length - 1) tile.name = $"Tutorial_Row{row + 1}_End";
                else
                {
                    tile.name = $"Tutorial_Row{row + 1}_Tile{col}";
                    tile.AddComponent<DirectTileMovement>();
                    if (tile.GetComponent<XRGrabInteractable>() == null)
                        tile.AddComponent<XRGrabInteractable>();
                    tilesByRow[row].Add(tile);
                }
            }
        }
    }

    void SetColor(ref GameObject tile, ref string colorIndex)
    {
        Renderer renderer = tile.GetComponent<Renderer>();
        if (renderer != null && tileColorDict.ContainsKey(colorIndex))
        {
            if (isAffectedByColorDeficiency && tileColorCorrectedDict.ContainsKey(colorIndex))
                renderer.material.color = tileColorCorrectedDict[colorIndex];
            else
                renderer.material.color = tileColorDict[colorIndex];
        }
    }

    public void ClearAllRows()
    {
        foreach (Transform child in transform) Destroy(child.gameObject);
    }

    private void ShuffleTilesByRow()
    {
        for (int row = 0; row < tilesByRow.Count; row++)
        {
            List<GameObject> rowTiles = tilesByRow[row];
            for (int i = 0; i < rowTiles.Count; i++)
            {
                int randIndex = UnityEngine.Random.Range(i, rowTiles.Count);
                Vector3 tempPos = rowTiles[i].transform.localPosition;
                rowTiles[i].transform.localPosition = rowTiles[randIndex].transform.localPosition;
                rowTiles[randIndex].transform.localPosition = tempPos;
                (rowTiles[i], rowTiles[randIndex]) = (rowTiles[randIndex], rowTiles[i]);
            }
        }
    }

    private Dictionary<string, Vector3> GetMovableTilePositions()
    {
        Dictionary<string, Vector3> movableTilePositions = new Dictionary<string, Vector3>();
        DirectTileMovement[] movableTiles = GetComponentsInChildren<DirectTileMovement>();
        foreach (DirectTileMovement tileMovement in movableTiles)
        {
            GameObject tile = tileMovement.gameObject;
            movableTilePositions[tile.name] = tile.transform.localPosition;
        }
        return movableTilePositions;
    }

    public void SetColorDeficiency()
    {
        Debug.Log("Toggle color deficiency");
        tileColorCorrectedDict.Clear();
        if (ResultCalculator == null) return;

        TesResult resultPreTest = ResultCalculator.GetComponent<ResultTestCalculator>().GetTesResultPreTest();

        if (isAffectedByColorDeficiency)
        {
            isAffectedByColorDeficiency = false;
        }
        else
        {
            switch (resultPreTest.Verdict)
            {
                case AxisVerdict.Deuteranopia:
                    tileColorCorrectedDict = ColorCorrector.GetNewTileColorDic(tileColorDict, ColorCorrector.AnomalyType.Deuteranopia);
                    isAffectedByColorDeficiency = true;
                    break;
                case AxisVerdict.Protanopia:
                    tileColorCorrectedDict = ColorCorrector.GetNewTileColorDic(tileColorDict, ColorCorrector.AnomalyType.Protanopia);
                    isAffectedByColorDeficiency = true;
                    break;
                case AxisVerdict.Probable_BY:
                    tileColorCorrectedDict = ColorCorrector.GetNewTileColorDic(tileColorDict, ColorCorrector.AnomalyType.Tritanopia);
                    isAffectedByColorDeficiency = true;
                    break;
                default:
                    tileColorCorrectedDict = ColorCorrector.GetNewTileColorDic(tileColorDict, ColorCorrector.AnomalyType.Normal);
                    isAffectedByColorDeficiency = false;
                    break;
            }
        }
        ResetGrid();
    }

    public bool IsAffected()
    {
        return isAffectedByColorDeficiency;
    }
}