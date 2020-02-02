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
        if (flags == null) {
            flags = new List<string>();
            flags.Add("Winch Unknown");
            flags.Add("rope");
        }
        interactables = new List<Interactable>();

        MovementButtonPanel = GameObject.Find("Movement Panel");
        WinchButtonDisplay = GameObject.Find("Winch Action").GetComponent<TextMeshProUGUI>();
        ReleaseButtonDisplay = GameObject.Find("Release Action").GetComponent<TextMeshProUGUI>();
        InteractButtonDisplay = GameObject.Find("Interact Action").GetComponent<TextMeshProUGUI>();
        WinchButtonText = null;
        ReleaseButtonText = null;
        InteractButtonText = null;
    }

    private string WinchButtonText;
    private string ReleaseButtonText;
    private string InteractButtonText;
    private TextMeshProUGUI WinchButtonDisplay;
    private TextMeshProUGUI ReleaseButtonDisplay;
    private TextMeshProUGUI InteractButtonDisplay;
    private GameObject MovementButtonPanel;
    void UpdateDisplay(TextMeshProUGUI gui, string text, ref bool didDisplay) {
        if (text == null) {
            if (gui.gameObject.activeSelf) gui.gameObject.SetActive(false);
        } else {
            if (!gui.gameObject.activeSelf) gui.gameObject.SetActive(true);
            gui.text = text;
            didDisplay = true;
        }
    }
    void UpdateUIControlsDisplay() {
        bool hasSpecificDisplay = false;
        UpdateDisplay(WinchButtonDisplay, WinchButtonText, ref hasSpecificDisplay);
        UpdateDisplay(ReleaseButtonDisplay, ReleaseButtonText, ref hasSpecificDisplay);
        UpdateDisplay(InteractButtonDisplay, InteractButtonText, ref hasSpecificDisplay);
        if (MovementButtonPanel.activeSelf == hasSpecificDisplay) {
            MovementButtonPanel.SetActive(!hasSpecificDisplay);
        }
    }

    private GameObject DialogPanel;
    private TextMeshProUGUI SpeakerText;
    private TextMeshProUGUI ContentText;
    private List<Interactable> interactables;
    private bool hasInteraction;
    private Interactable.Interaction interaction;
    private int dialogIndex;
    public List<string> flags;
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
    bool HandleDialog(ref bool consumeInput) {
        if (consumeInput) return false;
        if (!hasInteraction) {
            if (this.currentState != PlayerStates.Grounded) return false;
            if (interactables.Count == 0) return false;
            var interactionIndex = interactables[0].GetInteraction(flags);
            consumeInput = interactionIndex != -1;
            if (consumeInput) {
                InteractButtonText = "Interact";
            }
            if (Input.GetButtonDown("Interact")) {
                if (interactionIndex != -1) {
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
            consumeInput = true;
            InteractButtonText = "Continue";
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
    bool UseWinch(ref bool consumeInput) {
        if (consumeInput) return false;
        // We must be at a WinchStation to use a winch.
        if (stationAt == null) return false;
        if (grabbingInWorldRope) return false;
        // We must be standing still (or approximately still) to use a winch.
        if (this.movement.rb.velocity.magnitude > standingTolerance) return false;
        bool hasRope = stationAt.HasRope();
        if (!hasRope) {
            if (!flags.Contains("rope")) return false;
            consumeInput = true;
            InteractButtonText = "Place Rope";
            if (Input.GetButtonDown("Interact")) {
                stationAt.MakeRope();
                // TODO: Take the rope from the player.
                return true;
            }
        } else {
            consumeInput = true;
            WinchButtonText = "Wind";
            ReleaseButtonText = "Unwind";
            if (!stationAt.ownRope) {
                InteractButtonText = "Remove Rope";
            }
            if (Input.GetButton("Winch")) {
                stationAt.Winch();
                return true;
            } else if (Input.GetButton("Release")) {
                stationAt.Release();
                return true;
            } else if (!stationAt.ownRope && Input.GetButtonDown("Interact")) {
                stationAt.TakeRope();
                // Remove any destroyed ropes.
                overlappingRopes.RemoveAll((collider) => collider == null);
                flags.Add("rope");
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
    bool HandleRope(ref bool consumeInput) {
        if (consumeInput) return false;
        if (ropeJoint == null) {
            grabbingInWorldRope = false;
            ropeJoint = null;
            proxy = null;
        }
        consumeInput = grabbingInWorldRope || overlappingRopes.Count > 0;
        if (consumeInput) {
            if (grabbingInWorldRope) {
                if (overlappingWinchables.Count != 0) {
                    InteractButtonText = "Attach Rope";
                } else {
                    InteractButtonText = "Drop Rope";
                }
            } else {
                InteractButtonText = "Grab Rope";
            }
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
        WinchButtonText = null;
        ReleaseButtonText = null;
        InteractButtonText = null;
        bool consumeInput = false;
        if (HandleDialog(ref consumeInput)) return;
        if (grounded) {
            if (UseWinch(ref consumeInput)) return;
        }
        if (HandleRope(ref consumeInput)) return;
        movement.HandleInput(grounded);
        UpdateUIControlsDisplay();
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