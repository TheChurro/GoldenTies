using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MovementController : RaycastMovement
{
    public float speed;
    public float maxsp;
    public float maxSlope;
    private float cosMaxSlope;
    
    public Rigidbody2D rb;
    public BoxCollider2D col;
    public ContactFilter2D filter;
    public float contactTolerance;
    public SupportInfo support;

    // Start is called before the first frame update
    public override void Awake()
    {
        base.Awake();
        cosMaxSlope = Mathf.Cos(Mathf.Deg2Rad * maxSlope);
    }

    private struct SlopeTransition {
        public bool doTransition;
        public bool useVerticalVelocityInsteadOfHorizontal;
        public float cosSlope;
        public bool descend;
    }

    public void Move(Vector2 targetDisplacement, Vector2 input) {
        UpdateRayOrigins();

        Vector2 direction = targetDisplacement.normalized;
        float targetDistance = targetDisplacement.magnitude;
        Vector2 displacement = Vector2.zero;

        SlopeTransition transition = new SlopeTransition{};
        for (int i = 0; i < 4; i++) {
            support.Reset();
            transition = new SlopeTransition{};
            float distance = targetDistance;
            float skinAdjust = skinWidth / Mathf.Min(Mathf.Abs(direction.x), Mathf.Abs(direction.y));
            if (Mathf.Abs(direction.x) > EPSILON) {
                var side = direction.x < 0 ? RaycastBoundarySide.Left : RaycastBoundarySide.Right;
                DetectCollisions(
                    GetRayLine(side),
                    side,
                    true,
                    false,
                    ref transition,
                    ref distance,
                    direction
                );
            } else {
                skinAdjust = skinWidth;
            }
            if (Mathf.Abs(direction.y) > EPSILON) {
                var side = direction.y < 0 ? RaycastBoundarySide.Bottom : RaycastBoundarySide.Top;
                DetectCollisions(
                    GetRayLine(side),
                    side,
                    false,
                    side == RaycastBoundarySide.Bottom,
                    ref transition,
                    ref distance,
                    direction
                );
            } else {
                skinAdjust = skinWidth;
            }
            distance -= skinAdjust;
            this.TranslateRayOrigins(direction * distance);
            displacement += direction * distance;
            if (transition.doTransition) {
                // Reflect the movement direction up the slope.
                var newDirection = new Vector2(
                    transition.cosSlope * Mathf.Sign(direction.x),
                    Mathf.Sqrt(Mathf.Max(0, 1 - transition.cosSlope * transition.cosSlope))
                );
                // if we are descending, flip the y
                if (transition.descend) {
                    newDirection.y *= -1;
                }
                // Don't move up a slope if we are moving away from it.
                if (direction.y > newDirection.y) {
                    support.slopeCos = 1;
                    support.hitBottom = false;
                    break;
                }
                direction = newDirection;
                support.slopeCos = transition.cosSlope;
                targetDistance -= distance;
                if (transition.useVerticalVelocityInsteadOfHorizontal) {
                    //targetDistance = Mathf.Abs()
                } else {

                }
            } else {
                break;
            }
        }

        this.transform.Translate(displacement);
    }

    private void DetectCollisions(
        RaycastBoundaryLine line,
        RaycastBoundarySide boundarySide,
        bool climbSlopes,
        bool descendSlopes,
        ref SlopeTransition transition,
        ref float moveDistance,
        Vector2 direction
    ) {
        // Check for descending slopes on the opposite direction of movement.
        int descendCheckIndex = direction.x < 0 ? line.numRays - 1 : 0;
        for (int i = 0; i < line.numRays; i++) {
            Vector2 rayOrigin = line.origin + i * line.displacement;
            var hit = Physics2D.Raycast(rayOrigin, direction, moveDistance, collisionMask);
            if (hit) {
                moveDistance = hit.distance;
                switch (boundarySide) {
                    case RaycastBoundarySide.Bottom: support.hitBottom = true; break;
                    case RaycastBoundarySide.Top: support.hitTop = true; break;
                    case RaycastBoundarySide.Left: support.hitLeft = true; break;
                    case RaycastBoundarySide.Right: support.hitRight = true; break;
                }
                if (i == 0 && climbSlopes) {
                    var cosSlope = Vector2.Dot(Vector2.up, hit.normal);
                    if (cosSlope >= cosMaxSlope) {
                        transition.cosSlope = cosSlope;
                        transition.doTransition = true;
                        transition.descend = false;
                        continue;
                    }
                }
                // We always descend down slopes. The only caveat is that we may
                // not have control on those slopes
                if (i == descendCheckIndex && descendSlopes) {
                    transition.cosSlope = Vector2.Dot(Vector2.up, hit.normal);
                    transition.doTransition = true;
                    transition.useVerticalVelocityInsteadOfHorizontal = transition.cosSlope < cosMaxSlope;
                    transition.descend = true;
                    continue;
                }
                transition.doTransition = false;
            }
        }
    }

    public bool Grounded()
    {
        return false;
    }

    void OnDrawGizmos() {

    }
}

public struct SupportInfo {
    public bool hitTop, hitBottom, hitLeft, hitRight;
    public float slopeCos, oldSlopeCos;
    public void Reset() {
        hitTop = hitBottom = hitLeft = hitRight = false;
        oldSlopeCos = slopeCos;
        slopeCos = 1;
    }
}