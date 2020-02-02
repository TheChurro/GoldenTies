using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

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
        grabbingInWorldRope = false;
        ropeLayerMask = LayerMask.NameToLayer("RopeTrigger");
        overlappingRopes = new List<Collider2D>();
        overlappingWinchables = new List<WinchableObject>();
        DialogPanel = GameObject.Find("Dialog");
        SpeakerText = DialogPanel.transform.Find("Speaker").GetComponent<TextMeshProUGUI>();
        ContentText = DialogPanel.transform.Find("Content").GetComponent<TextMeshProUGUI>();
        HideDialog();
        hasInteraction = false;
        flags = new List<string>();
        flags.Add("Winch Unknown");
        interactables = new List<Interactable>();
    }

    private GameObject DialogPanel;
    private TextMeshProUGUI SpeakerText;
    private TextMeshProUGUI ContentText;
    private List<Interactable> interactables;
    private bool hasInteraction;
    private Interactable.Interaction interaction;
    private int dialogIndex;
    private List<string> flags;
    void ShowDialog() {
        if (!DialogPanel.activeSelf) DialogPanel.SetActive(true);
        if (hasInteraction && 0 <= dialogIndex && dialogIndex < interaction.dialog.Length) {
            SpeakerText.text = interaction.dialog[dialogIndex].Speaker;
            ContentText.text = interaction.dialog[dialogIndex].Content;
        } else {
            SpeakerText.text = "???";
            ContentText.text = "???";
        }
    }
    void HideDialog() {
        DialogPanel.SetActive(false);
    }
    bool HandleDialog() {
        if (!hasInteraction) {
            if (this.currentState != PlayerStates.Grounded) return false;
            if (interactables.Count == 0) return false;
            if (Input.GetButtonDown("Interact")) {
                print("Got button down!");
                var interactionIndex = interactables[0].GetInteraction(flags);
                if (interactionIndex != -1) {
                    print("Got button down!");
                    interaction = interactables[0].interactions[interactionIndex];
                    hasInteraction = interaction.dialog.Length > 0;
                    if (hasInteraction) {
                        dialogIndex = 0;
                        ShowDialog();
                        this.currentState = PlayerStates.Dialog;
                        return true;
                    }
                }
            }
            return false;
        } else {
            if (Input.GetButtonDown("Interact")) {
                dialogIndex++;
                if (dialogIndex < interaction.dialog.Length) {
                    ShowDialog();
                } else {
                    HideDialog();
                    hasInteraction = false;
                    foreach (var flag in interaction.flagsSet) {
                        flags.Add(flag);
                    }
                    foreach (var flag in interaction.flagsRemove) {
                        flags.Remove(flag);
                    }
                    this.currentState = this.movement.Grounded() ? PlayerStates.Grounded : PlayerStates.Aerial;
                }
            }
            return true;
        }
    }

    // Returns whether using the winch consumed input. If true, then the winch was used
    // and no otehr input should be processed.
    bool UseWinch() {
        // We must be at a WinchStation to use a winch.
        if (stationAt == null) return false;
        if (grabbingInWorldRope) return false;
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
                // Remove any destroyed ropes.
                overlappingRopes.RemoveAll((collider) => collider == null);
                // TODO: Give the player the rope back.
                return true;
            }
        }
        return false;
    }

    private List<Collider2D> overlappingRopes;
    private List<WinchableObject> overlappingWinchables;
    private bool grabbingInWorldRope;
    private int ropeLayerMask;
    private FixedJoint2D ropeJoint;
    private RopeProxy proxy;
    bool HandleRope() {
        if (ropeJoint == null) {
            grabbingInWorldRope = false;
            ropeJoint = null;
            proxy = null;
        }
        if (Input.GetButtonDown("Interact")) {
            // If we are grabbing a rope, release it.
            if (grabbingInWorldRope) {
                grabbingInWorldRope = false;
                Destroy(ropeJoint);
                if (overlappingWinchables.Count != 0 && proxy != null) {
                    proxy.obj.AttachTo(overlappingWinchables[0].GetComponent<Rigidbody2D>());
                }
                proxy = null;
                return true;
            // Otherwise, check to see if we are overlapping a rope (and 
            // therefore can grab said rope.)
            } else if (overlappingRopes.Count > 0) {
                var connectingBody = overlappingRopes[0].transform.parent.gameObject;
                proxy = overlappingRopes[0].GetComponent<RopeProxy>();
                ropeJoint = connectingBody.AddComponent<FixedJoint2D>();
                ropeJoint.autoConfigureConnectedAnchor = false;
                ropeJoint.connectedBody = this.movement.rb;
                ropeJoint.anchor = Vector2.zero;
                ropeJoint.connectedAnchor = Vector2.zero;
                grabbingInWorldRope = true;
            }
        }
        return false;
    }

    void Update() {
        bool grounded = false;
        if (this.currentState != PlayerStates.Dialog) {
            grounded = this.movement.Grounded();
            this.currentState = grounded ? PlayerStates.Grounded : PlayerStates.Aerial;
        }
        if (HandleDialog()) return;
        if (grounded) {
            if (UseWinch()) return;
        }
        if (HandleRope()) return;
        movement.HandleInput(grounded);
    }

    void OnTriggerEnter2D(Collider2D collider) {
        var winchStation = collider.GetComponent<WinchStation>();
        if (winchStation != null) {
            if (stationAt == null) {
                stationAt = winchStation;
            }
        }
        if (collider.gameObject.layer == ropeLayerMask) {
            overlappingRopes.Add(collider);
        }
        var winchableProxy = collider.GetComponent<WinchableProxy>();
        if (winchableProxy != null) {
            overlappingWinchables.Add(winchableProxy.obj);
        }
        var interactable = collider.GetComponent<Interactable>();
        if (interactable != null) {
            print("Adding INteractable: " + interactable.name);
            interactables.Add(interactable);
        }
    }

    void OnTriggerExit2D(Collider2D collider) {
        var winchStation = collider.GetComponent<WinchStation>();
        if (winchStation != null) {
            if (stationAt == winchStation) {
                stationAt = null;
            }
        }
        if (collider.gameObject.layer == ropeLayerMask) {
            overlappingRopes.Remove(collider);
        }
        var winchableProxy = collider.GetComponent<WinchableProxy>();
        if (winchableProxy != null) {
            overlappingWinchables.Remove(winchableProxy.obj);
        }
        var interactable = collider.GetComponent<Interactable>();
        if (interactable != null) {
            interactables.Remove(interactable);
        }
    }
}