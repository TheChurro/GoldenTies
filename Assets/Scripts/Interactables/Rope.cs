using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rope : MonoBehaviour, OnRoomTransition
{
    public bool fixStart;
    public List<Rigidbody2D> ropePoints;
    public List<DistanceJoint2D> ropeSegments;
    public GameObject endAttachment;
    public int numLinks;
    public float resolution;
    public float simulationRadius;
    public float wenchTolerance;
    public float perSimulatorMass;
    private FixedJoint2D startConnection;
    private FixedJoint2D endConnection;
    private LineRenderer lineRenderer;
    // Start is called before the first frame update
    void Start()
    {
        var manager = FindObjectOfType<RoomManager>();
        if (manager != null) {
            manager.RegisterTransitionHandler(this);
        }
        this.lineRenderer = this.GetComponent<LineRenderer>();
        ropePoints = new List<Rigidbody2D>();
        ropeSegments = new List<DistanceJoint2D>();
        // Add the first point in the rope. This has different properties than other
        // points in that it does not connect to any other link and may be fixed
        // to this rigidbody
        var spawnedObject = SpawnLink();
        var newSimulationObject = spawnedObject.Item1;
        var rigidBody = spawnedObject.Item2;
        newSimulationObject.GetComponent<Collider2D>().enabled = false;
        if (fixStart) {
            startConnection = newSimulationObject.AddComponent<FixedJoint2D>();
            startConnection.autoConfigureConnectedAnchor = false;
            startConnection.connectedBody = this.GetComponent<Rigidbody2D>();
            startConnection.anchor = Vector2.zero;
        }
        newSimulationObject.transform.position = this.transform.position;
        ropePoints.Add(rigidBody);

        if (endAttachment != null) {
            AttachTo(endAttachment.GetComponent<Rigidbody2D>());
        } else {
            for (int i = 0; i < numLinks; i++) {
                AddLink(this.transform.position - new Vector3(0, resolution * i, 0));
            }
        }
    }

    private (GameObject, Rigidbody2D) SpawnLink() {
        var newSimulationObject = new GameObject();
        newSimulationObject.layer = LayerMask.NameToLayer("Rope");
        var collider = newSimulationObject.AddComponent<CircleCollider2D>();
        collider.radius = simulationRadius;
        var childTriggerObject = new GameObject();
        childTriggerObject.transform.parent = newSimulationObject.transform;
        childTriggerObject.transform.localPosition = Vector3.zero;
        childTriggerObject.layer = LayerMask.NameToLayer("RopeTrigger");
        var childCollider = childTriggerObject.AddComponent<CircleCollider2D>();
        childCollider.isTrigger = true;
        childCollider.radius = simulationRadius;
        childTriggerObject.AddComponent<RopeProxy>().obj = this;
        var rigidBody = newSimulationObject.AddComponent<Rigidbody2D>();
        rigidBody.mass = perSimulatorMass;
        return (newSimulationObject, rigidBody);
    }

    private void AddLink(Vector3 position) {
        var spawnedObject = SpawnLink();
        var newSimulationObject = spawnedObject.Item1;
        var rigidBody = spawnedObject.Item2;
        var joint = newSimulationObject.AddComponent<DistanceJoint2D>();
        joint.autoConfigureConnectedAnchor = false;
        joint.autoConfigureDistance = false;
        joint.connectedBody = ropePoints[ropePoints.Count - 1];
        joint.anchor = Vector2.zero;
        joint.connectedAnchor = Vector2.zero;
        joint.distance = resolution;
        joint.maxDistanceOnly = true;
        ropeSegments.Add(joint);
        ropePoints.Add(rigidBody);
        newSimulationObject.transform.position = position;
    }

    public void AttachTo(Rigidbody2D target) {
        if (endConnection != null) {
            Destroy(endConnection);
        }
        var targetCollider = target.GetComponent<Collider2D>();
        for (int i = 0; i < 50; i++) {
            var lastSegment = ropePoints[ropePoints.Count - 1];
            var lastSegPos3 = lastSegment.transform.position;
            var lastSegPos = new Vector2(lastSegPos3.x, lastSegPos3.y);
            var closestPoint = targetCollider.ClosestPoint(lastSegPos);
            var dist = (closestPoint - lastSegPos).magnitude;
            if (dist < simulationRadius) {
                endConnection = target.gameObject.AddComponent<FixedJoint2D>();
                endConnection.autoConfigureConnectedAnchor = false;
                endConnection.connectedBody = lastSegment;
                endConnection.connectedAnchor = Vector2.zero;
                endConnection.anchor = endConnection.transform.InverseTransformPoint(closestPoint);
                return;
            }
            var offset = (closestPoint - lastSegPos).normalized;
            var movement = Mathf.Min(resolution, dist);
            var attachPos = lastSegPos3 + (Vector3)(offset * movement);
            AddLink(attachPos);
        }
    }

    public void Wench(float amount) {
        if (ropeSegments.Count == 0) {
            return;
        }
        if (ropeSegments[0].distance < wenchTolerance / 2) {
            // If we have effectively connect the start point and the next point, then we
            // want to check for the two bodies actually being close together
            float dist = (ropePoints[0].position - ropePoints[1].position).magnitude;
            if (dist < wenchTolerance) {
                // Now we will remove the second rope point and update the connection of the
                // next rope point to the start. But first we need to see if we are removing
                // the end rope point.
                if (ropeSegments.Count == 1) {
                    // If we are dragging and object, then anchor that object to the
                    // start node. So if we give it lenience, it will be pulled back out.
                    if (endConnection != null) {
                        var newEndConnection = endConnection.gameObject.AddComponent<FixedJoint2D>();
                        newEndConnection.autoConfigureConnectedAnchor = false;
                        newEndConnection.connectedBody = ropePoints[0];
                        newEndConnection.connectedAnchor = Vector2.zero;
                        newEndConnection.anchor = endConnection.anchor;
                        // Destroy the attachment to ropePoints[1].
                        Destroy(endConnection);
                        endConnection = newEndConnection;
                    }
                } else {
                    // Otherwise, there is another point. Attach that point to the base point and
                    // remove the old connection. Keep the current distance between the second point
                    // and base point as their distance for now. It will be wenched together later.
                    var newJoint = ropePoints[2].gameObject.AddComponent<DistanceJoint2D>();
                    newJoint.autoConfigureConnectedAnchor = false;
                    newJoint.autoConfigureDistance = false;
                    newJoint.connectedBody = ropePoints[0];
                    newJoint.connectedAnchor = Vector2.zero;
                    newJoint.anchor = Vector2.zero;
                    newJoint.maxDistanceOnly = true;
                    newJoint.distance = (ropePoints[0].position - ropePoints[2].position).magnitude;
                    Destroy(ropeSegments[1]);
                    ropeSegments[1] = newJoint;
                }
                Destroy(ropePoints[1].gameObject);
                ropePoints.RemoveAt(1);
                ropeSegments.RemoveAt(0);
            }
        }
        for (int i = 0; (i < ropeSegments.Count) && (amount >= 0.001 * wenchTolerance); i++) {
            var change = Mathf.Min(amount, ropeSegments[i].distance - 0.005f);
            ropeSegments[i].distance -= change;
            amount -= change;
        }
    }

    public void Release(float amount) {
        for (int i = 0; (i < ropeSegments.Count) && (amount >= 0.001 * wenchTolerance); i++) {
            if (ropeSegments[i].distance < resolution) {
                float lastDist = ropeSegments[i].distance;
                ropeSegments[i].distance = Mathf.Min(lastDist + amount, resolution);
                amount -= ropeSegments[i].distance - lastDist;
            }
        }
        if (amount < 0.0001) {
            return;
        }
        amount = Mathf.Min(amount, resolution);
        var spawnedObject = SpawnLink();
        var newSimulationObject = spawnedObject.Item1;
        var rigidBody = spawnedObject.Item2;
        if (ropePoints.Count == 1) {
            newSimulationObject.transform.position = ropePoints[0].transform.position - new Vector3(0, resolution, 0);
        } else {
            newSimulationObject.transform.position = (1 - amount / resolution) * ropePoints[0].transform.position + amount / resolution * ropePoints[1].transform.position;
        }
        rigidBody.position = new Vector2(newSimulationObject.transform.position.x, newSimulationObject.transform.position.y);
        
        // Create a joint between the new rope point and the start
        var joint = newSimulationObject.AddComponent<DistanceJoint2D>();
        joint.autoConfigureDistance = false;
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedBody = ropePoints[0];
        joint.connectedAnchor = Vector2.zero;
        joint.anchor = Vector2.zero;
        joint.distance = amount;
        joint.maxDistanceOnly = true;

        if (ropeSegments.Count == 0) {
            // When we are adding the first rope segment, check to see if we have attached a body
            // and reattach that body to the new rope point. This will be the end of the rope.
            if (endConnection != null) {
                var newEndConnection = endConnection.gameObject.AddComponent<FixedJoint2D>();
                newEndConnection.autoConfigureConnectedAnchor = false;
                newEndConnection.connectedBody = rigidBody;
                newEndConnection.connectedAnchor = Vector2.zero;
                newEndConnection.anchor = endConnection.anchor;
                // Destroy the attachment to ropePoints[0].
                Destroy(endConnection);
                endConnection = newEndConnection;
            }
        } else {
            // Otherwise, we are inserting a new rope point bewteen the start and second point.
            // We need to remove the connection from the old second point to the start, and create
            // one bewteen the new second point and the old second point.
            var newJoint = ropePoints[1].gameObject.AddComponent<DistanceJoint2D>();
            newJoint.autoConfigureConnectedAnchor = false;
            newJoint.autoConfigureDistance = false;
            newJoint.connectedBody = rigidBody;
            newJoint.distance = resolution;
            newJoint.anchor = Vector2.zero;
            newJoint.connectedAnchor = Vector2.zero;
            newJoint.maxDistanceOnly = true;
            Destroy(ropeSegments[0]);
            ropeSegments[0] = newJoint;
        }
        // Insert our new points and segments.
        ropeSegments.Insert(0, joint);
        ropePoints.Insert(1, rigidBody);
    }

    void DrawRope() {
        lineRenderer.startWidth = simulationRadius;
        lineRenderer.endWidth = simulationRadius;
        Vector3[] ropePositions = new Vector3[this.ropePoints.Count];
        for (int i = 0; i < ropePositions.Length; i++) {
            ropePositions[i] = ropePoints[i].position;
        }
        lineRenderer.positionCount = ropePositions.Length;
        lineRenderer.SetPositions(ropePositions);
    }

    void Update() {
        // Update our line renderer
        DrawRope();
    }

    public void Destroy() {
        if (endConnection != null) {
            Destroy(endConnection);
        }
        foreach (var point in ropePoints) {
            if (point != null)
                Destroy(point.gameObject);
        }
        if (this != null)
            Destroy(this.gameObject);
    }

    public void OnRoomTransition(RoomManager manager, bool willSave) {
        this.Destroy();
    } 

    public void OnRoomSave(RoomManager manager) {

    }
}

public class RopeProxy : MonoBehaviour {
    public Rope obj;
}