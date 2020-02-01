using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rope : MonoBehaviour
{
    public bool freezeFirstLink;
    public Rigidbody2D[] ropeSegments;
    public Joint2D[] ropeConnections;
    public int numLinks;
    // Fraction of distance between achor points in local space.
    public float anchorDistance;
    // Fraction of height allowed between links.
    public float lenience;
    public GameObject link;
    // Start is called before the first frame update
    void Start()
    {
        ropeSegments = new Rigidbody2D[numLinks];
        for (int i = 0; i < numLinks; i++) {
            ropeSegments[i] = Instantiate(
                link,
                this.gameObject.transform
            ).GetComponent<Rigidbody2D>();
        }
        float height = 2 * ropeSegments[0].gameObject.GetComponent<Collider2D>().bounds.extents.y;
        print("Extents: " + ropeSegments[0].gameObject.GetComponent<Collider2D>().bounds.extents);
        ropeConnections = new Joint2D[numLinks - 1];
        for (int i = 1; i < numLinks; i++) {
            ropeSegments[i].transform.position = this.gameObject.transform.position 
                                                - new Vector3(0, i * height, 0);
            DistanceJoint2D newJoint = ropeSegments[i].gameObject.AddComponent<DistanceJoint2D>();
            newJoint.autoConfigureConnectedAnchor = false;
            newJoint.autoConfigureDistance = false;
            newJoint.connectedBody = ropeSegments[i - 1];
            newJoint.connectedAnchor = new Vector2(0, -height * anchorDistance / 2);
            newJoint.anchor = new Vector2(0, height * anchorDistance / 2);
            newJoint.distance = height * lenience;
            newJoint.maxDistanceOnly = true;
            ropeConnections[i - 1] = newJoint;
        }
        if (freezeFirstLink) {
            ropeSegments[0].constraints = RigidbodyConstraints2D.FreezeAll;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
