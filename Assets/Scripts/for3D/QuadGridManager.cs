using UnityEngine;

public class QuadGridManager : MonoBehaviour
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



    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        if (cubePrefab == null)
        {
            Debug.LogError("⚠ Nessun prefab assegnato al CubeGridManager!");
            return;
        }

        int colorIndex;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                // calcola posizione nella griglia
                Vector3 localPos = new Vector3(col * spacing, yOffset, -row * spacing);

                // istanzia il cube senza ereditare la scala del parent
                GameObject cube = Instantiate(cubePrefab);

                // posiziona il cube come figlio del GridManager
                cube.transform.SetParent(transform, worldPositionStays: false);
                cube.transform.localPosition = localPos;

                //SetColor(cube, color);

                // ruota il cube per appoggiarlo sul plane (non serve rotazione come il quad)
                cube.transform.localRotation = Quaternion.identity;

                // forza scala quadrata con spessore minimo
                cube.transform.localScale = new Vector3(cubeSize, thickness, cubeSize);
            }
        }
    }

    void SetColor(ref GameObject tile, Color color)
    {
        tile.GetComponent<Renderer>().material.color = color;
    }
}