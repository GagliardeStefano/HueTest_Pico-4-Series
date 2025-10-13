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
        canvasTutorial.SetActive(true);
        canvasHueTest.SetActive(false);
        canvasOutput.SetActive(false);
    }

    public void ShowCanvasHueTest()
    {
        canvasTutorial.SetActive(false);
        canvasHueTest.SetActive(true);
        canvasOutput.SetActive(false);
    }

    public void ShowCanvasOutput()
    {
        canvasTutorial.SetActive(false);
        canvasHueTest.SetActive(false);
        canvasOutput.SetActive(true);
    }

}
