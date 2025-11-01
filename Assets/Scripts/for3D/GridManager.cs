using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GridManager : MonoBehaviour
{
    [Header("Prefab del Cube (3D con XRGrabInteractable)")]
    public GameObject cubePrefab;

    [Header("Dimensioni griglia")]
    public float spacing = 1.25f; // distanza tra i cube

    [Header("Altezza sopra il plane")]
    public float yOffset = 0.01f; // leggero offset per non affondare i cube

    [Header("Dimensione dei cube")]
    public float cubeSize = 0.2f; // lato quadrato
    public float thickness = 0.01f; // spessore minimo sul piano

    [Header("Outline Settings")]
    public float outlineWidth = 3f; // spessore dell'outline per inizio/fine riga

    private List<List<GameObject>> tilesByRow; // Lista di liste per organizzare le tiles per riga

    public TextMeshProUGUI TextProgressPage;

    private readonly Dictionary<string, Color> tileColorDict = new Dictionary<string, Color>
    {
        // Riga 1
        { "1-1",  new Color32(132,132,163,255) },
        { "1-2",  new Color32(141,133,163,255) },
        { "1-3",  new Color32(148,131,160,255) },
        { "1-4",  new Color32(153,129,157,255) },
        { "1-5",  new Color32(159,127,152,255) },
        { "1-6",  new Color32(169,121,139,255) },
        { "1-7",  new Color32(174,119,135,255) },
        { "1-8",  new Color32(177,117,127,255) },
        { "1-9",  new Color32(179,117,122,255) },
        { "1-10", new Color32(179,118,115,255) },

        // Riga 2
        { "2-1",  new Color32(78,150,137,255) },
        { "2-2",  new Color32(76,150,145,255) },
        { "2-3",  new Color32(74,150,150,255) },
        { "2-4",  new Color32(74,150,152,255) },
        { "2-5",  new Color32(82,148,159,255) },
        { "2-6",  new Color32(96,144,165,255) },
        { "2-7",  new Color32(104,143,167,255) },
        { "2-8",  new Color32(108,138,166,255) },
        { "2-9",  new Color32(116,137,167,255) },
        { "2-10", new Color32(123,132,163,255) },

        // Riga 3
        { "3-1",  new Color32(151,145,75,255) },
        { "3-2",  new Color32(141,147,82,255) },
        { "3-3",  new Color32(134,149,92,255) },
        { "3-4",  new Color32(126,151,96,255) },
        { "3-5",  new Color32(124,149,103,255) },
        { "3-6",  new Color32(105,154,113,255) },
        { "3-7",  new Color32(100,154,118,255) },
        { "3-8",  new Color32(91,148,122,255) },
        { "3-9",  new Color32(88,148,128,255) },
        { "3-10", new Color32(82,150,135,255) },

        // Riga 4
        { "4-1",  new Color32(178,118,111,255) },
        { "4-2",  new Color32(177,116,102,255) },
        { "4-3",  new Color32(174,114,95,255) },
        { "4-4",  new Color32(168,116,90,255) },
        { "4-5",  new Color32(168,116,82,255) },
        { "4-6",  new Color32(168,121,78,255) },
        { "4-7",  new Color32(169,126,76,255) },
        { "4-8",  new Color32(167,130,68,255) },
        { "4-9",  new Color32(162,137,70,255) },
        { "4-10", new Color32(157,142,72,255) }
    };

    private Dictionary<string, Color> tileColorCorrectedDict = new Dictionary<string, Color>();

    private Boolean isAffectedByColorDeficiency = false;

    public Dictionary<string, Vector3> InitialTilePositions;

    public int pageNumTutorial = 0;
    public void Start()
    {
        GenerateTutorialGrid();
        InitialTilePositions = GetMovableTilePositions();
        SwitchScene.Instance.ShowCanvasTutorial();
    }
    public void ResetGrid()
    {
        SwitchScene.Instance.ShowCanvasHueTest();
        GenerateGrid(4, 10, 1);
        ShuffleTilesByRow();
        InitialTilePositions = GetMovableTilePositions();
    }

    public void ClearAllRows()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }

    // Genera griglia 2x5 saltando colori ogni 4
    public void GenerateTutorialGrid()
    {
        pageNumTutorial++;

        switch (pageNumTutorial)
        {
            case 1:
                transform.position = new Vector3(-3.38f, 7.76f, -0.5f);
                string[] tutorialColors = { "4-1", "4-4", "4-7", "4-10", "3-3", "3-6", "3-9" };
                GenerateCustomGrid(1, tutorialColors);
                break;

            case 2:
                transform.position = new Vector3(-5.64f, 7.2f, -0.5f);
                GenerateGrid(2, 10, 2);
                Debug.Log("Initial tile position " + InitialTilePositions);
                break;

            default:
                transform.position = new Vector3(-5.64f, 5.69f, -0.5f);
                SwitchScene.Instance.ShowCanvasHueTest();
                GenerateGrid(4, 10, 1);
                break;
        }
        TextProgressPage.text = $"{pageNumTutorial}/2";
        ShuffleTilesByRow();
        InitialTilePositions = GetMovableTilePositions();
    }


    /// <summary>
    /// Genera una griglia di dimensioni rows x columns.
    /// La selezione del colore segue l'ordine delle righe personalizzato: 4 -> 1 -> 2 -> 3.
    /// 'jump': Salta gli INDICI dei COLORI all'interno della sequenza personalizzata.
    /// </summary>
    /// <summary>
    /// Genera una griglia di dimensioni rows x columns.
    /// Se jump = 1, l'ordine dei colori è sequenziale (riga 1->2->3->4).
    /// Se jump != 1, l'ordine è personalizzato (riga 4->1->2->3) e applica il salto.
    /// </summary>
    public void GenerateGrid(int rows, int columns, int jump)
    {
        ClearAllRows();

        if (cubePrefab == null)
        {
            Debug.LogError("Nessun prefab assegnato al CubeGridManager!");
            return;
        }

        tilesByRow = new List<List<GameObject>>();
        for (int i = 0; i < rows; i++)
        {
            tilesByRow.Add(new List<GameObject>());
        }

        int globalTileIndex = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector3 localPos = new Vector3(col * spacing, yOffset, (-row * spacing) - 0.2f);
                GameObject tile = Instantiate(cubePrefab);
                tile.transform.SetParent(transform, worldPositionStays: false);
                tile.transform.localPosition = localPos;

                string colorIndex;

                // --- LOGICA CONDIZIONALE PER LA SELEZIONE DEL COLORE ---
                if (jump == 1)
                {
                    // CASO 1: JUMP = 1 -> Ordine originale sequenziale
                    int linearColorIndex = globalTileIndex; // Nessun salto

                    int colorRow = (linearColorIndex / 10) + 1;
                    int colorCol = (linearColorIndex % 10) + 1;

                    // Limita alle 4 righe disponibili
                    if (colorRow > 4)
                    {
                        colorRow = 4;
                        colorCol = 10;
                    }
                    colorIndex = $"{colorRow}-{colorCol}";
                }
                else
                {
                    // CASO 2: JUMP != 1 -> Ordine personalizzato con salto
                    int[] rowOrder = { 4, 3, 2, 1 };
                    int linearColorIndex = globalTileIndex * jump;
                    int blockIndex = linearColorIndex / 10;
                    int colorRow = rowOrder[blockIndex % rowOrder.Length];
                    int colorCol = (linearColorIndex % 10) + 1;
                    colorIndex = $"{colorRow}-{colorCol}";
                }
                // --- FINE LOGICA ---

                if (tileColorDict.ContainsKey(colorIndex))
                {
                    SetColor(ref tile, ref colorIndex);
                }
                else
                {
                    Debug.LogWarning($"Colore non trovato per l'indice: {colorIndex}");
                }

                globalTileIndex++;

                tile.transform.localRotation = Quaternion.identity;
                tile.transform.localScale = new Vector3(cubeSize, thickness, cubeSize);

                bool isFirstCol = col == 0;
                bool isLastCol = col == columns - 1;

                if (isFirstCol)
                {
                    tile.name = $"Row{rows - row}_Start";
                }
                else if (isLastCol)
                {
                    tile.name = $"Row{rows - row}_End";
                }
                else
                {
                    tile.name = $"Row{rows - row}_Tile{col}";
                    tile.AddComponent<DirectTileMovement>();
                    if (tile.GetComponent<XRGrabInteractable>() == null)
                    {
                        tile.AddComponent<XRGrabInteractable>();
                    }
                    tilesByRow[row].Add(tile);
                }
            }
        }
    }

    private void GenerateCustomGrid(int rows, string[] colorKeys)
    {
        ClearAllRows();

        if (cubePrefab == null)
        {
            Debug.LogError("Nessun prefab assegnato al CubeGridManager!");
            return;
        }

        tilesByRow = new List<List<GameObject>>();
        for (int i = 0; i < rows; i++)
        {
            tilesByRow.Add(new List<GameObject>());
        }

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < colorKeys.Length; col++)
            {
                Vector3 localPos = new Vector3(col * spacing, yOffset, (-row * spacing) - 0.2f);
                GameObject tile = Instantiate(cubePrefab);
                tile.transform.SetParent(transform, worldPositionStays: false);
                tile.transform.localPosition = localPos;

                // Usa direttamente l'indice dell'array per i colori custom
                string colorKey = colorKeys[col];
                if (tileColorDict.ContainsKey(colorKey))
                {
                    SetColor(ref tile, ref colorKey);
                }
                else
                {
                    Debug.LogWarning($"Colore non trovato: {colorKey}");
                }

                if (col == 0)
                {
                    tile.transform.localRotation = Quaternion.identity;
                    tile.transform.localScale = new Vector3(cubeSize, thickness, cubeSize);
                    tile.name = $"Tutorial_Row{rows - row}_Start";

                }
                else if (col == colorKeys.Length - 1)
                {
                    tile.transform.localRotation = Quaternion.identity;
                    tile.transform.localScale = new Vector3(cubeSize, thickness, cubeSize);
                    tile.name = $"Tutorial_Row{rows - row}_End";
                }
                else
                {
                    tile.transform.localRotation = Quaternion.identity;
                    tile.transform.localScale = new Vector3(cubeSize, thickness, cubeSize);
                    tile.name = $"Tutorial_Row{rows - row}_Tile{col + 1}";
                    tile.AddComponent<DirectTileMovement>();
                    XRGrabInteractable tileInteractable = tile.GetComponent<XRGrabInteractable>();
                    if (tileInteractable == null)
                    {
                        tileInteractable = tile.AddComponent<XRGrabInteractable>();
                    }

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
            if (isAffectedByColorDeficiency)
                renderer.material.color = tileColorCorrectedDict[colorIndex];
            else
                renderer.material.color = tileColorDict[colorIndex];
        }
        else
        {
            Debug.LogWarning($"Renderer non trovato o colore mancante per {colorIndex}");
        }
    }

    private void ShuffleTilesByRow()
    {
        // Mescola le tiles di ogni riga separatamente
        for (int row = 0; row < tilesByRow.Count; row++)
        {
            List<GameObject> rowTiles = tilesByRow[row];

            // Fisher-Yates shuffle per ogni riga
            for (int i = 0; i < rowTiles.Count; i++)
            {
                int randIndex = UnityEngine.Random.Range(i, rowTiles.Count);

                // Scambia le posizioni delle tiles nella griglia
                Vector3 tempPosition = rowTiles[i].transform.localPosition;
                rowTiles[i].transform.localPosition = rowTiles[randIndex].transform.localPosition;
                rowTiles[randIndex].transform.localPosition = tempPosition;

                // Scambia anche gli elementi nella lista
                (rowTiles[i], rowTiles[randIndex]) = (rowTiles[randIndex], rowTiles[i]);
            }
        }
    }

    private Dictionary<string, Vector3> GetMovableTilePositions()
    {
        Dictionary<string, Vector3> movableTilePositions = new Dictionary<string, Vector3>();

        // Ottieni tutti i componenti DirectTileMovement (solo cubi movibili)
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
        isAffectedByColorDeficiency = !isAffectedByColorDeficiency;
        tileColorCorrectedDict.Clear();

        //TODO aggiungere tipo anomalia come parametro
        tileColorCorrectedDict = ColorCorrector.GetNewTileColorDic(tileColorDict,ColorCorrector.AnomalyType.Deuteranopia );
        ResetGrid();
    }
}

