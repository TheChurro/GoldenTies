using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MovementController : RaycastMovement
{
    public float maxSlope;
    public float stepHeight;
    [SerializeField]
    private float cosMaxSlope;
    public float elasticity;
    public SupportInfo support;
    public bool Sliding() {
        return this.support.slopeCos < cosMaxSlope;
    }
    public bool Grounded() {
        return this.support.hitBottom;
    }

    // Start is called before the first frame update
    public override void Awake()
    {
        base.Awake();
        cosMaxSlope = Mathf.Cos(Mathf.Deg2Rad * maxSlope);
    }

    private void AdjustVelocityForSlope(
        ref Vector2 velocity,
        ref float targetDistance,
        ref Vector2 direction,
        bool allowJumping
    ) {
        // Check to see if last we knew we were on a slope. If so,
        // restrict our movement to that slope.
        if (support.onSlope) {
            float normMovement = Vector2.Dot(support.hitNormal, velocity);
            // As long as we are not moving away from the slope...
            if (!allowJumping || normMovement < EPSILON) {
                // If we are standing on the slope, then align our movement to the slope
                // Make our direction and velocity perpendicular to the surface we are on
                Vector2 movementDir = new Vector2(support.hitNormal.y, -support.hitNormal.x);
                if (support.hitNormal.y > 0 && support.slopeCos < cosMaxSlope) {
                    // If the slope we are on is too steep
                    // Ensure we are moving dowards.
                    if (movementDir.y > 0) {
                        movementDir *= -1;
                    }
                } else {
                    if (movementDir.x < 0 != direction.x < 0) {
                        movementDir *= -1;
                    }
                }
                float velInMoveDir = Vector2.Dot(velocity, movementDir);
                targetDistance *= Mathf.Abs(velInMoveDir / velocity.magnitude);
                velocity = velInMoveDir * movementDir;
                direction = movementDir;
            } else {
                // Moving away from the slope, not on it.
                support.onSlope = false;
                support.hitBottom = false;
            }
        }
    }

    public void Move(ref Vector2 velocity, float timeStep, Vector2 input) {
        // Don't move if we are too slow.
        if (velocity.magnitude < EPSILON) {
            velocity = Vector2.zero;
            return;
        }

        Physics2D.SyncTransforms();

        Vector2 targetDisplacement = velocity * timeStep;
        Vector2 direction = targetDisplacement.normalized;
        float targetDistance = targetDisplacement.magnitude;

        AdjustVelocityForSlope(
            ref velocity,
            ref targetDistance,
            ref direction,
            true
        );

        Vector2 position = this.transform.position;
        position += this.box.offset;
        Vector2 size = this.box.size - 2 * skinWidth * Vector2.one;
        Collider2D lastContact = null;
        for (int i = 0; i < 4; i++) {
            // Don't move if we are too slow.
            if (velocity.magnitude < EPSILON) {
                velocity = Vector2.zero;
                break;
            }
            if (targetDistance < EPSILON) {
                break;
            }
            
            support.Reset();
            // Extend the distance we move to account for moving backwards by skin width.
            float skinAdjustFactor = 1 + Mathf.Max(direction.x * direction.x, direction.y * direction.y);
            float distance = targetDistance + skinWidth * Mathf.Sqrt(skinAdjustFactor);
            this.DetectCollisions(
                position,
                size,
                ref distance,
                direction,
                ref lastContact
            );
            if (distance < 0) {
                distance = 0;
            }
            // In the case that we moved further than our skin width factor, truncate.
            // We add the skin width factor to make sure we don't move into something that
            // is too close to detect.
            if (distance > targetDistance) {
                distance = targetDistance;
            }
            position += distance * direction;
            targetDistance -= distance;
            // In the case where we hit nothing, check to see if we
            // should try to stick to the slope we are on.
            if (!support.hit) {
                StickToSlopes(ref position, size, targetDistance);
            }

            if (support.hitBottom) {
                // If we are supported below, transition our velocity
                // to the new slope. We are *not* jumping off.
                support.onSlope = true;
                AdjustVelocityForSlope(
                    ref velocity,
                    ref targetDistance,
                    ref direction,
                    false
                );
            } else if (support.hit) {
                support.onSlope = false;
                // Reflect our velocity off the hit slope
                float normVelocity = Vector2.Dot(support.hitNormal, velocity);
                if (normVelocity < 0) {
                    float currentSpeed = velocity.magnitude;
                    // Cancel out our movement towards the hit normal.
                    Vector2 newVelocity = velocity - (1 + elasticity) * normVelocity * support.hitNormal;
                    // Adjust distance for new velocity.
                    targetDistance *= newVelocity.magnitude / currentSpeed;
                    velocity = newVelocity;
                    // If zero movement
                    if (targetDistance < EPSILON) {
                        direction = Vector2.zero;
                    } else {
                        direction = newVelocity.normalized;
                    }
                }
            }
        }
        if (!support.hitBottom) {
            StickToSlopes(ref position, size, velocity.magnitude);
            if (support.hitBottom) {
                AdjustVelocityForSlope(
                    ref velocity,
                    ref targetDistance,
                    ref direction,
                    false
                );
            }
        }
        Vector3 outPos = position - this.box.offset;
        outPos.z = this.transform.position.z;
        this.transform.position = outPos;
    }

    private void StickToSlopes(
        ref Vector2 position,
        Vector2 size,
        float movement
    ) {
        // If we were on a slope in the last iteration then we will check to see if
        // we are still on a slope, and if so, drop down to it. This is especially
        // useful for when you climb over the top of one slope only to drop down
        // the next one.
        if (support.didHitBottom) {
            float checkDistance = Mathf.Infinity;
            Collider2D none = null;
            DetectCollisions(position, size, ref checkDistance, Vector2.down, ref none);
            
            Debug.DrawRay(position, Mathf.Abs(support.oldSlopeSin) * movement * Vector2.down, Color.red);
            Debug.DrawRay(position + Vector2.down * checkDistance, Mathf.Abs(support.slopeSin) * movement * Vector2.up, Color.green);
            // If we hit something below us, then we are over a surface. If that surface
            // can be stood on, (aka, has a small enough angle) and we are close enough
            // to the surface to reasonable hit it at our current speed
            if (support.hitBottom && support.slopeCos >= cosMaxSlope && checkDistance - skinWidth <= Mathf.Abs(support.oldSlopeSin) * movement + Mathf.Abs(support.slopeSin) * movement) {
                DebugDraws.DrawBox(position + Vector2.down * checkDistance, size, Color.green);
                support.onSlope = true;
                position = position + checkDistance * Vector2.down;
                return;
            } else {
                DebugDraws.DrawBox(position + Vector2.down * checkDistance, size, Color.red);
            }
            support.hitBottom = false;
        }
    }

    private void DetectCollisions(
        Vector2 position,
        Vector2 size,
        ref float moveDistance,
        Vector2 direction,
        ref Collider2D ignore,
        bool verbose=false
    ) {
        var hits = Physics2D.BoxCastAll(position, size, 0, direction, moveDistance, this.collisionMask);
        RaycastHit2D hit = new RaycastHit2D{};
        float minHit = moveDistance + 10;
        for (int i = 0; i < hits.Length; i++) {
            // If you are hitting the same edge of the same collider, ignore it. We already
            // have that support.
            bool sameHit = hits[i].collider == ignore && (support.lastHitNormal - hits[i].normal).magnitude < EPSILON;
            if (!sameHit && hits[i].distance < minHit) {
                if (hits[i].distance == 0 && Vector2.Dot(hits[i].normal, direction) > 0) { continue; }
                hit = hits[i];
                minHit = hit.distance;
            }
        }
        if (hit) {
            ignore = hit.collider;
            // Move, but adjust for the fact that our cast doesn't account for a skinwidth border
            float adjust = Mathf.Min(hit.distance, skinWidth / Mathf.Abs(direction.y), skinWidth / Mathf.Abs(direction.x));
            moveDistance = hit.distance - adjust;
            var centroid = position + direction * moveDistance;

            // DebugDraws.DrawBox(hit.centroid, size, Color.red);
            // DebugDraws.DrawBox(centroid, size + 2 * skinWidth * Vector2.one);
            // Debug.DrawLine(hit.centroid, hit.point, Color.red);
            // Debug.DrawLine(hit.centroid, centroid, Color.white);

            // Here we use a compute centroid which naturally accounts of ambiguities surrounding
            // us hitting corners. Gives us a lenience of skinWidth in either direction.
            var relativeToEnd = hit.point - centroid;
            if (relativeToEnd.x <= -size.x / 2) {
                support.hitLeft = true;
            } else if (relativeToEnd.x >= size.x / 2) {
                support.hitRight = true;
            }
            if (relativeToEnd.y <= -size.y / 2) {
                support.hitBottom = true;
            } else if (relativeToEnd.y >= size.y / 2) {
                support.hitTop = true;
            }
            support.hitNormal = hit.normal;
        } else {
            if (moveDistance != Mathf.Infinity) {
                DebugDraws.DrawBox(position + moveDistance * direction, size + 2 * skinWidth * Vector2.one);
            }
        }
    }
}

