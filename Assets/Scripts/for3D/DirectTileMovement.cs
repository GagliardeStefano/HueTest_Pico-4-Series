using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class DirectTileMovement : MonoBehaviour
{
    private GridManager gridManager;
    private Vector3 startTileLocalPosition;
    private Vector3 startControllerWorldPosition;
    private XRGrabInteractable grabInteractable;
    private Transform controllerTransform;
    private bool isGrabbed = false;

    // Riferimento al GridManager per ottenere il spacing e i limiti
    private Transform gridParent;

    private readonly float minX = -4.25f, maxX = 4.50f; // Limiti per riga 

    [Header("Movement Settings")]
    public float movementMultiplier = 50f; // Amplifica il movimento del controller

    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        gridParent = transform.parent; // Il GridManager
        gridManager = gridParent.GetComponent<GridManager>();

       
        // IMPORTANTE: Disabilita completamente il movimento automatico
        grabInteractable.movementType = XRBaseInteractable.MovementType.Kinematic;
        grabInteractable.trackPosition = false;
        grabInteractable.trackRotation = false;
        grabInteractable.trackScale = false;

        // Disabilita anche i rigidbody se esistono
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // Sottoscrivi agli eventi
        grabInteractable.selectEntered.AddListener(OnGrabStart);
        grabInteractable.selectExited.AddListener(OnGrabEnd);
    }

    private void OnGrabStart(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        controllerTransform = args.interactorObject.transform;

        // Salva le posizioni di partenza (LOCAL per il tassello, WORLD per il controller)
        startTileLocalPosition = transform.localPosition;
        startControllerWorldPosition = controllerTransform.position;
        Vector3 elevatedPosition = transform.localPosition + new Vector3(0, 1, 0);

        
        Debug.Log($"Tassello {this.gameObject} Prima di assegnazione elevatedPosition: {this.transform.localPosition}");
        this.transform.localPosition = elevatedPosition;
        Debug.Log($"Tassello {this.gameObject} Dopo assegnazione elevatedPosition: {this.transform.localPosition}");


    }

    private void OnGrabEnd(SelectExitEventArgs args)
    {
        isGrabbed = false;
        controllerTransform = null;

        // Trova e scambia con il cubo movibile più vicino
        SwapWithNearestTile();
    }

    void Update()
    {
        if (isGrabbed && controllerTransform != null)
        {
            // Calcola quanto si è mosso il controller dalla posizione iniziale (in world space)
            Vector3 controllerWorldDelta = controllerTransform.position - startControllerWorldPosition;

            // Converti il movimento del controller in coordinate locali del GridManager
            Vector3 controllerLocalDelta = gridParent.InverseTransformDirection(controllerWorldDelta);

            // AMPLIFICA il movimento del controller per renderlo più responsivo
            Vector3 amplifiedMovement = controllerLocalDelta * movementMultiplier;

            // Applica solo il movimento sull'asse X locale
            Vector3 newLocalPosition = startTileLocalPosition + new Vector3(amplifiedMovement.x, 0, 0);

            // Limita il movimento all'interno della griglia
            newLocalPosition.x = Mathf.Clamp(newLocalPosition.x, minX, maxX);

            // Imposta la posizione locale
            transform.localPosition = newLocalPosition;
        }
    }

    private void SwapWithNearestTile()
    {
        // Soglia di distanza massima per effettuare lo swap
        float swapDistanceThreshold = 0.5f; // Modifica questo valore secondo le tue esigenze

        Vector3 currentPos = transform.localPosition;
        GameObject nearestTile = null;
        float shortestDistance = float.MaxValue;

        // Cerca tutti i cubi movibili nella stessa riga
        DirectTileMovement[] allMovableTiles = gridParent.GetComponentsInChildren<DirectTileMovement>();
        foreach (DirectTileMovement tileMovement in allMovableTiles)
        {
            GameObject otherTile = tileMovement.gameObject;
            // Salta se è lo stesso cubo
            if (otherTile == this.gameObject) continue;

            Vector3 otherPos = otherTile.transform.localPosition;
            // Controlla se è nella stessa riga (stesso Z)
            Debug.Log("Prima dell'IF della stessa riga");
            Debug.Log($"Sta controllando il tassello (other) {otherTile.name} che ha posizione {otherPos} e il tassello (grabbato) {gameObject.name} che ha posizione {currentPos}");
            if (Mathf.Approximately(otherPos.z, currentPos.z))
            {
                Debug.Log("DOPO dell'IF della stessa riga, quindi fanno parte della stessa riga");
                Debug.Log("Vicino nella stessa riga");
                float distance = Mathf.Abs(currentPos.x - otherPos.x);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearestTile = otherTile;
                    Debug.Log($"Trovato nuovo vicino {otherTile.name}");
                }
            }
        }

        Dictionary<string, Vector3> movablePositions = gridManager.InitialTilePositions;
        // Controlla se il tassello più vicino è entro la soglia di distanza
        if (nearestTile != null && shortestDistance <= swapDistanceThreshold)
        {
            Debug.Log("esiste il vicino");
            Vector3 myPos = movablePositions.GetValueOrDefault(this.name);
            Vector3 nearestPos = movablePositions.GetValueOrDefault(nearestTile.name);

            Debug.Log($"Scambio {this.name} in {myPos} con {nearestTile.name} in {nearestPos} (distanza: {shortestDistance})");

            // Scambia le posizioni
            transform.localPosition = nearestPos;
            nearestTile.transform.localPosition = myPos;

            // Aggiorna le posizioni nel dizionario del GridManager
            movablePositions[this.name] = nearestPos;
            movablePositions[nearestTile.name] = myPos;
        }
        else
        {
            Debug.Log("Non esiste il vicino");
            // Se non trova nessun cubo vicino o è troppo lontano, torna alla posizione di partenza
            if (nearestTile != null)
            {
                Debug.Log($"Tassello troppo lontano per lo swap. Distanza: {shortestDistance}, soglia: {swapDistanceThreshold}");
            }
            else
            {
                Debug.Log($"Nessun tassello trovato per lo swap di {this.name}");
            }

            transform.localPosition = movablePositions.GetValueOrDefault(this.name);
        }
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabStart);
            grabInteractable.selectExited.RemoveListener(OnGrabEnd);
        }
    }
}