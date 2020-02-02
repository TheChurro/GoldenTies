using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(PlayerMvmt))]
public class PlayerController : MonoBehaviour
{
    public enum PlayerStates {
        Grounded,
        Aerial,
        Dialog
    }
    public PlayerStates currentState;

    private PlayerMvmt movement;
    private WinchStation stationAt;
    public float standingTolerance;
    void Start() {
        movement = this.GetComponent<PlayerMvmt>();
    }

    // Returns whether using the winch consumed input. If true, then the winch was used
    // and no otehr input should be processed.
    bool UseWinch() {
        // We must be at a WinchStation to use a winch.
        if (stationAt == null) return false;
        // We must be standing still (or approximately still) to use a winch.
        if (this.movement.rb.velocity.magnitude > standingTolerance) return false;
        bool hasRope = stationAt.HasRope();
        if (!hasRope) {
            if (Input.GetButtonDown("Interact")) {
                stationAt.MakeRope();
                // TODO: Take the rope from the player.
                return true;
            }
        } else {
            if (Input.GetButton("Winch")) {
                stationAt.Winch();
                return true;
            } else if (Input.GetButton("Release")) {
                stationAt.Release();
                return true;
            } else if (Input.GetButtonDown("Interact")) {
                stationAt.TakeRope();
                // TODO: Give the player the rope back.
                return true;
            }
        }
        return false;
    }

    void Update() {
        // Handle dialog. This should always return at the end. 
        if (currentState == PlayerStates.Dialog) {
            return;
        }
        bool grounded = this.movement.Grounded();
        this.currentState = grounded ? PlayerStates.Grounded : PlayerStates.Aerial;
        if (grounded) {
            print("GROUNDED");
            if (UseWinch()) return;
        }
        movement.HandleInput(grounded);
    }

    void OnTriggerEnter2D(Collider2D collider) {
        var winchStation = collider.GetComponent<WinchStation>();
        if (winchStation != null) {
            print("ENCOUNTERED WINCH STATION");
            if (stationAt == null) {
                print("    SAVING AS STATION AT");
                stationAt = winchStation;
            }
        }
    }

    void OnTriggerExit2D(Collider2D collider) {
        var winchStation = collider.GetComponent<WinchStation>();
        if (winchStation != null) {
            print("LEAVING WINCH STATION!");
            if (stationAt == winchStation) {
                print("    LEAVING STATION AT!");
                stationAt = null;
            }
        }
    }
}