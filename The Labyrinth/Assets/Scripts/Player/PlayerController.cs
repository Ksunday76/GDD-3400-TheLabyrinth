using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace  GDD3400.Labyrinth
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Player Settings")]
        [SerializeField] private float _MoveSpeed = 10;
        [SerializeField] private float _DashDistance = 2.5f;
        [SerializeField] private float _DashCooldown = 1.5f;

        [Header("Connections")]
        [SerializeField] private Transform _GraphicsRoot;
        private Rigidbody _rigidbody;
        private InputAction _moveAction;
        private InputAction _dashAction;
        private Vector3 _moveVector;

        private bool _performDash;
        private bool _isDashing;

        private void Awake()
        {
            // Assign member variables
            _rigidbody = GetComponent<Rigidbody>();

            _moveAction = InputSystem.actions.FindAction("Move");
            _dashAction = InputSystem.actions.FindAction("Dash");
        }

        private void Update()
        {
            // Store current input move vector, remap from X/Y to X/Z
            _moveVector.x = _moveAction.ReadValue<Vector2>().x;
            _moveVector.z = _moveAction.ReadValue<Vector2>().y;

            // Face the player toward the movement direction
            if (_moveVector.magnitude > 0f) transform.forward = Vector3.Lerp(transform.forward, _moveVector, 0.2f);

            // If the dash is available and pressed this frame, perform the dash
            if (!_isDashing && _dashAction.WasPressedThisFrame()) PerformDash();
        }

        private void FixedUpdate()
        {
            // Apply the movement force
            _rigidbody.AddForce(_moveVector * _MoveSpeed * 4f, ForceMode.Force);

            // Add the dash force
            if (_performDash)
            {
                _rigidbody.AddForce(transform.forward * _DashDistance * 5f, ForceMode.Impulse);
                _performDash = false;
            }
        }

        private void PerformDash()
        {
            _performDash = true;
            _isDashing = true;

            // Call reset after the cooldown
            Invoke("ResetDash", _DashCooldown);
        }

        private void IsDashing()
        {
            // Make invulnurable when dashing
        }

        private void ResetDash()
        {
            _isDashing = false;
            _rigidbody.linearVelocity = _moveVector * _MoveSpeed;
        }
    }
}
