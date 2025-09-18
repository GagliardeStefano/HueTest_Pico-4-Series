using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TileDraggerXR : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rt; // RectTransform of the grabbed tile
    private RectTransform rowArea; // Row of the grabbed tile
    private RectTransform leftLimit; // tile start of row
    private RectTransform rightLimit; // tile end of row
    private GridLayoutGroup rowLayout;

    private Vector2 startPos;
    private bool isGrabbed;
    private int initialSiblingIndex;

    // --- Unity Methods ---
    void Start()
    {
        rt = GetComponent<RectTransform>();
        rowArea = transform.parent as RectTransform;
        rowLayout = rowArea?.GetComponent<GridLayoutGroup>();
        FindLimits();
    }

    void Update() { }

    // --- Drag & Drop Events ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (rt == null || rowArea == null) return;

        rt.SetParent(rowArea, true);

        startPos = rt.localPosition;
        initialSiblingIndex = rt.GetSiblingIndex();
        if (rowLayout != null) rowLayout.enabled = false;
        isGrabbed = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isGrabbed || rowArea == null) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rowArea, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            return;

        float minX = GetLimitX(leftLimit, true);
        float maxX = GetLimitX(rightLimit, false);
        float clampedX = Mathf.Clamp(localPoint.x, minX, maxX);

        rt.SetSiblingIndex(rowArea.childCount - 1); //Bring to front
        rt.localPosition = new Vector3(clampedX, startPos.y, rt.localPosition.z);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isGrabbed) return;
        isGrabbed = false;

        rt.SetSiblingIndex(initialSiblingIndex);
        TrySwapWithNearest();

        if (rowLayout != null)
        {
            rowLayout.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rowArea);
        }
    }

    // --- Utils method ---
    private void FindLimits()
    {
        leftLimit = null;
        rightLimit = null;
        foreach (Transform child in rowArea)
        {
            string n = child.name.ToLower();
            if (n.Contains("start")) leftLimit = child as RectTransform;
            else if (n.Contains("end")) rightLimit = child as RectTransform;
        }
    }

    private float GetLimitX(RectTransform limit, bool isLeft)
    {
        if (limit == null) return isLeft ? float.MinValue : float.MaxValue;
        float offset = limit.rect.width * 0.5f;
        return limit.localPosition.x + (isLeft ? offset : -offset);
    }

    private void TrySwapWithNearest()
    {
        var (nearest, dist) = FindNearestTile();
        if (nearest == null) return;

        float threshold = GetSwapThreshold();
        if (dist < threshold)
        {
            SwapSiblingIndices(nearest);
        }
    }

    private (TileDraggerXR tile, float dist) FindNearestTile()
    {
        TileDraggerXR nearest = null;
        float minDist = float.MaxValue;
        float myX = rt.localPosition.x;

        foreach (Transform sib in rowArea)
        {
            if (sib == this.transform) continue;
            var other = sib.GetComponent<TileDraggerXR>();
            if (other == null) continue;

            float d = Mathf.Abs(myX - other.rt.localPosition.x);
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
        if (leftLimit == null || rightLimit == null || rowArea.childCount <= 2)
            return float.MaxValue;
        return Mathf.Abs(rightLimit.localPosition.x - leftLimit.localPosition.x) / (rowArea.childCount - 1) / 2;
    }

    private void SwapSiblingIndices(TileDraggerXR other)
    {
        int myIndex = rt.GetSiblingIndex();
        int otherIndex = other.rt.GetSiblingIndex();

        rt.SetSiblingIndex(otherIndex);
        other.rt.SetSiblingIndex(myIndex);
    }
}