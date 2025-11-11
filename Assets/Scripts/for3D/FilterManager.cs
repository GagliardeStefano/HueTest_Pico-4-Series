using UnityEngine;

public class FilterManager : MonoBehaviour
{
    public GameObject ResultCalculator;

    private TesResult resultPreTest;

    public void ApplicaFiltro()
    {
        resultPreTest = ResultCalculator.GetComponent<ResultTestCalculator>().GetTesResultPreTest();

        // cambio dei colori (script di Chri)

        // creazione array con colori nuovi

        // passare nuovo array a gridManager

        // chiamare la funzione di gridManager -> GenerateGrid (che verrà lanciato con il nuovo array)


        Debug.Log("############ result in Applica filtro: \n" + resultPreTest.TotalTES);
        Debug.Log("############ result in Applica filtro: \n" + resultPreTest.Verdict);
    }

    public void ViewDiffTesResults(TesResult resultPreFiltro, TesResult resultPostFiltro)
    {

    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}