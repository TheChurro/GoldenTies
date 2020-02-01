using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rope : MonoBehaviour
{
    public bool attachLinkToThis;
    public Rigidbody2D[] ropeSegments;
    public Joint2D[] ropeConnections;
    public int numLinks;
    public GameObject link;
    private float linkHeight = 0.0f;
    private FixedJoint2D endConnection;
    private FixedJoint2D thisConnection;
    // Start is called before the first frame update
    void Start()
    {
        ropeSegments = new Rigidbody2D[numLinks];
        for (int i = 0; i < numLinks; i++) {
            ropeSegments[i] = Instantiate(
                link,
                this.gameObject.transform.position,
                Quaternion.identity
            ).GetComponent<Rigidbody2D>();
        }
        linkHeight = 2 * ropeSegments[0].gameObject.GetComponent<Collider2D>().bounds.extents.y;
        float invYScale = 1 / ropeSegments[0].gameObject.transform.localScale.y;
        ropeConnections = new Joint2D[numLinks - 1];
        for (int i = 1; i < numLinks; i++) {
            ropeSegments[i].transform.position = this.gameObject.transform.position 
                                                - new Vector3(0, i * linkHeight, 0);
            HingeJoint2D newJoint = ropeSegments[i].gameObject.AddComponent<HingeJoint2D>();
            newJoint.autoConfigureConnectedAnchor = false;
            newJoint.connectedBody = ropeSegments[i - 1];
            newJoint.connectedAnchor = new Vector2(0, -0.5f * linkHeight);
            newJoint.anchor = new Vector2(0, 0.5f * linkHeight);
            newJoint.enableCollision = false;
            JointAngleLimits2D limits2D = new JointAngleLimits2D();
            limits2D.min = -Mathf.PI / 2;
            limits2D.max = Mathf.PI / 2;
            newJoint.limits = limits2D;
            newJoint.useLimits = true;
            ropeConnections[i - 1] = newJoint;
        }
        if (attachLinkToThis) {
            thisConnection = ropeSegments[0].gameObject.AddComponent<FixedJoint2D>();
            thisConnection.connectedBody = this.GetComponent<Rigidbody2D>();
            thisConnection.autoConfigureConnectedAnchor = false;
        }
    }
}
