using UnityEngine;

namespace Antigravity
{
    public class ShadowFigure : MonoBehaviour
    {
        [Header("Stalking Settings")]
        public float disappearDistance = 12f;
        public Vector3[] spawnPoints;

        [Header("Window Stalking")]
        public Transform houseTrans;
        public Vector3[] localWindows;
        public Vector3[] localWindowSpawns;
        public float windowTriggerDistance = 3.5f;

        private Transform _player;
        private DayNightCycle _dayNightCycle;
        private MeshRenderer _renderer;
        private bool _isNight;
        private bool _isSpawned;
        private bool _isSpawnedAtWindow;

        void Start()
        {
            _renderer = GetComponent<MeshRenderer>();
            
            // Find player
            GameObject playerGo = GameObject.FindWithTag("Player");
            if (playerGo == null) playerGo = GameObject.Find("Player");
            if (playerGo != null) _player = playerGo.transform;

            // Find house
            GameObject houseGo = GameObject.Find("House");
            if (houseGo != null) houseTrans = houseGo.transform;

            // Find day night cycle
            _dayNightCycle = FindFirstObjectByType<DayNightCycle>();

            // Start hidden
            SetSpawned(false);
        }

        void Update()
        {
            if (_dayNightCycle == null || _player == null) return;

            // Check if it is night: 0.73 to 1.0, or 0.0 to 0.22
            bool nightNow = (_dayNightCycle.timeOfDay > 0.73f || _dayNightCycle.timeOfDay < 0.22f);

            if (nightNow != _isNight)
            {
                _isNight = nightNow;
                if (_isNight)
                {
                    OnNightStart();
                }
                else
                {
                    OnDayStart();
                }
            }

            if (_isNight)
            {
                CheckWindows();
            }

            if (_isSpawned)
            {
                // Face the player (only rotating around Y axis so it doesn't tilt)
                Vector3 targetPos = new Vector3(_player.position.x, transform.position.y, _player.position.z);
                transform.LookAt(targetPos);

                // Distance check to disappear
                float distance = Vector3.Distance(transform.position, _player.position);
                if (distance < disappearDistance)
                {
                    Disappear();
                }
            }
        }

        private void CheckWindows()
        {
            if (houseTrans == null || localWindows == null || localWindowSpawns == null) return;

            bool playerNearAnyWindow = false;

            for (int i = 0; i < localWindows.Length; i++)
            {
                Vector3 worldWindowPos = houseTrans.TransformPoint(localWindows[i]);
                float dist2D = Vector2.Distance(new Vector2(_player.position.x, _player.position.z), new Vector2(worldWindowPos.x, worldWindowPos.z));

                if (dist2D < windowTriggerDistance)
                {
                    playerNearAnyWindow = true;

                    // Teleport to outside this window if not already spawned there
                    Vector3 targetSpawn = houseTrans.TransformPoint(localWindowSpawns[i]);
                    float distanceToCurrentSpawn = Vector3.Distance(transform.position, targetSpawn);

                    if (!_isSpawned || !_isSpawnedAtWindow || distanceToCurrentSpawn > 1f)
                    {
                        float groundY = targetSpawn.y;
                        RaycastHit hit;
                        if (Physics.Raycast(new Vector3(targetSpawn.x, 100f, targetSpawn.z), Vector3.down, out hit, 200f))
                        {
                            groundY = hit.point.y;
                        }

                        transform.position = new Vector3(targetSpawn.x, groundY + 1.1f, targetSpawn.z);
                        SetSpawned(true);
                        _isSpawnedAtWindow = true;
                        Debug.Log("Antigravity: Shadow Figure is watching you through the window...");
                    }
                    break; // Only trigger one window at a time
                }
            }

            // If player was near a window but is no longer near any window, vanish
            if (!playerNearAnyWindow && _isSpawnedAtWindow)
            {
                Disappear();
            }
        }

        private void OnNightStart()
        {
            _isSpawnedAtWindow = false;

            // 75% chance to spawn behind a tree tonight
            if (Random.value < 0.75f && spawnPoints != null && spawnPoints.Length > 0)
            {
                // Pick a random spawn point
                Vector3 rawPos = spawnPoints[Random.Range(0, spawnPoints.Length)];
                
                // Align with terrain height dynamically
                float groundY = rawPos.y;
                RaycastHit hit;
                if (Physics.Raycast(new Vector3(rawPos.x, 100f, rawPos.z), Vector3.down, out hit, 200f))
                {
                    groundY = hit.point.y;
                }
                
                transform.position = new Vector3(rawPos.x, groundY + 1.1f, rawPos.z);
                
                SetSpawned(true);
                Debug.Log("Antigravity: Shadow Figure spawned behind a tree to watch you...");
            }
        }

        private void OnDayStart()
        {
            SetSpawned(false);
        }

        private void Disappear()
        {
            if (!_isSpawned) return;
            
            SetSpawned(false);
            _isSpawnedAtWindow = false;
            Debug.Log("Antigravity: The shadow figure vanished...");
        }

        private void SetSpawned(bool spawned)
        {
            _isSpawned = spawned;
            if (_renderer != null)
            {
                _renderer.enabled = spawned;
            }
        }
    }
}
