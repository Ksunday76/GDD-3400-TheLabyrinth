using UnityEngine;


    public class DebugHelpers : MonoBehaviour
    {
        public static void DrawDebugCircle(Vector3 position, Vector3 normal, float debugCircleRadius, Color debugCircleColor, float debugCircleDuration)
        {
            // Create a circle perpendicular to the hit surface normal
            Vector3 forward = Vector3.Cross(normal, Vector3.right).normalized;
            if (forward.magnitude < 0.1f)
            {
                forward = Vector3.Cross(normal, Vector3.up).normalized;
            }
            Vector3 right = Vector3.Cross(forward, normal).normalized;
            
            // Draw circle using Debug.DrawLine
            int segments = 32;
            float angleStep = 360f / segments;
            
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep * Mathf.Deg2Rad;
                float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
                
                Vector3 point1 = position + (forward * Mathf.Cos(angle1) + right * Mathf.Sin(angle1)) * debugCircleRadius;
                Vector3 point2 = position + (forward * Mathf.Cos(angle2) + right * Mathf.Sin(angle2)) * debugCircleRadius;
                
                Debug.DrawLine(point1, point2, debugCircleColor, debugCircleDuration);
            }
        }

    public static void DrawGizmoCircle(Vector3 position, Vector3 normal, float gizmoCircleRadius, Color gizmoCircleColor)
    {
        // Create a circle perpendicular to the hit surface normal
        Vector3 forward = Vector3.Cross(normal, Vector3.right).normalized;
        if (forward.magnitude < 0.1f)
        {
            forward = Vector3.Cross(normal, Vector3.up).normalized;
        }
        Vector3 right = Vector3.Cross(forward, normal).normalized;
        
        // Draw circle using Gizmos.DrawLine
        int segments = 32;
        float angleStep = 360f / segments;
        
        Gizmos.color = gizmoCircleColor;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
            
            Vector3 point1 = position + (forward * Mathf.Cos(angle1) + right * Mathf.Sin(angle1)) * gizmoCircleRadius;
            Vector3 point2 = position + (forward * Mathf.Cos(angle2) + right * Mathf.Sin(angle2)) * gizmoCircleRadius;
            
            Gizmos.DrawLine(point1, point2);
        }
    }
    }

