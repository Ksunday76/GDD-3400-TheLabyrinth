using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // for restart

namespace GDD3400.Labyrinth
{
    // IMPORTANT: I Used ChatGPT to polished and clean up the code.
    // Also used Chatgpt to add in additional comments throughout the code
    // so it is easier to follow along with the logic.

    [RequireComponent(typeof(Rigidbody))]
    public class EnemyAgent : MonoBehaviour
    {
        // Simple set of modes the enemy can be in
        private enum EnemyState { Patrol, Pursue, Search }

        [Header("Scene References")]
        // Handles node graph for pathfinding
        [SerializeField] private LevelManager _levelManager;
        // Reference to the player (auto-found if not set)
        [SerializeField] private Transform _player;

        [Header("Movement")]
        // How fast the enemy can rotate (degrees per second)
        [SerializeField] private float _TurnRateDegPerSec = 540f;
        // Movement speed while chasing a target
        [SerializeField] private float _MaxSpeed = 3.0f;
        // How close we get before we "arrive" at a target
        [SerializeField] private float _StoppingDistance = 1.5f;

        [Tooltip("Distance from destination before we abandon the path and go direct.")]
        // Start ignoring the path and head straight near the end
        [SerializeField] private float _LeavingPathDistance = 2f;

        [Tooltip("If destination is farther than this, we request a path.")]
        // Only use A* if the target is at least this far away
        [SerializeField] private float _MinimumPathDistance = 6f;

        [Header("Perception")]
        // Vision cone angle
        [SerializeField, Range(10, 180)] private float _FieldOfViewDeg = 90f;
        // How far the enemy can see
        [SerializeField] private float _SightDistance = 8f;
        // Simple radius for hearing the player
        [SerializeField] private float _HearingRange = 3.5f;
        // Layer used to block line-of-sight
        [SerializeField] private LayerMask _wallLayer;

        [Header("Behavior Timing")]
        // How often we refresh the path while chasing
        [SerializeField] private float _PursueRepathEvery = 0.25f;
        // How long we search after losing sight
        [SerializeField] private float _SearchDuration = 4f;
        // How often we refresh path while searching
        [SerializeField] private float _SearchRepathEvery = 0.75f;

        [Header("Patrol")]
        // Waypoints the enemy walks between in Patrol
        [SerializeField] private Transform[] _patrolPoints;
        // If true, wrap back to 0 after last point
        [SerializeField] private bool _loopPatrol = true;

        [Header("Alert Audio")]
        [Tooltip("AudioSource that will loop the alarm sound while pursuing.")]
        // Alarm siren that plays only in Pursue state
        [SerializeField] private AudioSource _alarmSource;

        [Header("Vision Cone Debug")]
        // Color used for vision gizmo
        [SerializeField] private Color _visionColor = Color.yellow;
        // How many lines to draw in the cone
        [SerializeField] private int _visionSegments = 16;

        [Header("Catch / Restart")]
        // Tag used to detect the player on collision
        [SerializeField] private string _playerTag = "Player";

        [Header("Debug")]
        // If true, draws debug lines for the A* path
        [SerializeField] private bool DEBUG_SHOW_PATH = true;

        // Global on/off switch for this enemy
        private bool _isActive = true;
        public bool IsActive { get => _isActive; set => _isActive = value; }

        // Current behavior state
        private EnemyState _state = EnemyState.Patrol;

        // Current movement velocity we apply to the rigidbody
        private Vector3 _velocity;

        // Where we are currently steering towards (node or direct point)
        private Vector3 _floatingTarget;

        // Final destination we want to reach (like player or patrol point)
        private Vector3 _destinationTarget;

        // Current path from A* (list of nodes to follow)
        private List<PathNode> _path;

        // Physics body for movement
        private Rigidbody _rb;

        // Index into the patrol points array
        private int _patrolIndex = 0;

        // Timer used to know when to recalc paths in Pursue/Search
        private float _repathTimer = 0f;

        // Timer used to know when we stop searching and go back to patrol
        private float _searchTimer = 0f;

        // Last position where we saw or heard the player
        private Vector3 _lastSeenPos;

        // --- Simple anti-stuck tracking ---

        // Where we were last time we checked
        private Vector3 _lastPosition;

        // Counts up between stuck checks
        private float _stuckTimer = 0f;

        // How often we check if we're stuck
        private const float STUCK_CHECK_INTERVAL = 1.0f;

        // If we moved less than this, we might be stuck
        private const float STUCK_DISTANCE_EPSILON = 0.1f;

        void OnValidate()
        {
            // Make sure this value never drops below 1, to avoid weird behavior
            if (_LeavingPathDistance < 1f) _LeavingPathDistance = 1f;
        }

