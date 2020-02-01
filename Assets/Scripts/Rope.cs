using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rope : MonoBehaviour
{
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
        float height = 2 * ropeSegments[0].GetComponent<Collider2D>().bounds.extents.y;
        for (int i = 1; i < numLinks; i++) {
            ropeSegments[i].transform.position = this.gameObject.transform.position 
                                                - new Vector3(0, i * height, 0);
            DistanceJoint2D newJoint = ropeSegments[i].gameObject.AddComponent<DistanceJoint2D>();
            newJoint.connectedBody = ropeSegments[i - 1];
            newJoint.connectedAnchor = new Vector2(0, -height * anchorDistance / 2);
            newJoint.anchor = new Vector2(0, height * anchorDistance / 2);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
