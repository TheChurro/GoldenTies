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
    private Boolean jumped;
    // Start is called before the first frame update
    void Start()
    {
        jumped = false;
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

        if(!jumped && (Input.GetKeyDown(KeyCode.UpArrow)||Input.GetKeyDown(KeyCode.W))){
            rb.AddForce(new Vector2(0,jumpforce));
            jumped = true;
        }
    }
    void OnCollisionEnter2D()
    {
        jumped = false;
    }
}