        public void Awake()
        {
            // Cache the rigidbody and set it up like a hovering drone
            _rb = GetComponent<Rigidbody>();
            // No gravity so it "floats"
            _rb.useGravity = false;
            // We control rotation manually
            _rb.constraints = RigidbodyConstraints.FreezeRotation;

            // If no wall layer is set in the inspector, try to grab the "Walls" layer by name
            if (_wallLayer == 0)
                _wallLayer = LayerMask.GetMask("Walls");
        }

        public void Start()
        {
            // Try to find the LevelManager if it wasn't wired in
            if (_levelManager == null)
                _levelManager = FindAnyObjectByType<LevelManager>();

            if (_levelManager == null)
                Debug.LogError("Unable To Find Level Manager");

            // Try to find the player by tag if no reference was assigned
            if (_player == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag(_playerTag);
                if (p != null) _player = p.transform;
                else Debug.LogError($"EnemyAgent: No object with tag '{_playerTag}' found. Player reference is null.");
            }

            // Make sure the alarm is set up to be controlled by code only
            if (_alarmSource != null)
            {
                // We want this to loop like a siren
                _alarmSource.loop = true;
                // Don't start on scene load
                _alarmSource.playOnAwake = false;
            }

            // If we have patrol points, start at the first one
            if (_patrolPoints != null && _patrolPoints.Length > 0)
            {
                _patrolIndex = 0;
                SetDestinationTarget(_patrolPoints[_patrolIndex].position);
                SetState(EnemyState.Patrol);
            }
            else
            {
                // No patrol points means just idle at our starting position
                _destinationTarget = transform.position;
                _floatingTarget = transform.position;
                SetState(EnemyState.Patrol);
            }

            // Remember where we started for the stuck check
            _lastPosition = transform.position;
        }

        public void Update()
        {
            // Early out if this enemy is disabled
            if (!_isActive) return;

            // Sense the world: can we see or hear the player this frame?
            bool saw = CanSeePlayer();
            bool heard = CanHearPlayer();

            // If we see the player, go into chase mode
            if (saw)
            {
                // Remember where we saw them
                _lastSeenPos = _player.position;
                // Reset search timer for later
                _searchTimer = _SearchDuration;

                if (_state != EnemyState.Pursue)
                {
                    SetState(EnemyState.Pursue);
                    // Force an immediate path update
                    _repathTimer = 0f;
                }
            }
            // If we didn't see but we heard them, and we're just patrolling, go investigate
            else if (heard && _state == EnemyState.Patrol)
            {
                _lastSeenPos = _player.position;
                // Shorter search when it's just a sound
                _searchTimer = _SearchDuration * 0.5f;
                SetState(EnemyState.Search);
                _repathTimer = 0f;
            }

            // Based on the current state, decide what to do next
            DecisionMaking();
        }

        // Handles switching between Patrol, Pursue, Search
        // Also hooks in things like the alarm sound
        private void SetState(EnemyState newState)
        {
            // If we're already in this state, do nothing
            if (_state == newState) return;

            Debug.Log("Enemy state changed from " + _state + " to " + newState);

            EnemyState oldState = _state;
            _state = newState;

            // Play or stop the alarm depending on the state
            if (_alarmSource != null)
            {
                if (_state == EnemyState.Pursue)
                {
                    // Only start if it's not already going
                    if (!_alarmSource.isPlaying)
                        _alarmSource.Play();
                }
                else
                {
                    // Any non-pursue state turns the alarm off
                    if (_alarmSource.isPlaying)
                        _alarmSource.Stop();
                }
            }
        }

