using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MovementController))]
public class PlayerPhysics : MonoBehaviour
{
    public float maxJumpHeight;
    public float minJumpHeight;
    public float jumpTime;
    private float jumpSpeed;
    private float gravityMin;
    private float gravityMax;
    // Start is called before the first frame update
    void Awake()
    {
        // Calculate jump velocity and gravity
        float invJumpTime = 1 / jumpTime;
        jumpSpeed = 2 * maxJumpHeight * invJumpTime;
        float jumpSpeedSq = jumpSpeed * jumpSpeed;
        gravityMin = -jumpSpeedSq / (2 * maxJumpHeight);
        gravityMax = -jumpSpeedSq / (2 * minJumpHeight);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
