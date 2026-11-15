using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Core
{
    /// <summary>
    /// Creates copies of a prefab over time: enemies, falling rocks, pickups.
    ///
    /// The Spawner keeps a pool. When one of its copies has been switched off
    /// (by Health, Lifetime or Damage On Touch), the Spawner reuses that copy
    /// before making a new one. Reusing is much faster than creating and destroying.
    /// </summary>
    [AddComponentMenu("Collin Game/Core/Spawner")]
    public class Spawner : MonoBehaviour
    {
        [Tooltip("The prefab to make copies of.")]
        [SerializeField] GameObject prefab;

        [Tooltip("Optional. Places to spawn at; one is picked at random each time. Leave empty to spawn at this object's position.")]
        [SerializeField] Transform[] spawnPoints;

        [Header("Timing")]

        [Tooltip("Start spawning as soon as the scene starts.")]
        [SerializeField] bool startAutomatically = true;

        [Tooltip("Seconds to wait before the first spawn.")]
        [SerializeField, Min(0f)] float startDelay = 1f;

        [Tooltip("Seconds between spawns.")]
        [SerializeField, Min(0.05f)] float interval = 2f;

        [Header("Limits")]

        [Tooltip("The most copies that can be in the scene at once. 0 means no limit.")]
        [SerializeField, Min(0)] int maxAtOnce = 5;

        [Tooltip("Stop after spawning this many in total. 0 means never stop.")]
        [SerializeField, Min(0)] int totalToSpawn = 0;

        [Header("Events")]

        [Tooltip("Runs each time something is spawned.")]
        [SerializeField] UnityEvent onSpawned;

        [Tooltip("Runs when the total has been reached and spawning stops.")]
        [SerializeField] UnityEvent onFinished;

        readonly List<GameObject> copies = new List<GameObject>();
        float timer;
        bool running;

        /// <summary>How many have been spawned since spawning started.</summary>
        public int SpawnedCount { get; private set; }

        /// <summary>True while the Spawner is working.</summary>
        public bool IsRunning => running;

        void Start()
        {
            if (prefab == null)
            {
                Debug.LogError($"Spawner \"{name}\": Prefab is empty. Drag a prefab from the Project window into the Prefab field.", this);
                return;
            }

            if (startAutomatically)
            {
                StartSpawning();
            }
        }

        void Update()
        {
            if (!running)
            {
                return;
            }

            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                timer = interval;
                SpawnOne();
            }
        }

        /// <summary>Starts spawning from the beginning.</summary>
        public void StartSpawning()
        {
            if (prefab == null)
            {
                return;
            }

            SpawnedCount = 0;
            timer = startDelay;
            running = true;
        }

        /// <summary>Stops spawning. Copies already in the scene stay.</summary>
        public void StopSpawning()
        {
            running = false;
        }

        /// <summary>Spawns one copy right now, if the limits allow it.</summary>
        public void SpawnOne()
        {
            if (prefab == null)
            {
                return;
            }

            // Forget copies that were destroyed, and look for one that can be reused.
            GameObject reusable = null;
            int activeCount = 0;

            for (int i = copies.Count - 1; i >= 0; i--)
            {
                if (copies[i] == null)
                {
                    copies.RemoveAt(i);
                }
                else if (copies[i].activeSelf)
                {
                    activeCount++;
                }
                else
                {
                    reusable = copies[i];
                }
            }

            if (maxAtOnce > 0 && activeCount >= maxAtOnce)
            {
                return;
            }

            Transform point = PickSpawnPoint();

            if (reusable != null)
            {
                reusable.transform.SetPositionAndRotation(point.position, point.rotation);
                reusable.SetActive(true);
            }
            else
            {
                copies.Add(Instantiate(prefab, point.position, point.rotation));
            }

            SpawnedCount++;
            onSpawned?.Invoke();

            if (totalToSpawn > 0 && SpawnedCount >= totalToSpawn)
            {
                running = false;
                onFinished?.Invoke();
            }
        }

        Transform PickSpawnPoint()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Transform choice = spawnPoints[Random.Range(0, spawnPoints.Length)];

                if (choice != null)
                {
                    return choice;
                }
            }

            return transform;
        }
    }
}