        // Core brain logic: choose behavior based on the current state
        private void DecisionMaking()
        {
            switch (_state)
            {
                case EnemyState.Patrol:
                    {
                        // --- PATROL: walk between waypoints in order ---

                        // If we don't have any patrol points, there's nothing to do
                        if (_patrolPoints == null || _patrolPoints.Length == 0)
                            break;

                        // Make sure the index stays in range
                        if (_patrolIndex < 0 || _patrolIndex >= _patrolPoints.Length)
                            _patrolIndex = 0;

                        // If we don't have a path yet, ask for one to the current patrol point
                        if (_path == null || _path.Count == 0)
                        {
                            SetDestinationTarget(_patrolPoints[_patrolIndex].position);
                        }

                        // Follow the path if we have one
                        if (_path != null && _path.Count > 0)
                        {
                            // Close enough to the final destination? Drop the path and go direct
                            if (Vector3.Distance(transform.position, _destinationTarget) < _LeavingPathDistance)
                            {
                                _path = null;
                                _floatingTarget = _destinationTarget;
                            }
                            else
                            {
                                PathFollowing();
                            }
                        }

                        // If we reached the current patrol destination, move on to the next point
                        if (Vector3.Distance(transform.position, _destinationTarget) <= _StoppingDistance + 0.1f)
                        {
                            _patrolIndex++;

                            // Loop or clamp index based on setting
                            if (_patrolIndex >= _patrolPoints.Length)
                            {
                                if (_loopPatrol)
                                    _patrolIndex = 0;
                                else
                                    _patrolIndex = _patrolPoints.Length - 1;
                            }

                            SetDestinationTarget(_patrolPoints[_patrolIndex].position);
                        }

                        break;
                    }

                case EnemyState.Pursue:
                    {
                        // --- PURSUE: actively chase the player's current position ---

                        // Count down until it's time to refresh the path
                        _repathTimer -= Time.deltaTime;
                        if (_repathTimer <= 0f)
                        {
                            _repathTimer = _PursueRepathEvery;
                            if (_player != null)
                                SetDestinationTarget(_player.position);
                        }

                        // Follow the path if we have one
                        if (_path != null && _path.Count > 0)
                        {
                            if (Vector3.Distance(transform.position, _destinationTarget) < _LeavingPathDistance)
                            {
                                _path = null;
                                _floatingTarget = _destinationTarget;
                            }
                            else
                            {
                                PathFollowing();
                            }
                        }

                        // If we can't see the player anymore, switch to search mode
                        if (!CanSeePlayer())
                        {
                            SetState(EnemyState.Search);
                            _searchTimer = _SearchDuration;
                            _repathTimer = 0f;
                            SetDestinationTarget(_lastSeenPos);
                        }
                        break;
                    }

                case EnemyState.Search:
                    {
                        // --- SEARCH: move to the last known position and hang around a bit ---

                        // Tick down the search timer
                        _searchTimer -= Time.deltaTime;

                        // Refresh path toward last seen position occasionally
                        _repathTimer -= Time.deltaTime;
                        if (_repathTimer <= 0f)
                        {
                            _repathTimer = _SearchRepathEvery;
                            SetDestinationTarget(_lastSeenPos);
                        }

                        // Follow the path if we have one
                        if (_path != null && _path.Count > 0)
                        {
                            if (Vector3.Distance(transform.position, _destinationTarget) < _LeavingPathDistance)
                            {
                                _path = null;
                                _floatingTarget = _destinationTarget;
                            }
                            else
                            {
                                PathFollowing();
                            }
                        }

                        // Check if we've basically arrived at the last known spot
                        bool reachedLastSeen =
                            Vector3.Distance(transform.position, _lastSeenPos) <= _StoppingDistance + 0.1f;

                        // If we got there and the search time is up, go back to patrol
                        if (reachedLastSeen && _searchTimer <= 0f)
                        {
                            SetState(EnemyState.Patrol);
                            if (_patrolPoints != null && _patrolPoints.Length > 0)
                                SetDestinationTarget(_patrolPoints[_patrolIndex].position);
                        }
                        break;
                    }
            }
        }

        // --- Perception ---

        // Check if the enemy can currently see the player
        private bool CanSeePlayer()
        {
            if (_player == null) return false;

            // Too far away to see
            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist > _SightDistance) return false;

            // Check if the player is inside the vision cone angle
            Vector3 to = _player.position - transform.position;
            to.y = 0f;
            float angle = Vector3.Angle(transform.forward, to);
            if (angle > _FieldOfViewDeg * 0.5f) return false;

