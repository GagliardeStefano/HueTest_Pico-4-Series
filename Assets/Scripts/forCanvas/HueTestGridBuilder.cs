using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class HueTestGridBuilder : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject startRowPrefab;
    public GameObject endRowPrefab;
    public GameObject tilePrefab;

    [Header("Rows")]
    public GameObject[] rowsObjects;

    [Header("Col for row")]
    private readonly int cols = 10;

    private readonly Color[] tileColors = new Color[] // colors in the right sequence
    {
        new Color32(178,118,111,255), new Color32(177,116,102,255), new Color32(174,114,95,255), new Color32(168,116,90,255),
        new Color32(168,116,82,255), new Color32(168,121,78,255), new Color32(169,126,76,255), new Color32(167,130,68,255),
        new Color32(162,137,70,255), new Color32(157,142,72,255), new Color32(151,145,75,255), new Color32(141,147,82,255),
        new Color32(134,149,92,255), new Color32(126,151,96,255), new Color32(124,149,103,255), new Color32(105,154,113,255),
        new Color32(100,154,118,255), new Color32(91,148,122,255), new Color32(88,148,128,255), new Color32(82,150,135,255),
        new Color32(78,150,137,255), new Color32(76,150,145,255), new Color32(74,150,150,255), new Color32(74,150,152,255),
        new Color32(82,148,159,255), new Color32(96,144,165,255), new Color32(104,143,167,255), new Color32(108,138,166,255),
        new Color32(116,137,167,255), new Color32(123,132,163,255), new Color32(132,132,163,255), new Color32(141,133,163,255),
        new Color32(148,131,160,255), new Color32(153,129,157,255), new Color32(159,127,152,255), new Color32(169,121,139,255),
        new Color32(174,119,135,255), new Color32(177,117,127,255), new Color32(179,117,122,255), new Color32(179,118,115,255)
    };

    // Start is called before the first frame update
    public void Start()
    {
        BuildGrid();
    }

    public void BuildGrid()
    {
        ClearAllRows();

        int colorIndex = 0;
        for (int r = 0; r < rowsObjects.Length; r++)
        {
            var rowObject = rowsObjects[r];

            // Create and color first tile
            InstantiateAndColorTile(startRowPrefab, rowObject.transform, $"Row{r + 1}_Start", ref colorIndex);

            // Create and add movable tiles
            var movableTiles = CreateMovableTiles(r + 1, ref colorIndex);
            ShuffleTiles(movableTiles);
            AddTilesToRow(movableTiles, rowObject.transform);


            // Create and color last tile
            InstantiateAndColorTile(endRowPrefab, rowObject.transform, $"Row{r + 1}_End", ref colorIndex);

        }
    }

    public void ClearAllRows()
    {
        foreach (var rowObject in rowsObjects)
        {
            // Delete any existing children
            foreach (Transform child in rowObject.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    public void InstantiateAndColorTile(GameObject prefab, Transform parent, string name, ref int colorIndex)
    {
        var tile = Instantiate(prefab, parent);
        tile.name = name;
        SetColor(tile, tileColors[colorIndex++]);
    }

    private List<GameObject> CreateMovableTiles(int rowIndex, ref int colorIndex)
    {
        var movableTiles = new List<GameObject>();
        for (int i = 0; i < cols - 2; i++) // -2 for start and end tiles
        {
            var tile = Instantiate(tilePrefab);
            tile.name = $"Row{rowIndex}_Tile_{i + 1}";
            SetColor(tile, tileColors[colorIndex++]);
            movableTiles.Add(tile);
        }
        return movableTiles;
    }

    private void ShuffleTiles(List<GameObject> tiles)
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            int randIndex = Random.Range(i, tiles.Count);
            (tiles[i], tiles[randIndex]) = (tiles[randIndex], tiles[i]);
        }
    }

    private void AddTilesToRow(List<GameObject> tiles, Transform parent)
    {
        foreach (var tile in tiles)
        {
            tile.transform.SetParent(parent, false);
        }
    }

    public void SetColor(GameObject obj, Color color)
    {
        var img = obj.GetComponent<Image>();
        img.color = color;
    }

    public Color[] GetTileColors()
    {
        return tileColors;
    }
}
