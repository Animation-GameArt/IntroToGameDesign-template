using System;
using System.Collections;
using System.Collections.Generic;
using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace CollinGame.Kits.TopDownShooter
{
    /// <summary>
    /// Sends enemies in waves. Each wave lists which enemies come and how many;
    /// the next wave starts once every enemy of the current one is gone. After
    /// the last wave, the arena is cleared.
    ///
    /// Enemies appear at the Spawn Points, chosen at random. Put the points
    /// just outside the camera's view, so enemies walk in from the edges.
    /// </summary>
    [AddComponentMenu("Collin Game/Top-Down Shooter Kit/Wave Spawner 2D")]
    public class WaveSpawner2D : MonoBehaviour
    {
        /// <summary>A group of one kind of enemy in a wave.</summary>
        [Serializable]
        public class Group
        {
            [Tooltip("The enemy prefab.")]
            public GameObject enemy;

            [Tooltip("How many of this enemy.")]
            [Min(1)] public int count = 3;
        }

        /// <summary>One wave of enemies.</summary>
        [Serializable]
        public class Wave
        {
            [Tooltip("The enemies in this wave.")]
            public List<Group> groups = new List<Group>();

            [Tooltip("Seconds between each enemy appearing.")]
            [Min(0f)] public float secondsBetweenEnemies = 0.5f;
        }

        [Tooltip("The waves, in order.")]
        [SerializeField] List<Wave> waves = new List<Wave>();

        [Tooltip("Where enemies appear. Children of this object work well.")]
        [SerializeField] Transform[] spawnPoints = new Transform[0];

        [Tooltip("Seconds before the first wave, and between waves.")]
        [SerializeField, Min(0f)] float pauseBetweenWaves = 2f;

        [Tooltip("Ticked: starts when the scene starts. Unticked: call Begin, for example from a Trigger Zone.")]
        [SerializeField] bool startAutomatically = true;

        [Tooltip("Ticked: after the last wave, start again from the first, for an endless arena.")]
        [SerializeField] bool loop = false;

        [Header("Showing progress")]

        [Tooltip("Optional. A number variable set to the current wave number, for the HUD.")]
        [SerializeField] IntVariable waveNumber;

        [Tooltip("Optional. A number variable set to how many enemies are left in this wave.")]
        [SerializeField] IntVariable enemiesLeft;

        [Header("Events")]

        [Tooltip("Optional. An event to raise when every wave is cleared.")]
        [SerializeField] VoidEventChannel raiseWhenCleared;

        [Tooltip("Runs when each wave starts.")]
        [SerializeField] UnityEvent onWaveStarted;

        [Tooltip("Runs when every wave is cleared.")]
        [SerializeField] UnityEvent onCleared;

        readonly List<GameObject> alive = new List<GameObject>();
        bool running;

        /// <summary>The wave being played, counting from 1. 0 before the first.</summary>
        public int CurrentWave { get; private set; }

        void Start()
        {
            if (startAutomatically)
            {
                Begin();
            }
        }

        /// <summary>Starts the waves, if they haven't started yet.</summary>
        public void Begin()
        {
            if (running)
            {
                return;
            }

            if (waves.Count == 0 || spawnPoints.Length == 0)
            {
                Debug.LogWarning($"Wave Spawner 2D on \"{name}\": it needs at least one wave and one spawn point.", this);
                return;
            }

            running = true;
            StartCoroutine(RunWaves());
        }

        IEnumerator RunWaves()
        {
            do
            {
                for (int w = 0; w < waves.Count; w++)
                {
                    yield return new WaitForSeconds(pauseBetweenWaves);

                    CurrentWave++;

                    if (waveNumber != null)
                    {
                        waveNumber.SetValue(CurrentWave);
                    }

                    onWaveStarted?.Invoke();
                    yield return SpawnWave(waves[w]);

                    // Wait until every enemy of this wave is gone.
                    while (CountAlive() > 0)
                    {
                        yield return null;
                    }
                }
            }
            while (loop);

            running = false;

            if (raiseWhenCleared != null)
            {
                raiseWhenCleared.Raise();
            }

            onCleared?.Invoke();
        }

        IEnumerator SpawnWave(Wave wave)
        {
            foreach (Group group in wave.groups)
            {
                if (group.enemy == null)
                {
                    continue;
                }

                for (int i = 0; i < group.count; i++)
                {
                    Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];

                    if (point != null)
                    {
                        alive.Add(Instantiate(group.enemy, point.position, point.rotation));
                        CountAlive();
                    }

                    if (wave.secondsBetweenEnemies > 0f)
                    {
                        yield return new WaitForSeconds(wave.secondsBetweenEnemies);
                    }
                }
            }
        }

        // Counts the enemies still in play. Destroyed or switched-off enemies are gone.
        int CountAlive()
        {
            alive.RemoveAll(enemy => enemy == null || !enemy.activeInHierarchy);

            if (enemiesLeft != null)
            {
                enemiesLeft.SetValue(alive.Count);
            }

            return alive.Count;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.9f);

            foreach (Transform point in spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.4f);
                }
            }
        }
    }
}
