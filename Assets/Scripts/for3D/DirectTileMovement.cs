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
    private float spacing = 1.25f;

    private readonly float minX = -4.25f, maxX = 5.75f; // Limiti per riga 

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
        Vector3 currentPos = transform.localPosition;
        GameObject nearestTile = null;
        float shortestDistance = float.MaxValue;

        // Cerca tutti i cubi movibili nella stessa riga
        DirectTileMovement[] allMovableTiles = gridParent.GetComponentsInChildren<DirectTileMovement>();

        foreach (DirectTileMovement tileMovement in allMovableTiles)
        {
            GameObject otherTile = tileMovement.gameObject;

            // Salta se è lo stesso cubo
            if (otherTile == gameObject) continue;

            Vector3 otherPos = otherTile.transform.localPosition;

            // Controlla se è nella stessa riga (stesso Z)
            if (Mathf.Approximately(otherPos.z, currentPos.z))
            {
                float distance = Mathf.Abs(currentPos.x - otherPos.x);

                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearestTile = otherTile;
                }
            }
        }

        // Se hai trovato un cubo vicino, scambia le posizioni
        if (nearestTile != null)
        {
            Dictionary<string, Vector3> movablePositions = gridManager.InitialTilePositions;

            Vector3 myPos = movablePositions.GetValueOrDefault(this.name);

            Vector3 nearestPos = movablePositions.GetValueOrDefault(nearestTile.name);

            // Scambia le posizioni
            transform.localPosition = nearestPos;
            nearestTile.transform.localPosition = myPos;

            // Aggiorna le posizioni nel dizionario del GridManager
            movablePositions[this.name] = nearestPos;
            movablePositions[nearestTile.name] = myPos;
        }
        else
        {
            // Se non trova nessun cubo vicino, torna alla posizione di partenza
            transform.localPosition = startTileLocalPosition;
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