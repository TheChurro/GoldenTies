using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WinchStation : MonoBehaviour, OnRoomTransition
{
    public GameObject ropePrefab;
    public Transform ropeHangPoint;
    public Rope rope;
    public float wenchRate;
    public float releaseRate;
    public bool ownRope;
    public int UID;
    private bool hasRope;
    void Start() {
        var manager = FindObjectOfType<RoomManager>();
        if (manager != null) {
            manager.RegisterTransitionHandler(this);
        }
        if (ownRope) MakeRope();
        if (!ownRope && manager.GetBool(UID)) {
            MakeRope();
        }
    }
    public bool MakeRope() {
        if (rope == null) {
            var ropeObject = Instantiate(ropePrefab, ropeHangPoint.position, ropeHangPoint.rotation);
            rope = ropeObject.GetComponent<Rope>();
            hasRope = true;
            return true;
        }
        return false;
    }

    public bool TakeRope() {
        if (rope != null) {
            rope.Destroy();
            rope = null;
            hasRope = false;
            return true;
        }
        return false;
    }

    public void Winch() {
        if (rope != null) {
            rope.Wench(wenchRate * Time.deltaTime);
        }
    }

    public void Release() {
        if (rope != null) {
            rope.Release(releaseRate * Time.deltaTime);
        }
    }

    public bool HasRope() {
        return rope != null;
    }

    public void OnRoomTransition(RoomManager manager, bool willSave) {
        if (willSave && !ownRope && hasRope) {
            manager.SetBool(UID, hasRope);
        }
    }

    public void OnRoomSave(RoomManager manager) {
        if (!ownRope && hasRope) {
            manager.SetBool(UID, hasRope);
        }
    }
}
