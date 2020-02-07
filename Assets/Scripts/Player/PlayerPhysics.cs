using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MovementController))]
public class PlayerPhysics : MonoBehaviour
{
    public float maxHorizSpeed;
    public float timeToReachSpeed;
    public float timeToSlow;
    private float dampingVelocity;
    public float maxJumpHeight;
    public float minJumpHeight;
    public float jumpTime;
    private float jumpSpeed;
    private float gravityMin;
    private float gravityMax;
    [SerializeField]
    private bool grounded;
    [SerializeField]
    private bool sliding;
    private MovementController controller;
    public Vector2 currentVelocity;
    public bool testMovement;
    public Vector2 testInput;
    // Start is called before the first frame update
    void Awake()
    {
        // Calculate jump velocity and gravity
        float invJumpTime = 1 / jumpTime;
        jumpSpeed = 2 * maxJumpHeight * invJumpTime;
        float jumpSpeedSq = jumpSpeed * jumpSpeed;
        gravityMin = -jumpSpeedSq / (2 * maxJumpHeight);
        gravityMax = -jumpSpeedSq / (2 * minJumpHeight);
        controller = GetComponent<MovementController>();
        grounded = false;
    }

    public bool UpdateGrounded() {
        Physics2D.SyncTransforms();
        this.grounded = this.controller.Grounded();
        this.sliding = this.controller.Sliding();
        return grounded;
    }

    void Update() {
        HandleInput();
    }

    // Update is called once per frame
    public void HandleInput() {
        this.UpdateGrounded();
        if (!this.sliding) {
            float targetXVel = this.maxHorizSpeed * Input.GetAxisRaw("Horizontal");
            if (testMovement) {
                targetXVel = this.maxHorizSpeed * testInput.x;
            }
            Vector2 normal = Vector2.up;
            Vector2 rightSlopeDir = Vector2.right;
            if (this.controller.support.onSlope) {
                normal = this.controller.support.hitNormal;
                rightSlopeDir = new Vector2(Mathf.Abs(normal.y), -Mathf.Sign(normal.y) * normal.x);
            }
            float rightVel = Vector2.Dot(rightSlopeDir, this.currentVelocity);
            rightVel = Mathf.SmoothDamp(
                rightVel,
                targetXVel,
                ref this.dampingVelocity,
                Mathf.Abs(targetXVel) < 0.001 ? this.timeToSlow : this.timeToReachSpeed
            );
            this.currentVelocity = rightVel * rightSlopeDir + Vector2.Dot(normal, this.currentVelocity) * normal;
        }
        
        
        // CHANGE IF GROUNDED
        if (grounded && !this.sliding) {
            if (Input.GetButtonDown("Jump")) {
                this.currentVelocity.y = jumpSpeed;
            }
        } else {
            if (this.currentVelocity.y > 0 && Input.GetButton("Jump")) {
                this.currentVelocity.y += gravityMin * Time.deltaTime;
            } else {
                this.currentVelocity.y += gravityMax * Time.deltaTime;
            }
        }
        this.controller.Move(ref this.currentVelocity, Time.deltaTime, Vector2.zero);
    }
}
