using System;
using System.Collections;
using System.Collections.Generic;
using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.TowerDefense
{
    /// <summary>
    /// Sends waves of enemies down the path. The player starts each wave with
    /// the Start Wave button, so they can build first; or waves can start by
    /// themselves. After the last wave is cleared, the map is won.
    ///
    /// It also holds the map's money and lives, and gives every enemy the path.
    /// </summary>
    [AddComponentMenu("Collin Game/Tower Defense Kit/Tower Wave Spawner")]
    public class TowerWaveSpawner : MonoBehaviour
    {
        /// <summary>A group of one kind of enemy in a wave.</summary>
        [Serializable]
        public class Group
        {
            [Tooltip("The enemy prefab. It needs a Path Enemy 2D.")]
            public PathEnemy2D enemy;

            [Tooltip("How many of this enemy.")]
            [Min(1)] public int count = 5;

            [Tooltip("Seconds between each one.")]
            [Min(0f)] public float secondsBetween = 0.8f;
        }

        /// <summary>One wave: its groups come one after another.</summary>
        [Serializable]
        public class Wave
        {
            public List<Group> groups = new List<Group>();

            [Tooltip("Money given when this wave is cleared.")]
            [Min(0)] public int bonus = 20;
        }

        [Tooltip("The path the enemies walk.")]
        [SerializeField] EnemyPath2D path;

        [Tooltip("The waves, in order.")]
        [SerializeField] List<Wave> waves = new List<Wave>();

        [Tooltip("Ticked: each wave starts by itself after Seconds Between Waves. Unticked: the player starts each wave.")]
        [SerializeField] bool startWavesAutomatically = false;

        [Tooltip("Seconds between waves when they start by themselves.")]
        [SerializeField, Min(0f)] float secondsBetweenWaves = 8f;

        [Header("Money and lives")]

        [Tooltip("The number variable for money.")]
        [SerializeField] IntVariable money;

        [Tooltip("Money at the start of the map.")]
        [SerializeField, Min(0)] int startingMoney = 100;

        [Tooltip("The number variable for lives.")]
        [SerializeField] IntVariable lives;

        [Tooltip("Lives at the start of the map.")]
        [SerializeField, Min(1)] int startingLives = 20;

        [Tooltip("Optional. A number variable set to the wave number, for the HUD.")]
        [SerializeField] IntVariable waveNumber;

        [Header("Events")]

        [Tooltip("Optional. An event to raise when every wave has been cleared.")]
        [SerializeField] VoidEventChannel raiseWhenWon;

        [Tooltip("Runs when each wave starts.")]
        [SerializeField] UnityEvent onWaveStarted;

        [Tooltip("Runs when each wave is cleared.")]
        [SerializeField] UnityEvent onWaveCleared;

        [Tooltip("Runs when every wave has been cleared.")]
        [SerializeField] UnityEvent onWon;

        readonly List<PathEnemy2D> alive = new List<PathEnemy2D>();
        bool spawning;
        bool won;

        /// <summary>The wave in play, counting from 1. 0 before the first.</summary>
        public int CurrentWave { get; private set; }

        /// <summary>True while a wave is still coming or still on the path.</summary>
        public bool WaveInProgress => spawning || CountAlive() > 0;

        /// <summary>True if there is another wave to start.</summary>
        public bool HasNextWave => CurrentWave < waves.Count;

        void Start()
        {
            if (money != null)
            {
                money.SetValue(startingMoney);
            }

            if (lives != null)
            {
                lives.SetValue(startingLives);
            }

            if (waveNumber != null)
            {
                waveNumber.SetValue(0);
            }

            if (path == null || path.Count < 2)
            {
                Debug.LogWarning($"Tower Wave Spawner on \"{name}\": Path is empty, or has fewer than two points.", this);
            }

            if (startWavesAutomatically)
            {
                StartCoroutine(AutoWaves());
            }
        }

        /// <summary>Starts the next wave, unless one is still in progress. Wire the Start Wave button to this.</summary>
        public void StartNextWave()
        {
            if (WaveInProgress || !HasNextWave || won)
            {
                return;
            }

            StartCoroutine(RunWave(waves[CurrentWave]));
        }

        IEnumerator AutoWaves()
        {
            while (HasNextWave)
            {
                yield return new WaitForSeconds(secondsBetweenWaves);
                yield return RunWave(waves[CurrentWave]);

                while (CountAlive() > 0)
                {
                    yield return null;
                }
            }
        }

        IEnumerator RunWave(Wave wave)
        {
            spawning = true;
            CurrentWave++;

            if (waveNumber != null)
            {
                waveNumber.SetValue(CurrentWave);
            }

            onWaveStarted?.Invoke();

            foreach (Group group in wave.groups)
            {
                for (int i = 0; group.enemy != null && i < group.count; i++)
                {
                    PathEnemy2D enemy = Instantiate(group.enemy, path != null ? (Vector3)path.Point(0) : transform.position, Quaternion.identity);
                    enemy.Begin(path, money, lives);
                    alive.Add(enemy);

                    if (group.secondsBetween > 0f)
                    {
                        yield return new WaitForSeconds(group.secondsBetween);
                    }
                }
            }

            spawning = false;

            while (CountAlive() > 0)
            {
                yield return null;
            }

            // Lost the map while this wave was on? Don't reward or win.
            if (lives != null && lives.Value <= 0)
            {
                yield break;
            }

            if (money != null)
            {
                money.Add(wave.bonus);
            }

            onWaveCleared?.Invoke();

            if (!HasNextWave && !won)
            {
                won = true;

                if (raiseWhenWon != null)
                {
                    raiseWhenWon.Raise();
                }

                onWon?.Invoke();
            }
        }

        int CountAlive()
        {
            alive.RemoveAll(enemy => enemy == null || !enemy.isActiveAndEnabled);
            return alive.Count;
        }
    }
}
