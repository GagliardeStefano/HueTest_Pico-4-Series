using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SwitchScene : MonoBehaviour
{
    public static SwitchScene Instance { get; private set; }

    [Header("Canvas (solo nella TestScene)")]
    public GameObject canvasTutorial;
    public GameObject canvasHueTest;
    public GameObject canvasOutput;

    private void Awake() 
    {
        if(Instance == null)
            Instance = this;
    }

    public void ActiveCanvasScene()
    {
        SceneManager.LoadScene("CanvasScene", LoadSceneMode.Single);
    }

    public void ShowCanvasTutorial()
    {
        if (canvasTutorial == null) return; // sicurezza per la prima scena
        canvasTutorial.SetActive(true);
        canvasHueTest.SetActive(false);
        canvasOutput.SetActive(false);
    }

    public void ShowCanvasHueTest()
    {
        if (canvasHueTest == null)
        {
            Debug.Log("ShowHueTest -> canvas non esiste");
            return;
        }

        Debug.Log("La canvas c'è ");
        canvasTutorial.SetActive(false);
        canvasHueTest.SetActive(true);
        canvasOutput.SetActive(false);
    }

    public void ShowCanvasOutput()
    {
        if (canvasOutput == null) return;
        canvasTutorial.SetActive(false);
        canvasHueTest.SetActive(false);
        canvasOutput.SetActive(true);
    }

}
