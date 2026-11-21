using UnityEngine;

namespace CollinGame.Kits.Stealth
{
    /// <summary>
    /// Draws a guard's view on the floor, cut short by walls, and colored by
    /// its alert meter: calm, suspicious, chasing. Stealth games often show
    /// it; horror games usually don't.
    ///
    /// Setup: add it to an empty child of the guard, at floor height, with a
    /// Mesh Filter and a Mesh Renderer using a see-through unlit material.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [AddComponentMenu("Collin Game/Stealth Kit/Vision Cone 3D")]
    public class VisionCone3D : MonoBehaviour
    {
        [Tooltip("The guard whose view to draw. Found on a parent if empty.")]
        [SerializeField] Guard3D guard;

        [Tooltip("Rays across the view. More is smoother.")]
        [SerializeField, Range(4, 64)] int rays = 24;

        [SerializeField] Color calmColor = new Color(1f, 1f, 0.6f, 0.25f);
        [SerializeField] Color suspiciousColor = new Color(1f, 0.6f, 0.1f, 0.35f);
        [SerializeField] Color chasingColor = new Color(1f, 0.15f, 0.1f, 0.45f);

        Mesh mesh;
        MeshRenderer meshRenderer;
        MaterialPropertyBlock block;
        Vector3[] vertices;
        int[] triangles;

        void Awake()
        {
            if (guard == null)
            {
                guard = GetComponentInParent<Guard3D>();
            }

            mesh = new Mesh { name = "Vision Cone" };
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            meshRenderer = GetComponent<MeshRenderer>();
            block = new MaterialPropertyBlock();

            vertices = new Vector3[rays + 2];
            triangles = new int[rays * 3];

            for (int i = 0; i < rays; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        void LateUpdate()
        {
            if (guard == null)
            {
                return;
            }

            float half = guard.ViewAngle * 0.5f;
            Vector3 eye = guard.transform.position + Vector3.up * guard.EyeHeight;
            vertices[0] = Vector3.zero;

            for (int i = 0; i <= rays; i++)
            {
                float angle = Mathf.Lerp(-half, half, i / (float)rays);
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * guard.transform.forward;
                float distance = guard.ViewDistance;

                if (Physics.Raycast(eye, direction, out RaycastHit hit, distance, guard.BlocksView, QueryTriggerInteraction.Ignore)
                    && hit.transform.GetComponentInParent<StealthTarget3D>() == null)
                {
                    distance = hit.distance;
                }

                Vector3 world = transform.position + direction * distance;
                vertices[i + 1] = transform.InverseTransformPoint(world);
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            Color color = guard.CurrentState == Guard3D.State.Chase ? chasingColor
                : guard.Alert > 0.01f ? Color.Lerp(calmColor, suspiciousColor, guard.Alert) : calmColor;
            meshRenderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            meshRenderer.SetPropertyBlock(block);
        }
    }
}
