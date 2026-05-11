using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CameraAdjust : MonoBehaviour
{
    [Header("Adjust Ortho Size")]
    public float orthoSizeMultiplier = 1.0f;
    public float buffer = 1.0f;
    private Camera cam;
    [Header("Debug")]
    public bool debug = false;

    private void Start()
    {
        cam = GetComponent<Camera>();
        AdjustOrthoSize();
    }
    private void FixedUpdate()
    {
        if(debug)
        AdjustOrthoSize();
    }
    private void Reset()
    {
        cam = GetComponent<Camera>();
        AdjustOrthoSize();
    }
    [ContextMenu("Adjust Ortho Size")]
    private void AdjustOrthoSize()
    {
        var (center, size) = CalculateOrthoSize();
        cam.transform.position = center;
        cam.orthographicSize = size;
    }
    private (Vector3 center, float size) CalculateOrthoSize()
    {
        var colliders = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        if (colliders.Length == 0)
            return (Vector3.zero, 1f);
        
        var bounds = colliders[0].bounds;
        for (int i = 1; i < colliders.Length; i++)
        {
            bounds.Encapsulate(colliders[i].bounds);
        }

        bounds.Expand(buffer);
        
        // Draw bounds rectangle
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3 bottomLeft = new Vector3(min.x, min.y, 0);
        Vector3 bottomRight = new Vector3(max.x, min.y, 0);
        Vector3 topRight = new Vector3(max.x, max.y, 0);
        Vector3 topLeft = new Vector3(min.x, max.y, 0);
        
        Debug.DrawLine(bottomLeft, bottomRight, Color.green);
        Debug.DrawLine(bottomRight, topRight, Color.green);
        Debug.DrawLine(topRight, topLeft, Color.green);
        Debug.DrawLine(topLeft, bottomLeft, Color.green);
        Debug.DrawLine(bounds.center, bounds.center + Vector3.right * bounds.size.x * 0.5f, Color.red);
        Debug.DrawLine(bounds.center, bounds.center + Vector3.up * bounds.size.y * 0.5f, Color.blue);
        
        var vertical = bounds.size.y;
        var horizontal = bounds.size.x * (Screen.height ) / Screen.width;
        var size = Mathf.Max(horizontal, vertical) * 0.5f;
        var center = bounds.center + new Vector3(0, 0, -10);
        return (center, size);
    }
}
