using System.Collections.Generic;
using UnityEngine;

namespace CollinGame.Kits.EndlessRunner
{
    /// <summary>
    /// Builds the track as the runner goes: it keeps placing Track Chunks ahead
    /// of the runner, picked at random from those allowed at the current
    /// difficulty, and removes the ones left behind. The track never ends.
    ///
    /// Setup: put this at the point where the first chunk should start, and
    /// fill in the chunk prefabs. A safe First Chunk with no obstacles gives the
    /// player time to get going.
    /// </summary>
    [AddComponentMenu("Collin Game/Endless Runner Kit/Track Spawner 2D")]
    public class TrackSpawner2D : MonoBehaviour
    {
        [Tooltip("Optional. The runner. Leave empty to find the Runner Controller 2D in the scene.")]
        [SerializeField] RunnerController2D runner;

        [Tooltip("Optional. A chunk placed first, every time, such as flat ground with no obstacles.")]
        [SerializeField] TrackChunk firstChunk;

        [Tooltip("The chunks to pick from.")]
        [SerializeField] List<TrackChunk> chunks = new List<TrackChunk>();

        [Tooltip("Keep this much track built ahead of the runner, in units. More than one screen.")]
        [SerializeField, Min(5f)] float buildAhead = 40f;

        [Tooltip("Remove chunks once they end this far behind the runner, in units.")]
        [SerializeField, Min(0f)] float removeBehind = 20f;

        [Tooltip("Ticked: the same chunk never comes twice in a row, if there's another to pick.")]
        [SerializeField] bool avoidRepeats = true;

        readonly List<TrackChunk> placed = new List<TrackChunk>();
        readonly List<TrackChunk> allowed = new List<TrackChunk>();
        float nextX;
        TrackChunk lastPrefab;

        void Start()
        {
            if (runner == null)
            {
                runner = FindFirstObjectByType<RunnerController2D>();
            }

            nextX = transform.position.x;

            if (firstChunk != null)
            {
                Place(firstChunk);
            }

            if (chunks.Count == 0)
            {
                Debug.LogWarning($"Track Spawner 2D on \"{name}\": Chunks is empty, so no track is built.", this);
            }
        }

        void Update()
        {
            if (runner == null || chunks.Count == 0)
            {
                return;
            }

            float runnerX = runner.transform.position.x;

            // Build ahead.
            int safety = 0;

            while (nextX < runnerX + buildAhead && safety++ < 10)
            {
                TrackChunk prefab = Pick(runner.Difficulty);

                if (prefab == null)
                {
                    break;
                }

                Place(prefab);
            }

            // Remove behind.
            while (placed.Count > 0 && (placed[0] == null || placed[0].transform.position.x + placed[0].Length < runnerX - removeBehind))
            {
                if (placed[0] != null)
                {
                    Destroy(placed[0].gameObject);
                }

                placed.RemoveAt(0);
            }
        }

        TrackChunk Pick(float difficulty)
        {
            allowed.Clear();
            float total = 0f;

            foreach (TrackChunk chunk in chunks)
            {
                if (chunk == null || !chunk.AllowedAt(difficulty) || chunk.Weight <= 0f)
                {
                    continue;
                }

                if (avoidRepeats && chunk == lastPrefab)
                {
                    continue;
                }

                allowed.Add(chunk);
                total += chunk.Weight;
            }

            // Fall back to repeating, or to any chunk, rather than leaving a hole.
            if (allowed.Count == 0)
            {
                return lastPrefab != null ? lastPrefab : chunks.Find(c => c != null);
            }

            float roll = Random.Range(0f, total);

            foreach (TrackChunk chunk in allowed)
            {
                roll -= chunk.Weight;

                if (roll <= 0f)
                {
                    return chunk;
                }
            }

            return allowed[allowed.Count - 1];
        }

        void Place(TrackChunk prefab)
        {
            Vector3 position = new Vector3(nextX, transform.position.y, 0f);
            TrackChunk chunk = Instantiate(prefab, position, Quaternion.identity, transform);
            placed.Add(chunk);
            nextX += prefab.Length;
            lastPrefab = prefab;
        }
    }
}