            // Raycast to see if a wall is blocking the view
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Vector3 dir = (_player.position - origin).normalized;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, _SightDistance))
            {
                // If we hit a wall first, line of sight is blocked
                if (((1 << hit.collider.gameObject.layer) & _wallLayer.value) != 0)
                    return false;
            }

            // If we got here, we can see the player
            _lastSeenPos = _player.position;
            return true;
        }

        // Simple hearing check: just radius-based
        private bool CanHearPlayer()
        {
            if (_player == null) return false;

            // Ignore height difference, only care about XZ distance
            Vector3 a = transform.position; a.y = 0f;
            Vector3 b = _player.position; b.y = 0f;

            return Vector3.Distance(a, b) <= _HearingRange;
        }

        // --- Path Following ---

        // Picks which node in the path to steer towards
        private void PathFollowing()
        {
            // Find the closest node to where we are now
            int closestNodeIndex = GetClosestNode();
            int nextNodeIndex = closestNodeIndex + 1;

            PathNode targetNode = null;

            // If there is a "next" node, aim for that, otherwise stick to the closest
            if (nextNodeIndex < _path.Count) targetNode = _path[nextNodeIndex];
            else targetNode = _path[closestNodeIndex];

            // Use this node as our current steering target
            _floatingTarget = targetNode.transform.position;
        }

        // Sets a new high-level destination, and decides whether to use A* or go direct
        public void SetDestinationTarget(Vector3 destination)
        {
            _destinationTarget = destination;

            float dist = Vector3.Distance(transform.position, destination);

            // If it's far away, try to use the node graph and A*
            if (dist > _MinimumPathDistance)
            {
                PathNode startNode = _levelManager != null ? _levelManager.GetNode(transform.position) : null;
                PathNode endNode = _levelManager != null ? _levelManager.GetNode(destination) : null;

                // If the nodes can't be found (maybe off-grid), just walk straight there
                if (startNode == null || endNode == null)
                {
                    Debug.LogWarning("EnemyAgent: Could not find path nodes for destination, moving directly.");
                    _path = null;
                    _floatingTarget = destination;
                    return;
                }

                // Ask the Pathfinder for a list of nodes to follow
                _path = Pathfinder.FindPath(startNode, endNode);

                // Draw debug lines so we can see the path in the Scene view
                if (DEBUG_SHOW_PATH && _path != null)
                    StartCoroutine(DrawPathDebugLines(_path));
            }
            else
            {
                // If it's close enough, skip A* and just move straight to it
                _path = null;
                _floatingTarget = destination;
            }
        }

        // Finds the path node that is closest to the enemy's current position
        private int GetClosestNode()
        {
            int closestNodeIndex = 0;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < _path.Count; i++)
            {
                float d = Vector3.Distance(transform.position, _path[i].transform.position);
                if (d < closestDistance)
                {
                    closestDistance = d;
                    closestNodeIndex = i;
                }
            }
            return closestNodeIndex;
        }

        // --- Movement ---

        private void FixedUpdate()
        {
            if (!_isActive) return;

            // Green line shows where the enemy is trying to go right now
            Debug.DrawLine(this.transform.position, _floatingTarget, Color.green);

            // If we have somewhere to go and we're not close enough yet, move towards it
            if (_floatingTarget != Vector3.zero &&
                Vector3.Distance(transform.position, _floatingTarget) > _StoppingDistance)
            {
                Vector3 dir = (_floatingTarget - transform.position).normalized;
                _velocity = dir * _MaxSpeed;

                // Rotate to face the direction we're moving
                if (_velocity.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(_velocity);
                    float step = _TurnRateDegPerSec * Time.fixedDeltaTime;
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, step);
                }
            }
            else
            {
                // If we're basically at the target, slowly bleed off velocity
                _velocity *= 0.95f;
            }

            // Apply movement to the rigidbody
            _rb.linearVelocity = _velocity;

            // --- Simple "stuck" handling ---

            _stuckTimer += Time.fixedDeltaTime;
            if (_stuckTimer >= STUCK_CHECK_INTERVAL)
            {
                float moved = Vector3.Distance(transform.position, _lastPosition);

                // If we haven't really moved but we still have somewhere to go,
                // assume we're stuck and try recalculating the path
                if (moved < STUCK_DISTANCE_EPSILON &&
                    _floatingTarget != Vector3.zero &&
                    Vector3.Distance(transform.position, _floatingTarget) > _StoppingDistance * 1.5f)
                {
                    Debug.Log("EnemyAgent: Detected stuck, recalculating path to current destination.");
                    SetDestinationTarget(_destinationTarget);
                }

                _lastPosition = transform.position;
                _stuckTimer = 0f;
            }
        }

        // --- Catch Player / Restart Level ---

        // If we physically collide with the player, restart the scene
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.CompareTag(_playerTag))
            {
                RestartLevel();
            }
        }

        // Also handle trigger volumes if the enemy uses triggers instead of solid colliders
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(_playerTag))
            {
                RestartLevel();
            }
        }

        // Reload the current scene from scratch
        private void RestartLevel()
        {
            Scene current = SceneManager.GetActiveScene();
            SceneManager.LoadScene(current.buildIndex);
        }

        // --- Debug drawing ---

        // Draws the A* path as red lines for a short time
        private IEnumerator DrawPathDebugLines(List<PathNode> path)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                Debug.DrawLine(path[i].transform.position, path[i + 1].transform.position, Color.red, 3.5f);
                yield return new WaitForSeconds(0.05f);
            }
        }

        // Draw a simple vision cone in the Scene view so we can tweak FOV and distance
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _visionColor;

            Vector3 origin = transform.position + Vector3.up * 0.1f;
            float halfFov = _FieldOfViewDeg * 0.5f;
            float step = _FieldOfViewDeg / Mathf.Max(1, _visionSegments);

            Vector3 prevDir = Quaternion.Euler(0f, -halfFov, 0f) * transform.forward;

            for (int i = 1; i <= _visionSegments; i++)
            {
                float angle = -halfFov + step * i;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * transform.forward;

                Gizmos.DrawLine(origin, origin + prevDir.normalized * _SightDistance);
                Gizmos.DrawLine(origin, origin + dir.normalized * _SightDistance);
                prevDir = dir;
            }
        }
    }
}
