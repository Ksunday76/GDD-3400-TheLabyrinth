using UnityEngine;

namespace GDD3400.Labyrinth
{
    public class TrackingCamera : MonoBehaviour
    {
        [SerializeField] Transform _Target;

        private Vector3 _offset;
        [SerializeField] private float _smoothSpeed = 0.125f;

        private void Start()
        {
            if (_Target != null)
            {
                _offset = transform.position - _Target.transform.position;
            }
        }

        private void LateUpdate()
        {
            if (_Target != null)
            {
                Vector3 desiredPosition = _Target.transform.position + _offset;
                Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed);
                transform.position = smoothedPosition;
            }
        }
    }
}
