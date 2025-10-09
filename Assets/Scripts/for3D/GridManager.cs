using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

    public Dictionary<string, Vector3> InitialTilePositions;

    public void StartWithTest()
    {
        Debug.Log("########################### chiamata funzione StartWithTest");
        GenerateGrid(4, 10, 1);       
        ShuffleTilesByRow();
        InitialTilePositions = GetMovableTilePositions();
        SwitchScene.Instance.ShowCanvasHueTest();
    }

    public void ResetGrid()
    {
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

    // Genera griglia 1x5 con colori: 1-8, 2-4, 3-1, 3-10, 4-5
    public void GenerateTutorialGrid1()
    {
        string[] tutorialColors = { "1-8", "2-4", "3-1", "3-10", "4-5" };
        GenerateCustomGrid(1, tutorialColors);
    }

    // Genera griglia 2x5 saltando colori ogni 4
    public void GenerateTutorialGrid2()
    {
        GenerateGrid(2, 5, 4);
    }

    // Genera griglia 2x10 con salto 2
    public void GenerateTutorialGrid3()
    {
        GenerateGrid(2, 10, 2);
    }

    /// <summary>
    /// Genera una griglia di dimensioni rows x columns
    /// jump: salto negli INDICI dei COLORI (non nelle posizioni fisiche)
    /// Es: jump=2 usa i colori 1,3,5,7,9 ma crea le tile in posizioni consecutive 0,1,2,3,4
    /// </summary>
    public void GenerateGrid(int rows, int columns, int jump)
    {
        ClearAllRows();

        if (cubePrefab == null)
        {
            Debug.LogError("Nessun prefab assegnato al CubeGridManager!");
            return;
        }

        // Inizializza la lista delle righe
        tilesByRow = new List<List<GameObject>>();
        for (int i = 0; i < rows; i++)
        {
            tilesByRow.Add(new List<GameObject>());
        }

        int globalTileIndex = 0; // Contatore globale delle tile create

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                // Posizione fisica: usa col direttamente (tile consecutive)
                Vector3 localPos = new Vector3(
                    col * spacing,  // 0, 1.25, 2.5, 3.75... (sempre consecutive!)
                    yOffset,
                    (-row * spacing) - 0.2f
                );

                // istanzia il cube senza ereditare la scala del parent
                GameObject tile = Instantiate(cubePrefab);

                // posiziona il cube come figlio del GridManager
                tile.transform.SetParent(transform, worldPositionStays: false);
                tile.transform.localPosition = localPos;

                // Indice colore: salta in base a jump usando l'indice globale
                // globalTileIndex incrementa per ogni tile creata (0,1,2,3...)
                // Moltiplica per jump per saltare i colori
                int linearColorIndex = globalTileIndex * jump;

                // Converti in riga e colonna del dizionario (che parte da 1)
                int colorRow = (linearColorIndex / 10) + 1;  // Riga (1-4)
                int colorCol = (linearColorIndex % 10) + 1;   // Colonna (1-10)

                // Limita alle 4 righe disponibili nel dizionario
                if (colorRow > 4)
                {
                    colorRow = 4;
                    colorCol = 10; // Usa l'ultimo colore disponibile
                }

                string colorIndex = $"{colorRow}-{colorCol}";

                if (tileColorDict.ContainsKey(colorIndex))
                {
                    SetColor(ref tile, ref colorIndex);
                    Debug.Log($"Assegnato colore {colorIndex} alla tile in Row{rows - row}_Tile{col + 1}");
                }
                else
                {
                    Debug.LogWarning($"Colore non trovato per indice: {colorIndex}");
                }

                globalTileIndex++; // Incrementa per ogni tile creata

                // ruota il cube per appoggiarlo sul plane (non serve rotazione come il quad)
                tile.transform.localRotation = Quaternion.identity;

                // forza scala quadrata con spessore minimo
                tile.transform.localScale = new Vector3(cubeSize, thickness, cubeSize);

                // Prima e ultima colonna sono fisse (Start/End)
                bool isFirstCol = col == 0;
                bool isLastCol = col == columns - 1;

                if (isFirstCol)
                {
                    tile.name = $"Row{rows - row}_Start";
                    Outline outline = tile.GetComponent<Outline>();
                    if (outline != null)
                    {
                        outline.effectColor = Color.red;
                    }
                }
                else if (isLastCol)
                {
                    tile.name = $"Row{rows - row}_End";
                    Outline outline = tile.GetComponent<Outline>();
                    if (outline != null)
                    {
                        outline.effectColor = Color.red;
                    }
                }
                else
                {
                    tile.name = $"Row{rows - row}_Tile{col}";

                    tile.AddComponent<DirectTileMovement>();

                    XRGrabInteractable tileInteractable = tile.GetComponent<XRGrabInteractable>();
                    if (tileInteractable == null)
                    {
                        tileInteractable = tile.AddComponent<XRGrabInteractable>();
                    }

                    // Aggiungi alle tile movibili della riga
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

                tile.transform.localRotation = Quaternion.identity;
                tile.transform.localScale = new Vector3(cubeSize, thickness, cubeSize);
                tile.name = $"Tutorial_Row{rows - row}_Tile{col + 1}";

                // Tutte le tile del tutorial sono movibili
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

    void SetColor(ref GameObject tile, ref string colorIndex)
    {
        Renderer renderer = tile.GetComponent<Renderer>();
        if (renderer != null && tileColorDict.ContainsKey(colorIndex))
        {
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
                int randIndex = Random.Range(i, rowTiles.Count);

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