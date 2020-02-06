using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MovementController : RaycastMovement
{
    public float maxSlope;
    [SerializeField]
    private float cosMaxSlope;
    public float elasticity;
    public SupportInfo support;
    public bool Sliding() {
        return this.support.slopeCos < cosMaxSlope;
    }

    // Start is called before the first frame update
    public override void Awake()
    {
        base.Awake();
        cosMaxSlope = Mathf.Cos(Mathf.Deg2Rad * maxSlope);
    }

    private struct SlopeTransition {
        public bool doTransition;
        public float cosSlope;
        public bool descend;
        public bool ascend;
    }

    public void Move(ref Vector2 velocity, float timeStep, Vector2 input) {
        // Don't move if we are too slow.
        if (velocity.magnitude < EPSILON) {
            velocity = Vector2.zero;
            return;
        }

        Physics2D.SyncTransforms();
        UpdateRayOrigins();

        Vector2 targetDisplacement = velocity * timeStep;
        Vector2 direction = targetDisplacement.normalized;
        float targetDistance = targetDisplacement.magnitude;
        Vector2 displacement = Vector2.zero;

        SlopeTransition transition = new SlopeTransition{};
        for (int i = 0; i < 4; i++) {
            // Don't move if we are too slow.
            if (velocity.magnitude < EPSILON) {
                velocity = Vector2.zero;
                break;
            }
            if (targetDistance < EPSILON) {
                break;
            }

            // Check to see if last we knew we were on a slope.
            if (support.onSlope) {
                float normMovement = Vector2.Dot(support.hitNormal, velocity);
                // As long as we are not moving away from the slope...
                if (normMovement < EPSILON) {
                    // Make our direction and velocity perpendicular to the surface we are on
                    Vector2 movementDir = new Vector2(support.hitNormal.y, -support.hitNormal.x);
                    if (support.hitNormal.y > 0 && support.slopeCos < cosMaxSlope) {
                        // If the slope we are on is too steep
                        // Ensure we are moving dowards.
                        if (movementDir.y > 0) {
                            movementDir *= -1;
                        }
                        velocity = Mathf.Abs(velocity.y) * movementDir;
                    } else {
                        if (movementDir.x < 0 != direction.x < 0) {
                            movementDir *= -1;
                        }
                        float velInMoveDir = Vector2.Dot(velocity, movementDir);
                        targetDistance *= Mathf.Abs(velInMoveDir / velocity.magnitude);
                        velocity = velInMoveDir * movementDir;
                    }
                    direction = movementDir;
                } else {
                    // Moving away from the slope, not on it.
                    support.onSlope = false;
                }
            }
            support.Reset();
            transition = new SlopeTransition{};
            // Extend the distance we move to account for moving backwards by skin width.
            float skinAdjustFactor = 1 + Mathf.Max(direction.x * direction.x, direction.y * direction.y);
            float distance = targetDistance + skinWidth * Mathf.Sqrt(skinAdjustFactor);
            int hitRay = -1;
            if (Mathf.Abs(direction.x) > EPSILON) {
                var side = direction.x < 0 ? RaycastBoundarySide.Left : RaycastBoundarySide.Right;
                DetectCollisions(
                    GetRayLine(side),
                    side,
                    true,
                    false,
                    ref transition,
                    ref distance,
                    direction,
                    ref hitRay
                );
            }
            if (Mathf.Abs(direction.y) > EPSILON) {
                var side = direction.y < 0 ? RaycastBoundarySide.Bottom : RaycastBoundarySide.Top;
                DetectCollisions(
                    GetRayLine(side),
                    side,
                    Mathf.Abs(direction.x) <= EPSILON,
                    side == RaycastBoundarySide.Bottom,
                    ref transition,
                    ref distance,
                    direction
                );
            }
            if (distance < -EPSILON) {
                distance = 0;
            }
            // In the case that we moved further than our skin width factor, truncate.
            // We add the skin width factor to make sure we don't move into something that
            // is too close to detect.
            if (distance > targetDistance) {
                distance = targetDistance;
            }
            bool move = true;
            if (transition.doTransition) {
                support.onSlope = true;
            } else {
                bool hitVert = support.hitBottom || support.hitTop;
                bool hitHoriz = support.hitLeft || support.hitRight;
                Color col = new Color[]{Color.blue, Color.yellow, Color.black, Color.cyan}[i];
                if (hitVert || hitHoriz) {
                    // If we hit something on the side, which is facing downards and we are
                    // moving downwards, then we are missing detecting the corner of an object
                    // as one of the lower positions should have hit the upwards facing side first.
                    // As such, we will consider this a hard right wall. But, we cannot move forwards
                    // in this step as otherwise, we will be straddling the edge making the
                    // top of the corner. So, we've been bamboozled, and will just reflect away
                    // at the current position and hope it isn't noticeable.
                    if (support.latestHitSide.IsHorizontal() && support.hitNormal.y < -EPSILON) {
                        support.hitNormal = Vector2.right * (direction.x < 0 ? 1 : -1);
                        move = false;
                    }
                    // float normalVel = Vector2.Dot(support.hitNormal, velocity);
                    // Vector2 nextVelocity = velocity - (1 + elasticity) * normalVel * support.hitNormal;
                    // direction = nextVelocity.normalized;
                    // targetDistance *= nextVelocity.magnitude / velocity.magnitude;
                    // velocity = nextVelocity;
                    if (support.wasOnSlope) {
                        support.onSlope = support.wasOnSlope;
                        support.hitNormal = support.oldHitNormal;
                    } else {
                        support.onSlope = true;
                    }
                } else {
                    support.onSlope = support.wasOnSlope;
                    support.hitNormal = support.oldHitNormal;
                }
            }
            if (move) {
                this.TranslateRayOrigins(direction * distance);
                displacement += direction * distance;
                targetDistance -= distance;
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
        Vector2 direction,
        bool verbose=false
    ) {
        int hitRay = 0;
        DetectCollisions(line, boundarySide, climbSlopes, descendSlopes, ref transition, ref moveDistance, direction, ref hitRay, verbose);
    }

    private void DetectCollisions(
        RaycastBoundaryLine line,
        RaycastBoundarySide boundarySide,
        bool climbSlopes,
        bool descendSlopes,
        ref SlopeTransition transition,
        ref float moveDistance,
        Vector2 direction,
        ref int hitRay,
        bool verbose=false
    ) {
        if (verbose && climbSlopes && descendSlopes) {
            print("-- CLIMBING AND DESCENDING!");
        }
        // Check for descending slopes on the opposite direction of movement.
        int descendingIndex = direction.x < 0 ? line.numRays - 1 : 0;
        int ascendingIndex = boundarySide != RaycastBoundarySide.Bottom ? 0 : direction.x < 0 ? 0 : line.numRays - 1;
        for (int i = 0; i < line.numRays; i++) {
            Vector2 rayOrigin = line.origin + i * line.displacement;
            var hit = Physics2D.Raycast(rayOrigin, direction, moveDistance, collisionMask);
            if (hit) {
                Debug.DrawRay(rayOrigin, direction * hit.distance, Color.red);
            } else {
                Debug.DrawRay(rayOrigin, direction * moveDistance, Color.green);
            }
            
            if (hit && hit.distance != 0) {
                hitRay = i;
                support.latestHitSide = boundarySide;
                support.hitNormal = hit.normal;
                // Skin adjusted sizes
                switch (boundarySide) {
                    case RaycastBoundarySide.Bottom:
                    case RaycastBoundarySide.Top:
                        moveDistance = hit.distance - Mathf.Abs(skinWidth / direction.y);
                        break;
                    case RaycastBoundarySide.Left:
                    case RaycastBoundarySide.Right:
                        moveDistance = hit.distance -  Mathf.Abs(skinWidth / direction.x);
                        break;
                }
                if (i == ascendingIndex && climbSlopes) {
                    transition.ascend = true;
                    transition.cosSlope = Vector2.Dot(Vector2.up, hit.normal);
                    transition.doTransition = true;
                    continue;
                }
                // We always descend down slopes. The only caveat is that we may
                // not have control on those slopes
                if (i == descendingIndex && descendSlopes) {
                    transition.descend = true;
                    transition.cosSlope = Vector2.Dot(Vector2.up, hit.normal);
                    transition.doTransition = true;
                    continue;
                }
                switch (boundarySide) {
                    case RaycastBoundarySide.Bottom: support.hitBottom = true; break;
                    case RaycastBoundarySide.Top: support.hitTop = true; break;
                    case RaycastBoundarySide.Left: support.hitLeft = true; break;
                    case RaycastBoundarySide.Right: support.hitRight = true; break;
                }
                transition.doTransition = false;
            }
        }
        if (verbose && climbSlopes && descendSlopes) {
            print("---- Got " + transition.ascend + " and " + transition.descend);
        }
    }

    public bool Grounded()
    {
        this.UpdateRayOrigins();
        this.support.Reset();
        SlopeTransition transition = new SlopeTransition{};
        float moveDistance = 2f * skinWidth;
        this.DetectCollisions(
            this.GetRayLine(RaycastBoundarySide.Bottom),
            RaycastBoundarySide.Bottom,
            true,
            true,
            ref transition,
            ref moveDistance,
            Vector2.down
        );
        if (transition.doTransition) {
            this.support.onSlope = true;
        }
        return this.support.hitBottom || transition.doTransition;
    }

    void OnDrawGizmos() {

    }
}

[System.Serializable]
public struct SupportInfo {
    public bool hitTop, hitBottom, hitLeft, hitRight;
    public bool wasOnSlope;
    public bool onSlope;
    public Vector2 hitNormal;
    public Vector2 oldHitNormal;
    public RaycastBoundarySide latestHitSide;
    public float slopeCos { get { return this.hitNormal.y; } }
    public float oldSlopeCos { get { return this.oldHitNormal.y; } }
    public void Reset() {
        hitTop = false;
        hitBottom = false;
        hitLeft = false;
        hitRight = false;
        oldHitNormal = hitNormal;
        wasOnSlope = onSlope;
        hitNormal = Vector2.up;
        onSlope = false;
    }
}
