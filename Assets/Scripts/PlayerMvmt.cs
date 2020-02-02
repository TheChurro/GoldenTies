using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerMvmt : MonoBehaviour
{
    public float speed;
    public float maxsp;
    public float jumpHeight;
    private Vector2 jumpVelocity;
    public Rigidbody2D rb;
    public BoxCollider2D col;
    public float gravitymin;
    public float gravitymax;
    public ContactFilter2D filter;
    public float contactTolerance;

    // Start is called before the first frame update
    void Start()
    {
        jumpVelocity = -Physics2D.gravity.normalized * Mathf.Sqrt(2 * Physics2D.gravity.magnitude * gravitymin * jumpHeight);
    }

    public void HandleInput(bool grounded) {
        float translate = speed*Input.GetAxis("Horizontal");
        rb.velocity += new Vector2(translate, 0);
        if (rb.velocity.x > maxsp)
        {
            rb.velocity = new Vector2(maxsp, rb.velocity.y);
        }
        else if (rb.velocity.x < -maxsp)
        {
            rb.velocity = new Vector2(-maxsp, rb.velocity.y);
        }

        
        if (grounded) {
            if (Input.GetButtonDown("Jump")) {
                rb.velocity += jumpVelocity;
                rb.gravityScale = gravitymin;
            }
        } else {
            if (!Input.GetButton("Jump")) {
                rb.gravityScale = gravitymax;
            }
        }
    }

    public bool Grounded()
    {
        List<RaycastHit2D> results = new List<RaycastHit2D>();
        var result = Physics2D.BoxCast(rb.position, col.size, rb.rotation, new Vector2(0, -1), filter, results, contactTolerance);
        if (result == 0)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
   
}
