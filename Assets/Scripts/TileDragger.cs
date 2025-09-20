using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Rigidbody))]
public class TileDragger : MonoBehaviour
{
    [Header("VR Settings")]
    public float followSpeed = 20f;
    public float grabbedScaleMultiplier = 1.1f;

    private RectTransform rt;
    private RectTransform rowArea;
    private RectTransform leftLimit;
    private RectTransform rightLimit;
    private GridLayoutGroup rowLayout;

    private Vector3 startPos;
    private bool isGrabbed;
    private int initialSiblingIndex;

    private XRGrabInteractable rayInteractable;
    private Vector3 originalScale;
    private Transform grabbingController;
    private Canvas worldCanvas;
    private Camera vrCamera;

    private Vector3 targetPosition;
    private bool isAnimating = false;

    public GameObject logger;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        rowArea = transform.parent as RectTransform;
        rowLayout = rowArea?.GetComponent<GridLayoutGroup>();
        worldCanvas = GetComponentInParent<Canvas>();

        // prefer the canvas camera (important for world-space canvas)
        vrCamera = (worldCanvas != null && worldCanvas.worldCamera != null) ? worldCanvas.worldCamera : Camera.main;
        if (vrCamera == null)
            Debug.LogWarning("[TileDragger] No camera found. Some conversions may be incorrect.");

        FindLimits();
        SetupPhysicsAndCollider();
        SetupVRInteraction();

