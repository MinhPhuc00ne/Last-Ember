using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Antigravity
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterAI : MonoBehaviour
    {
        [Header("AI Settings")]
        public float patrolSpeed = 2f;
        public float chaseSpeed = 5.5f;
        public float detectionRange = 18f;
        public float attackRange = 2f;
        public float flashlightDetectionRange = 28f;

        [Header("Patrol Waypoints")]
        public Vector3[] patrolPoints;
        private int _currentWaypointIndex;

        [Header("References")]
        public Transform player;
        private NavMeshAgent _agent;
        private Animator _animator;
        private Light _playerFlashlight;

        private enum AIState { Patrol, Chase, Attack }
        private AIState _currentState = AIState.Patrol;

        void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();

            // Find player
            GameObject playerGo = GameObject.FindWithTag("Player");
            if (playerGo == null) playerGo = GameObject.Find("Player");
            if (playerGo != null) player = playerGo.transform;

            // Find player's flashlight
            GameObject flashlightGo = GameObject.Find("Flashlight");
            if (flashlightGo != null) _playerFlashlight = flashlightGo.GetComponent<Light>();

            _agent.speed = patrolSpeed;

            // Set up default patrol points if none are assigned
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                patrolPoints = new[]
                {
                    new Vector3(-20f, 0f, 10f),
                    new Vector3(20f, 0f, 10f),
                    new Vector3(-15f, 0f, -18f),
                    new Vector3(15f, 0f, -18f),
                    new Vector3(0f, 0f, -25f)
                };
            }

            GoToNextWaypoint();
        }

        void Update()
        {
            if (player == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            switch (_currentState)
            {
                case AIState.Patrol:
                    UpdateAnimator(patrolSpeed);
                    if (CanDetectPlayer(distanceToPlayer))
                    {
                        StartChasing();
                    }
                    else if (_agent.isActiveAndEnabled && _agent.isOnNavMesh && !_agent.pathPending && _agent.remainingDistance < 1f)
                    {
                        GoToNextWaypoint();
                    }
                    break;

                case AIState.Chase:
                    UpdateAnimator(_agent.velocity.magnitude);
                    if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
                    {
                        _agent.SetDestination(player.position);
                    }

                    if (distanceToPlayer <= attackRange)
                    {
                        StartAttacking();
                    }
                    else if (distanceToPlayer > detectionRange + 8f)
                    {
                        StartPatrolling();
                    }
                    break;

                case AIState.Attack:
                    UpdateAnimator(0f);
                    if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
                    {
                        _agent.SetDestination(transform.position); // Stop moving
                    }

                    Vector3 lookDir = (player.position - transform.position).normalized;
                    lookDir.y = 0f;
                    if (lookDir != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
                    }

                    if (distanceToPlayer > attackRange + 0.5f)
                    {
                        StartChasing();
                    }
                    else
                    {
                        ExecuteAttack();
                    }
                    break;
            }
        }

        private bool CanDetectPlayer(float distanceToPlayer)
        {
            if (distanceToPlayer < detectionRange) return true;

            // Flashlight detection: if player shines light at the monster, detect it from farther
            if (_playerFlashlight != null && _playerFlashlight.enabled && distanceToPlayer < flashlightDetectionRange)
            {
                Vector3 toMonster = (transform.position - _playerFlashlight.transform.position).normalized;
                float angle = Vector3.Angle(_playerFlashlight.transform.forward, toMonster);
                if (angle < _playerFlashlight.spotAngle / 2f)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(_playerFlashlight.transform.position, toMonster, out hit, flashlightDetectionRange))
                    {
                        if (hit.transform == this.transform || hit.transform.IsChildOf(this.transform))
                        {
                            Debug.Log("Antigravity: Monster detected you because of your flashlight!");
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void StartPatrolling()
        {
            _currentState = AIState.Patrol;
            _agent.speed = patrolSpeed;
            GoToNextWaypoint();
            Debug.Log("Antigravity: Monster lost interest and returned to patrol.");
        }

        private void StartChasing()
        {
            _currentState = AIState.Chase;
            _agent.speed = chaseSpeed;
            Debug.Log("Antigravity: Monster detected you and is chasing!");
        }

        private void StartAttacking()
        {
            _currentState = AIState.Attack;
            if (_animator != null)
            {
                _animator.SetTrigger("Attack"); 
            }
            Debug.Log("Antigravity: Monster is attacking you!");
        }

        private float _attackCooldown = 2f;
        private float _lastAttackTime = 0f;

        private void ExecuteAttack()
        {
            if (Time.time - _lastAttackTime > _attackCooldown)
            {
                _lastAttackTime = Time.time;
                Debug.LogWarning("Antigravity JUMPSCARE: Game Over! Reloading scene...");
                Invoke("ReloadCurrentScene", 1.5f);
            }
        }

        private void ReloadCurrentScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void GoToNextWaypoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;

            if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                _agent.SetDestination(patrolPoints[_currentWaypointIndex]);
            }
            _currentWaypointIndex = (_currentWaypointIndex + 1) % patrolPoints.Length;
        }

        private void UpdateAnimator(float speed)
        {
            if (_animator == null) return;
            _animator.SetFloat("Speed", speed);
            _animator.SetBool("IsMoving", speed > 0.1f);
        }
    }
}
