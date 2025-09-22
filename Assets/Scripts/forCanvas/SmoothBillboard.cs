using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothBillboard : MonoBehaviour
{
    public Transform cameraTransform;
    [Range(0f, 20f)] public float smoothSpeed = 8f;
    public bool lockY = true;
    public bool faceAway = true;

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 direction = cameraTransform.position - transform.position;

        if (lockY) direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(direction);
        if (faceAway)
            targetRot *= Quaternion.Euler(0, 180f, 0);

        // Smoothing
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * smoothSpeed);
    }
}