        originalScale = transform.localScale;
        startPos = rt.localPosition;
        targetPosition = transform.localPosition;
    }

    void Update()
    {
        if (isGrabbed && grabbingController != null)
        {
            UpdateTilePosition();
        }

        if (isAnimating)
        {
            // smooth localPosition movement
            rt.localPosition = Vector3.Lerp(rt.localPosition, targetPosition, Time.deltaTime * followSpeed);
            if (Vector3.Distance(rt.localPosition, targetPosition) < 0.001f)
            {
                rt.localPosition = targetPosition;
                isAnimating = false;
            }
        }
    }

    private void SetupPhysicsAndCollider()
    {
        // Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // we move by transform while grabbed (no physics forces)
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        // Collider: ensure exists and is NOT trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = false;
            // size in local space approximated using rect and lossyScale
            Vector2 rectSize = rt.rect.size;
            Vector3 worldSize = new Vector3(rectSize.x * rt.lossyScale.x, rectSize.y * rt.lossyScale.y, 0.02f);
            box.size = worldSize;
            box.center = Vector3.zero;
        }
        else
        {
            col.isTrigger = false;
        }
    }

    private void SetupVRInteraction()
    {
        rayInteractable = GetComponent<XRGrabInteractable>();
        if (rayInteractable == null)
            rayInteractable = gameObject.AddComponent<XRGrabInteractable>();

        // Subscribe
        rayInteractable.selectEntered.AddListener(OnGrabStart);
        rayInteractable.selectExited.AddListener(OnGrabEnd);
        rayInteractable.hoverEntered.AddListener(OnHoverStart);
        rayInteractable.hoverExited.AddListener(OnHoverEnd);

        // Make sure initial startPos is set
        startPos = rt.localPosition;
    }

    private void OnHoverStart(HoverEnterEventArgs args)
    {
        transform.localScale = originalScale * 1.05f;
    }

    private void OnHoverEnd(HoverExitEventArgs args)
    {
        if (!isGrabbed) transform.localScale = originalScale;
    }

    private void OnGrabStart(SelectEnterEventArgs args)
    {
        if (rt == null || rowArea == null) return;

        Debug.Log($"[TileDragger] Grab started on {gameObject.name}");

        logger.GetComponent<GrabLogger>().LogGrab(this.gameObject, true);

        startPos = rt.localPosition;
        initialSiblingIndex = rt.GetSiblingIndex();

        // Prefer interactor.attachTransform (gives tip position) if available
        Transform attach = null;
        if (args.interactorObject is XRBaseInteractor xrBi && xrBi.attachTransform != null)
            attach = xrBi.attachTransform;
        grabbingController = attach != null ? attach : args.interactorObject.transform;

        transform.localScale = originalScale * grabbedScaleMultiplier;

        if (rowLayout != null) rowLayout.enabled = false;

        // bring to front
        rt.SetSiblingIndex(rowArea.childCount - 1);

        isGrabbed = true;
        isAnimating = false; // while grabbed we follow controller
    }

    private void OnGrabEnd(SelectExitEventArgs args)
    {
        if (!isGrabbed) return;

        Debug.Log($"[TileDragger] Grab ended on {gameObject.name}");
        logger.GetComponent<GrabLogger>().LogGrab(this.gameObject, false);


        transform.localScale = originalScale;

        // Try swap; if none, animate back to slot
        TrySwapWithNearest();

        if (rowLayout != null)
        {
            rowLayout.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rowArea);
        }

        isGrabbed = false;
        grabbingController = null;
    }

    private void UpdateTilePosition()
    {
        if (grabbingController == null || rowArea == null) return;

        // Convert controller world position into rowArea local coordinates (works for WorldSpace Canvas)
        Vector3 localPoint = rowArea.InverseTransformPoint(grabbingController.position);

        float minX = GetLimitX(leftLimit, true);
        float maxX = GetLimitX(rightLimit, false);

        float clampedX = Mathf.Clamp(localPoint.x, minX, maxX);

        // Keep same Y and Z as the original slot (so we don't change vertical layout)
        targetPosition = new Vector3(clampedX, startPos.y, startPos.z);

        // Move immediately (smoothed)
        rt.localPosition = Vector3.Lerp(rt.localPosition, targetPosition, Time.deltaTime * followSpeed);
    }

    private void FindLimits()
    {
        leftLimit = null;
        rightLimit = null;

        if (rowArea == null) return;

        foreach (Transform child in rowArea)
        {
            string n = child.name.ToLower();
            if (n.Contains("start"))
            {
                leftLimit = child as RectTransform;
                Debug.Log($"Found left limit: {child.name} at {leftLimit.localPosition.x}");
            }
            else if (n.Contains("end"))
            {
                rightLimit = child as RectTransform;
                Debug.Log($"Found right limit: {child.name} at {rightLimit.localPosition.x}");
            }
        }

        if (leftLimit == null) Debug.LogWarning($"Left limit not found for {gameObject.name}");
        if (rightLimit == null) Debug.LogWarning($"Right limit not found for {gameObject.name}");
    }

    private float GetLimitX(RectTransform limit, bool isLeft)
    {
        if (limit == null) return isLeft ? -1000f : 1000f;
        float offset = limit.rect.width * 0.5f * limit.lossyScale.x;
        float limitX = limit.localPosition.x + (isLeft ? offset + 0.01f : -offset - 0.01f);
        return limitX;
    }

    private void TrySwapWithNearest()
    {
        var (nearest, dist) = FindNearestTile();
        if (nearest == null)
        {
            Debug.Log("[TileDragger] No nearest tile found; returning to original slot");
            // animate back to original
            targetPosition = startPos;
            isAnimating = true;
            rt.SetSiblingIndex(initialSiblingIndex);
            return;
        }

        float threshold = GetSwapThreshold();
        Debug.Log($"Distance to nearest {nearest.gameObject.name}: {dist}, threshold: {threshold}");

        if (dist < threshold)
        {
            SwapSiblingIndices(nearest);
            Debug.Log($"Swapped {gameObject.name} with {nearest.gameObject.name}");
        }
        else
        {
            Debug.Log($"Distance {dist} too far from nearest tile {nearest.gameObject.name}; returning");
            targetPosition = startPos;
            isAnimating = true;
            rt.SetSiblingIndex(initialSiblingIndex);
        }
    }

    private (TileDragger tile, float dist) FindNearestTile()
    {
        TileDragger nearest = null;
        float minDist = float.MaxValue;
        float myX = rt.localPosition.x;

        foreach (Transform sib in rowArea)
        {
            if (sib == this.transform) continue;
            if (sib.name.ToLower().Contains("start") || sib.name.ToLower().Contains("end")) continue;

            var other = sib.GetComponent<TileDragger>();
            if (other == null) continue;

            float otherX = other.rt.localPosition.x;
            float d = Mathf.Abs(myX - otherX);

            if (d < minDist)
            {
                minDist = d;
                nearest = other;
            }
        }

        return (nearest, minDist);
    }

    private float GetSwapThreshold()
    {
        if (leftLimit == null || rightLimit == null) return 0.5f; // in local units

        float totalWidth = Mathf.Abs(rightLimit.localPosition.x - leftLimit.localPosition.x);
        int movableTiles = 0;
        foreach (Transform child in rowArea)
        {
            if (!child.name.ToLower().Contains("start") && !child.name.ToLower().Contains("end") && child.GetComponent<TileDragger>() != null)
                movableTiles++;
        }
        if (movableTiles <= 1) return 0.5f;
        return (totalWidth / movableTiles) * 0.4f;
    }

    private void SwapSiblingIndices(TileDragger other)
    {
        int myIndex = rt.GetSiblingIndex();
        int otherIndex = other.rt.GetSiblingIndex();

        rt.SetSiblingIndex(otherIndex);
        other.rt.SetSiblingIndex(myIndex);

        // After swap, force layout rebuild so UI updates correctly
        if (rowLayout != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rowArea);
    }

    private void OnDestroy()
    {
        if (rayInteractable != null)
        {
            rayInteractable.selectEntered.RemoveListener(OnGrabStart);
            rayInteractable.selectExited.RemoveListener(OnGrabEnd);
            rayInteractable.hoverEntered.RemoveListener(OnHoverStart);
            rayInteractable.hoverExited.RemoveListener(OnHoverEnd);
        }
    }
}
