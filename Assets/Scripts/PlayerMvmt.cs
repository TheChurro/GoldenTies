using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerMvmt : MonoBehaviour
{
    public float speed;
    public float maxsp;
    public float jumpforce;
    public Rigidbody2D rb;
    public BoxCollider2D col;
    public float gravitymin;
    public float gravitymax;

    public enum states
    {
        grounded,
        up,
        down
    }
    public states state;
    public ContactFilter2D filter;
    public float contactTolerance;

    // Start is called before the first frame update
    void Start()
    {
        state = states.grounded;
    }

    // Update is called once per frame
    void Update()
    {
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

        

        switch (state)
        {
            case states.grounded:
                if ((Input.GetAxis("Jump"))>0)
                {
                    rb.AddForce(new Vector2(0, jumpforce));
                    state = states.up;
                }
                else
                {
                    raycheck();
                }
                break;
            case states.up:
                if (Input.GetAxis("Jump")>0)
                {
                    rb.gravityScale = gravitymin;
                }
                else
                {
                    rb.gravityScale = gravitymax;
                    state = states.down;
                }
                break;
            case states.down:
                raycheck();
                break;
        }

    void raycheck()
        {
            List<RaycastHit2D> results = new List<RaycastHit2D>();
            var result = Physics2D.BoxCast(rb.position, col.size, rb.rotation, new Vector2(0, -1), filter, results, contactTolerance);
            if (result == 0)
            {
                state = states.down;
            }
            else
            {
                state = states.grounded;
            }
        }
    }
   
}
