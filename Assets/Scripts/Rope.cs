using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rope : MonoBehaviour
{
    public bool fixStart;
    public List<Rigidbody2D> ropeSegments;
    public List<DistanceJoint2D> ropeConnections;
    public GameObject endAttachment;
    public int numLinks;
    public float resolution;
    public float simulationRadius;
    public float wenchTolerance;
    private FixedJoint2D startConnection;
    private FixedJoint2D endConnection;
    private LineRenderer lineRenderer;
    // Start is called before the first frame update
    void Start()
    {
        this.lineRenderer = this.GetComponent<LineRenderer>();
        ropeSegments = new List<Rigidbody2D>();
        ropeConnections = new List<DistanceJoint2D>();
        // Add the first point in the rope. This has different properties than other
        // points in that it does not connect to any other link and may be fixed
        // to this rigidbody
        var spawnedObject = spawnLink();
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
        ropeSegments.Add(rigidBody);

        if (endAttachment != null) {
            AttachTo(endAttachment.GetComponent<Rigidbody2D>());
        } else {
            for (int i = 0; i < numLinks; i++) {
                AddLink(this.transform.position - new Vector3(0, resolution * i, 0));
            }
        }
    }

    private (GameObject, Rigidbody2D) spawnLink() {
        var newSimulationObject = new GameObject();
        var collider = newSimulationObject.AddComponent<CircleCollider2D>();
        collider.radius = simulationRadius;
        var rigidBody = newSimulationObject.AddComponent<Rigidbody2D>();
        rigidBody.mass = 1;
        return (newSimulationObject, rigidBody);
    }

    private void AddLink(Vector3 position) {
        var spawnedObject = spawnLink();
        var newSimulationObject = spawnedObject.Item1;
        var rigidBody = spawnedObject.Item2;
        var joint = newSimulationObject.AddComponent<DistanceJoint2D>();
        joint.autoConfigureConnectedAnchor = false;
        joint.autoConfigureDistance = false;
        joint.connectedBody = ropeSegments[ropeSegments.Count - 1];
        joint.anchor = Vector2.zero;
        joint.connectedAnchor = Vector2.zero;
        joint.distance = resolution;
        joint.maxDistanceOnly = true;
        ropeConnections.Add(joint);
        ropeSegments.Add(rigidBody);
        newSimulationObject.transform.position = position;
    }

    public void AttachTo(Rigidbody2D target) {
        if (endConnection != null) {
            Destroy(endConnection);
        }
        var targetCollider = target.GetComponent<Collider2D>();
        for (int i = 0; i < 50; i++) {
            var lastSegment = ropeSegments[ropeSegments.Count - 1];
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
        if (ropeConnections.Count == 0) {
            return;
        }
        if (ropeConnections[0].distance < wenchTolerance / 2) {
            // If we have effectively connect the start point and the next point, then we
            // want to check for the two bodies actually being close together
            float dist = (ropeSegments[0].position - ropeSegments[1].position).magnitude;
            if (dist < wenchTolerance) {
                // Now we will remove the second rope point and update the connection of the
                // next rope point to the start. But first we need to see if we are removing
                // the end rope point.
                if (ropeConnections.Count == 1) {
                    // If we are dragging and object, then anchor that object to the
                    // start node. So if we give it lenience, it will be pulled back out.
                    if (endConnection != null) {
                        var newEndConnection = endConnection.gameObject.AddComponent<FixedJoint2D>();
                        newEndConnection.autoConfigureConnectedAnchor = false;
                        newEndConnection.connectedBody = ropeSegments[0];
                        newEndConnection.connectedAnchor = Vector2.zero;
                        newEndConnection.anchor = endConnection.anchor;
                        // Destroy the attachment to ropeSegments[1].
                        Destroy(endConnection);
                        endConnection = newEndConnection;
                    }
                } else {
                    // Otherwise, there is another point. Attach that point to the base point and
                    // remove the old connection. Keep the current distance between the second point
                    // and base point as their distance for now. It will be wenched together later.
                    var newJoint = ropeSegments[2].gameObject.AddComponent<DistanceJoint2D>();
                    newJoint.autoConfigureConnectedAnchor = false;
                    newJoint.autoConfigureDistance = false;
                    newJoint.connectedBody = ropeSegments[0];
                    newJoint.connectedAnchor = Vector2.zero;
                    newJoint.anchor = Vector2.zero;
                    newJoint.distance = (ropeSegments[0].position - ropeSegments[2].position).magnitude;
                    Destroy(ropeConnections[1]);
                    ropeConnections[1] = newJoint;
                }
                Destroy(ropeSegments[1].gameObject);
                ropeSegments.RemoveAt(1);
                ropeConnections.RemoveAt(0);
            }
        } else {
            amount = Mathf.Min(amount, ropeConnections[0].distance);
            ropeConnections[0].distance -= amount;
        }
    }

    void DrawRope() {
        lineRenderer.startWidth = simulationRadius;
        lineRenderer.endWidth = simulationRadius;
        Vector3[] ropePositions = new Vector3[this.ropeSegments.Count];
        for (int i = 0; i < ropePositions.Length; i++) {
            ropePositions[i] = ropeSegments[i].position;
        }
        lineRenderer.positionCount = ropePositions.Length;
        lineRenderer.SetPositions(ropePositions);
    }

    void Update() {
        // Update our line renderer
        DrawRope();
        Wench(0.5f * Time.deltaTime);
    }
}
