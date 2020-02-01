using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomCamera : MonoBehaviour
{
    public Bounds WorldBounds;
    public GameObject TrackingObject;
    public void SetWorldBounds(Bounds NewBounds) {
        WorldBounds = NewBounds;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
