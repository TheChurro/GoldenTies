using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * A base monobehavior for objects which move via ray-cast based physics.
 * This includes objects such as moving platforms and the player.
 **/
[RequireComponent(typeof(BoxCollider2D))]
public class RaycastMovement : MonoBehaviour
{
    public const float EPSILON = 0.00001f;

    public LayerMask collisionMask;
    public float skinWidth = 0.015f;
    public float distanceBetweenRays = 0.25f;

    protected int numHorizontalRays;
    protected int numVerticalRays;
    protected float horizontalSpacing;
    protected float verticalSpacing;

    [HideInInspector]
    public BoxCollider2D box;
    public RaycastBoundary rayOrigins;

    public virtual void Awake() {
        box = GetComponent<BoxCollider2D>();
    }

    public virtual void Start() {
        CalculateRaySpacing();
    }

    protected void CalculateRaySpacing() {
        Bounds bounds = box.bounds;
        bounds.Expand(skinWidth * -2);
        var boxSize = bounds.size;

        numHorizontalRays = Mathf.RoundToInt(boxSize.x / distanceBetweenRays);
        horizontalSpacing = boxSize.x / (numHorizontalRays - 1);
        numVerticalRays = Mathf.RoundToInt(boxSize.y / distanceBetweenRays);
        verticalSpacing = boxSize.y / (numVerticalRays - 1);
    }

    protected void UpdateRayOrigins() {
        Bounds bounds = box.bounds;
        bounds.Expand(skinWidth * -2);
        var min = bounds.min;
        var max = bounds.max;

        rayOrigins = new RaycastBoundary{
            topLeft = new Vector2(min.x, max.y),
            topRight = new Vector2(max.x, max.y),
            bottomLeft = new Vector2(min.x, min.y),
            bottomRight = new Vector2(max.x, min.y),
        };
    }

    protected void TranslateRayOrigins(Vector2 displacement) {
        rayOrigins.topLeft += displacement;
        rayOrigins.topRight += displacement;
        rayOrigins.bottomLeft += displacement;
        rayOrigins.bottomRight += displacement;
    }

    protected RaycastBoundaryLine GetRayLine(RaycastBoundarySide boundary) {
        switch (boundary) {
            case RaycastBoundarySide.Top: return new RaycastBoundaryLine {
                origin = this.rayOrigins.topLeft,
                displacement = Vector2.right * this.horizontalSpacing,
                numRays = this.numHorizontalRays
            };
            case RaycastBoundarySide.Bottom: return new RaycastBoundaryLine {
                origin = this.rayOrigins.bottomLeft,
                displacement = Vector2.right * this.horizontalSpacing,
                numRays = this.numHorizontalRays
            };
            case RaycastBoundarySide.Left: return new RaycastBoundaryLine {
                origin = this.rayOrigins.bottomLeft,
                displacement = Vector2.up * this.verticalSpacing,
                numRays = this.numVerticalRays
            };
            case RaycastBoundarySide.Right: return new RaycastBoundaryLine {
                origin = this.rayOrigins.bottomRight,
                displacement = Vector2.up * this.verticalSpacing,
                numRays = this.numVerticalRays
            };
            default: return new RaycastBoundaryLine{};
        }
    }
}

public enum RaycastBoundarySide {
    Top,
    Bottom,
    Left,
    Right
}
public struct RaycastBoundary {
    public Vector2 topLeft;
    public Vector2 topRight;
    public Vector2 bottomLeft;
    public Vector2 bottomRight;
}
public struct RaycastBoundaryLine {
    public Vector2 origin;
    public Vector2 displacement;
    public int numRays;
}