// Componente per vincolare il movimento (alternativa a DirectTileMovement)
public class ConstrainedMovement : MonoBehaviour
{
    private Vector3 originalPosition;
    private XRGrabInteractable grabInteractable;
    public float snapSpacing = 1.25f;
    public int minColumn = 1;
    public int maxColumn = 8;

    void Start()
    {
        originalPosition = transform.localPosition;
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabStart);
            grabInteractable.selectExited.AddListener(OnGrabEnd);
        }
    }

    private void OnGrabStart(SelectEnterEventArgs args)
    {
        // Salva la posizione corrente come riferimento
        originalPosition = transform.localPosition;
    }

    private void OnGrabEnd(SelectExitEventArgs args)
    {
        SnapToGrid();
    }

    private void Update()
    {
        if (grabInteractable != null && grabInteractable.isSelected)
        {
            Vector3 currentPos = transform.localPosition;
            // Vincola solo movimento su asse X
            transform.localPosition = new Vector3(
                currentPos.x,
                originalPosition.y,
                originalPosition.z
            );
        }
    }

    private void SnapToGrid()
    {
        Vector3 currentPos = transform.localPosition;
        int nearestCol = Mathf.RoundToInt(currentPos.x / snapSpacing);
        nearestCol = Mathf.Clamp(nearestCol, minColumn, maxColumn);

        transform.localPosition = new Vector3(
            nearestCol * snapSpacing,
            originalPosition.y,
            originalPosition.z
        );
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabStart);
            grabInteractable.selectExited.RemoveListener(OnGrabEnd);
        }
    }
}
