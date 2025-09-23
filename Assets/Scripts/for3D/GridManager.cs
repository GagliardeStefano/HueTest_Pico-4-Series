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
    public int rows = 2;
    public int columns = 5;
    public float spacing = 0.3f; // distanza tra i cube

    [Header("Altezza sopra il plane")]
    public float yOffset = 0.01f; // leggero offset per non affondare i cube

    [Header("Dimensione dei cube")]
    public float cubeSize = 0.2f; // lato quadrato
    public float thickness = 0.01f; // spessore minimo sul piano

    [Header("Outline Settings")]
    public float outlineWidth = 3f; // spessore dell'outline per inizio/fine riga

    private List<List<GameObject>> tilesByRow; // Lista di liste per organizzare le tiles per riga

    private readonly Color[] tileColors = new Color[] // colors with rows inverted horizontally only
    {
        // Riga 1 - invertita orizzontalmente
        new Color32(132,132,163,255), new Color32(141,133,163,255), new Color32(148,131,160,255), new Color32(153,129,157,255),
        new Color32(159,127,152,255), new Color32(169,121,139,255), new Color32(174,119,135,255), new Color32(177,117,127,255),
        new Color32(179,117,122,255), new Color32(179,118,115,255),
    
        // Riga 2 - invertita orizzontalmente  
        new Color32(78,150,137,255), new Color32(76,150,145,255), new Color32(74,150,150,255), new Color32(74,150,152,255),
        new Color32(82,148,159,255), new Color32(96,144,165,255), new Color32(104,143,167,255), new Color32(108,138,166,255),
        new Color32(116,137,167,255), new Color32(123,132,163,255),
    
        // Riga 3 - invertita orizzontalmente
        new Color32(151,145,75,255), new Color32(141,147,82,255), new Color32(134,149,92,255), new Color32(126,151,96,255),
        new Color32(124,149,103,255), new Color32(105,154,113,255), new Color32(100,154,118,255), new Color32(91,148,122,255),
        new Color32(88,148,128,255), new Color32(82,150,135,255),
    
        // Riga 4 - invertita orizzontalmente
        new Color32(178,118,111,255), new Color32(177,116,102,255), new Color32(174,114,95,255), new Color32(168,116,90,255),
        new Color32(168,116,82,255), new Color32(168,121,78,255), new Color32(169,126,76,255), new Color32(167,130,68,255),
        new Color32(162,137,70,255), new Color32(157,142,72,255)
    };

    void Start()
    {
        GenerateGrid();
        ShuffleTilesByRow();
    }

    void GenerateGrid()
    {
        if (cubePrefab == null)
        {
            Debug.LogError("⚠ Nessun prefab assegnato al CubeGridManager!");
            return;
        }

        // Inizializza la lista delle righe
        tilesByRow = new List<List<GameObject>>();
        for (int i = 0; i < rows; i++)
        {
            tilesByRow.Add(new List<GameObject>());
        }

        int colorIndex = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                // calcola posizione nella griglia
                Vector3 localPos = new Vector3(col * spacing, yOffset, -row * spacing);

                // istanzia il cube senza ereditare la scala del parent
                GameObject tile = Instantiate(cubePrefab);

                // posiziona il cube come figlio del GridManager
                tile.transform.SetParent(transform, worldPositionStays: false);
                tile.transform.localPosition = localPos;

                SetColor(ref tile, ref colorIndex);

                // ruota il cube per appoggiarlo sul plane (non serve rotazione come il quad)
                tile.transform.localRotation = Quaternion.identity;

                // forza scala quadrata con spessore minimo
                tile.transform.localScale = new Vector3(cubeSize, thickness, cubeSize);

                if (col == 0)
                {
                    tile.name = $"Row{rows - row}_Start";
                    tile.GetComponent<Outline>().effectColor = Color.red;
                }
                else if (col == columns - 1)
                {
                    tile.name = $"Row{rows - row}_End";
                    tile.GetComponent<Outline>().effectColor = Color.red;
                }
                else
                {
                    tile.name = $"Row{rows - row}_Tile{col}";

                    // Configurazione XRGrabInteractable per movimento vincolato
                    XRGrabInteractable tileInteractable = tile.GetComponent<XRGrabInteractable>();
                    if (tileInteractable == null)
                    {
                        tileInteractable = tile.AddComponent<XRGrabInteractable>();
                    }

                    // Usa Kinematic per movimento più diretto e veloce
                    tileInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;

                    // Disabilita tracking di rotazione e scala
                    tileInteractable.trackRotation = false;
                    tileInteractable.trackScale = false;
                    tileInteractable.trackPosition = true;

                    // Disabilita smoothing per movimento più reattivo
                    tileInteractable.smoothPosition = false;
                    tileInteractable.smoothRotation = false;

                    // Aggiungi la tile alla riga corrispondente (solo quelle che possono essere mescolate)
                    tilesByRow[row].Add(tile);
                }
            }
        }
    }

    void SetColor(ref GameObject tile, ref int colorIndex)
    {
        tile.GetComponent<Renderer>().material.color = tileColors[colorIndex++];
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
}

// Componente aggiuntivo per vincolare il movimento
public class ConstrainedMovement : MonoBehaviour
{
    private Vector3 originalPosition;
    private int rowIndex;
    private XRGrabInteractable grabInteractable;

    public void Initialize(Vector3 originalPos, int row)
    {
        originalPosition = originalPos;
        rowIndex = row;
        grabInteractable = GetComponent<XRGrabInteractable>();

        // Sottoscrivi agli eventi di grab
        grabInteractable.selectEntered.AddListener(OnGrabStart);
        grabInteractable.selectExited.AddListener(OnGrabEnd);
    }

    private void OnGrabStart(SelectEnterEventArgs args)
    {
        // Salva la posizione corrente come riferimento
        originalPosition = transform.localPosition;
    }

    private void OnGrabEnd(SelectExitEventArgs args)
    {
        // Opzionale: snap alla posizione più vicina nella griglia
        SnapToGrid();
    }

    private void Update()
    {
        // Se l'oggetto è grabbato, vincola il movimento
        if (grabInteractable.isSelected)
        {
            Vector3 currentPos = transform.localPosition;

            // Mantieni solo il movimento sull'asse X, blocca Y e Z
            transform.localPosition = new Vector3(
                currentPos.x,
                originalPosition.y,
                originalPosition.z
            );
        }
    }

    private void SnapToGrid()
    {
        // Opzionale: snap alla posizione della griglia più vicina
        float spacing = 0.3f; // Usa lo stesso spacing del GridManager
        Vector3 currentPos = transform.localPosition;

        // Calcola la colonna più vicina
        int nearestCol = Mathf.RoundToInt(currentPos.x / spacing);

        // Limita alle colonne valide (escludi inizio e fine)
        nearestCol = Mathf.Clamp(nearestCol, 1, 8); // Assumendo 5 colonne totali (indici 1-3 per quelle mobili)

        // Snap alla posizione
        transform.localPosition = new Vector3(
            nearestCol * spacing,
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