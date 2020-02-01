using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSine : MonoBehaviour
{
    private Vector3 startPos;
    private float time;
    public Vector2 moveHalfDistance;
    public float movementTime;
    private float movementTimeRatio;
    private Vector3 moveDistances;
    // Start is called before the first frame update
    void Start()
    {
        time = 0;
        startPos = this.transform.position;
        movementTimeRatio = 2 * Mathf.PI / movementTime;
        moveDistances = new Vector3(moveHalfDistance.x, moveHalfDistance.y, 0);
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        if (time > movementTime) {
            time -= movementTime;
        }
        float ratio = Mathf.Sin(movementTimeRatio * time);
        this.transform.position = startPos + ratio * moveDistances;
    }
}