[System.Serializable]
public struct SupportInfo {
    public bool hitTop, hitBottom, hitLeft, hitRight;
    public bool didHitTop, didHitBottom, didHitLeft, didHitRight;
    public bool hitHoriz { get { return hitLeft || hitRight; } }
    public bool hitVert { get { return hitLeft || hitRight; } }
    public bool hit { get { return hitTop || hitBottom || hitLeft || hitRight; } }
    public bool onSlope;
    public bool wasOnSlope;
    public Vector2 hitNormal;
    public Vector2 lastHitNormal;
    public float slopeTan { get { return this.hitNormal.x / this.hitNormal.y; } }
    public float slopeSin { get { return this.hitNormal.x; } }
    public float slopeCos { get { return this.hitNormal.y; } }
    public float oldSlopeTan { get { return this.lastHitNormal.x / this.lastHitNormal.y; } }
    public float oldSlopeSin { get { return this.lastHitNormal.x; } }
    public float oldSlopeCos { get { return this.lastHitNormal.y; } }
    public void Reset() {
        didHitBottom = hitBottom;
        didHitTop = hitTop;
        didHitLeft = hitLeft;
        didHitRight = hitRight;
        hitTop = false;
        hitBottom = false;
        hitLeft = false;
        hitRight = false;
        lastHitNormal = hitNormal;
        wasOnSlope = onSlope;
        hitNormal = Vector2.up;
        onSlope = false;
    }
}

static class DebugDraws {
    public static void DrawBox(Vector2 position, Vector2 size) {
        DrawBox(position, size, Color.white);
    }
    public static void DrawBox(Vector2 position, Vector2 size, Color c) {
        Vector2 right = new Vector2(size.x / 2, 0);
        Vector2 up = new Vector2(0, size.y / 2);
        Debug.DrawLine(position - right - up, position + right - up, c);
        Debug.DrawLine(position + right - up, position + right + up, c);
        Debug.DrawLine(position + right + up, position - right + up, c);
        Debug.DrawLine(position - right + up, position - right - up, c);
    }
}
