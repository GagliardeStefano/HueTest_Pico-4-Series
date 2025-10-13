using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SwitchToTestScene : MonoBehaviour
{
    public void ActiveTestScene()
    {
        SceneManager.LoadScene("TestScene", LoadSceneMode.Single);
    }
}
