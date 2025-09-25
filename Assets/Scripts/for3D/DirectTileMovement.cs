using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class DirectTileMovement : MonoBehaviour
{
    private Vector3 startTileLocalPosition;
    private Vector3 startControllerWorldPosition;
    private XRGrabInteractable grabInteractable;
    private Transform controllerTransform;
    private bool isGrabbed = false;

    // Riferimento al GridManager per ottenere il spacing e i limiti
    private Transform gridParent;
    private float spacing = 1.25f;

    [Header("Movement Settings")]
    public float movementMultiplier = 50f; // Amplifica il movimento del controller

    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        gridParent = transform.parent; // Il GridManager

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

        // NON fare snap automatico, lascia il tassello dove è stato rilasciato
        // Se vuoi comunque uno snap, decommentalo:
        // SnapToGrid();
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

            // Opzionale: Limita il movimento all'interno della griglia
            // newLocalPosition.x = Mathf.Clamp(newLocalPosition.x, 0.3f, 3.9f); // Esempio per colonne 1-13

            // Imposta la posizione locale
            transform.localPosition = newLocalPosition;
        }
    }

    private void SnapToGrid()
    {
        Vector3 localPos = transform.localPosition;

        // Trova la colonna più vicina
        int nearestCol = Mathf.RoundToInt(localPos.x / spacing);
        nearestCol = Mathf.Clamp(nearestCol, 1, 8); // Limita alle colonne mobili

        // Snap alla posizione della griglia
        transform.localPosition = new Vector3(
            nearestCol * spacing,
            localPos.y,
            localPos.z
        );
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
