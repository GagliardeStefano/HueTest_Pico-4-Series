using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GrabLogger : MonoBehaviour
{
    // Start is called before the first frame update
    private string logFilePath;

    void Start()
    {
        // Percorso su Pico/Android, sicuro per scrivere file
        Debug.Log("path"+Application.persistentDataPath);
        logFilePath = Path.Combine(Application.persistentDataPath, "grab_log.txt");
    }

    // Chiamala quando grabbi l’oggetto
    public void LogGrab(GameObject grabbedObject, bool grabbed)
    {
        string coords = grabbedObject.transform.position.ToString("F3");
        string rot = grabbedObject.transform.rotation.eulerAngles.ToString("F1");

        string line;

        if (grabbed)
        {
            line = $"Grabbato {System.DateTime.Now:HH:mm:ss} | {grabbedObject.name} | Pos: {coords} | Rot: {rot}\n";
        }
        else
        {
            line = $"Lasciato {System.DateTime.Now:HH:mm:ss} | {grabbedObject.name} | Pos: {coords} | Rot: {rot}\n";

        }


        try
        {
            File.AppendAllText(logFilePath, line);
            Debug.Log("Log scritto in: " + logFilePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Errore scrittura log: " + e);
        }
    }
}
