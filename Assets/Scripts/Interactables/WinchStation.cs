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
    void Start() {
        var manager = FindObjectOfType<RoomManager>();
        if (manager != null) {
            manager.RegisterTransitionHandler(this);
        }
    }
    public bool MakeRope() {
        if (rope == null) {
            var ropeObject = Instantiate(ropePrefab, ropeHangPoint.position, ropeHangPoint.rotation);
            rope = ropeObject.GetComponent<Rope>();
            return true;
        }
        return false;
    }

    public bool TakeRope() {
        if (rope != null) {
            rope.Destroy();
            rope = null;
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
        // TODO: Write to save data whether this object has a rope.
    }
}
