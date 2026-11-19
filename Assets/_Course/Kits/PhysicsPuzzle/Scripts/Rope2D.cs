using System.Collections.Generic;
using UnityEngine;

namespace CollinGame.Kits.PhysicsPuzzle
{
    /// <summary>
    /// Builds a hanging rope of linked pieces when the scene starts, from this
    /// object down to an object hanging on the end. Each link is joined to the
    /// next by a hinge, so the rope swings. Each link can be cut by a Cutter 2D.
    ///
    /// Setup: put this on the object the rope hangs from (it doesn't move), and
    /// drag the hanging object, which needs a Rigidbody 2D, into Hanging Body.
    /// Select this object to see where the rope will go.
    /// </summary>
    [AddComponentMenu("Collin Game/Physics Puzzle Kit/Rope 2D")]
    public class Rope2D : MonoBehaviour
    {
        [Tooltip("The object that hangs on the end of the rope. It needs a Rigidbody 2D.")]
        [SerializeField] Rigidbody2D hangingBody;

        [Tooltip("How many links the rope has. More links swing more smoothly.")]
        [SerializeField, Range(2, 40)] int linkCount = 10;

        [Tooltip("Optional. A sprite to draw each link. Leave empty for an invisible rope.")]
        [SerializeField] Sprite linkSprite;

        [Tooltip("The color of the links.")]
        [SerializeField] Color linkColor = new Color(0.55f, 0.4f, 0.25f);

        [Tooltip("How thick the links are, in units.")]
        [SerializeField, Min(0.02f)] float thickness = 0.12f;

        [Tooltip("How heavy each link is. Light links make a floppy rope.")]
        [SerializeField, Min(0.001f)] float linkMass = 0.05f;

        readonly List<GameObject> links = new List<GameObject>();

        void Start()
        {
            if (hangingBody == null)
            {
                Debug.LogError($"Rope 2D on \"{name}\": Hanging Body is empty. Drag in the object that hangs on the rope.", this);
                return;
            }

            Build();
        }

        void Build()
        {
            // The top of the rope is fixed: give this object a static body to hang from.
            Rigidbody2D anchor = GetComponent<Rigidbody2D>();

            if (anchor == null)
            {
                anchor = gameObject.AddComponent<Rigidbody2D>();
                anchor.bodyType = RigidbodyType2D.Static;
            }

            Vector2 top = transform.position;
            Vector2 bottom = hangingBody.position;
            Vector2 step = (bottom - top) / linkCount;
            float linkLength = step.magnitude;
            float angle = Mathf.Atan2(step.y, step.x) * Mathf.Rad2Deg - 90f;

            Rigidbody2D previous = anchor;

            for (int i = 0; i < linkCount; i++)
            {
                Vector2 center = top + step * (i + 0.5f);

                GameObject link = new GameObject("Rope Link " + (i + 1));
                link.transform.SetPositionAndRotation(center, Quaternion.Euler(0f, 0f, angle));
                link.transform.SetParent(transform, true);
                links.Add(link);

                Rigidbody2D body = link.AddComponent<Rigidbody2D>();
                body.mass = linkMass;
                body.linearDamping = 0.2f;
                body.angularDamping = 0.5f;

                // A trigger collider, so the rope can be swiped but doesn't push things.
                CapsuleCollider2D shape = link.AddComponent<CapsuleCollider2D>();
                shape.isTrigger = true;
                shape.size = new Vector2(thickness, linkLength);

                HingeJoint2D hinge = link.AddComponent<HingeJoint2D>();
                hinge.connectedBody = previous;
                hinge.autoConfigureConnectedAnchor = false;
                hinge.anchor = new Vector2(0f, linkLength * 0.5f);
                hinge.connectedAnchor = previous == anchor
                    ? Vector2.zero
                    : new Vector2(0f, -linkLength * 0.5f);

                link.AddComponent<Cuttable2D>();

                if (linkSprite != null)
                {
                    // The picture goes on a child, stretched to the link's size,
                    // so stretching it doesn't change the collider.
                    GameObject look = new GameObject("Look");
                    look.transform.SetParent(link.transform, false);

                    SpriteRenderer renderer = look.AddComponent<SpriteRenderer>();
                    renderer.sprite = linkSprite;
                    renderer.color = linkColor;
                    renderer.sortingOrder = 2;

                    Vector2 spriteSize = linkSprite.bounds.size;
                    look.transform.localScale = new Vector3(
                        spriteSize.x > 0f ? thickness / spriteSize.x : 1f,
                        spriteSize.y > 0f ? linkLength / spriteSize.y : 1f,
                        1f);
                }

                previous = body;
            }

            // Hang the body from the last link.
            HingeJoint2D end = hangingBody.gameObject.AddComponent<HingeJoint2D>();
            end.connectedBody = previous;
            end.autoConfigureConnectedAnchor = false;
            end.anchor = Vector2.zero;
            end.connectedAnchor = new Vector2(0f, -linkLength * 0.5f);
        }

        void OnDrawGizmosSelected()
        {
            if (hangingBody == null)
            {
                return;
            }

            Gizmos.color = new Color(0.8f, 0.6f, 0.3f);
            Gizmos.DrawLine(transform.position, hangingBody.transform.position);
            Gizmos.DrawWireSphere(transform.position, 0.1f);
        }
    }
}
