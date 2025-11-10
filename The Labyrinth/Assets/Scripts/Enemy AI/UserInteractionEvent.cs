using UnityEngine;
using UnityEngine.Events;

namespace GDD3400.Labyrinth
{
    public class UserInteractionEvent : MonoBehaviour
    {
        [Header("Raycast Settings")]
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask raycastLayerMask = -1;
        [SerializeField] private float maxRaycastDistance = 1000f;

        [Header("Debug Visualization")]
        [SerializeField] private bool showDebugCircle = true;
        [SerializeField] private float debugCircleRadius = 1f;
        [SerializeField] private Color debugCircleColor = Color.yellow;
        [SerializeField] private float debugCircleDuration = 2f;

        [Header("Events")]
        [SerializeField] private UnityEvent<Vector3> OnTargetLocationHit;
        [SerializeField] private UnityEvent OnNoTargetHit;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0)) // Left mouse button click
            {
                PerformRaycast();
            }
        }

        private void PerformRaycast()
        {
            if (_camera == null)
            {
                Debug.LogWarning("MouseClickRaycaster: Main camera is null, cannot perform raycast.");
                return;
            }

            // Create ray from camera through mouse position
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);

            // Perform raycast
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, maxRaycastDistance, raycastLayerMask))
            {
                Vector3 targetLocation = hit.point;

                // Draw debug circle at target location
                if (showDebugCircle)
                {
                    DebugHelpers.DrawDebugCircle(targetLocation, hit.normal, debugCircleRadius, debugCircleColor, debugCircleDuration);

                    // Draw a white X at the target location
                    float xSize = 0.25f;
                    Debug.DrawLine(targetLocation + Vector3.left * xSize, targetLocation + Vector3.right * xSize, Color.white, debugCircleDuration);
                    Debug.DrawLine(targetLocation + Vector3.up * xSize, targetLocation + Vector3.down * xSize, Color.white, debugCircleDuration);
                    Debug.DrawLine(targetLocation + Vector3.forward * xSize, targetLocation + Vector3.back * xSize, Color.white, debugCircleDuration);
                }

                // Invoke event with target location
                OnTargetLocationHit?.Invoke(targetLocation);
            }
            else
            {
                // No target hit
                OnNoTargetHit?.Invoke();
            }
        }

        // Public method to manually trigger raycast (useful for testing or other input methods)
        public void TriggerRaycast()
        {
            PerformRaycast();
        }

        // Public method to set raycast layer mask at runtime
        public void SetRaycastLayerMask(LayerMask newLayerMask)
        {
            raycastLayerMask = newLayerMask;
        }

        // Public method to enable/disable debug visualization at runtime
        public void SetDebugVisualization(bool enabled)
        {
            showDebugCircle = enabled;
        }
    }
}
