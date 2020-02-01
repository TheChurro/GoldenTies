using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomDoor : MonoBehaviour
{
    public string RoomTo;
    public string DoorTo;
    [System.NonSerialized]
    public RoomManager Manager;
    public enum DoorOrientation {
        North,
        East,
        South,
        West
    }
    public DoorOrientation EnterFrom;
    void OnTriggerEnter2D(Collider2D other) {
        if (Manager != null && other.tag == "Player") {
            Manager.ChangeRoom(RoomTo, DoorTo);
        }
    }
    public Vector3 GetEntryWorldOffset() {
        switch (this.EnterFrom) {
            case DoorOrientation.North: return Vector3.up;
            case DoorOrientation.East: return Vector3.right;
            case DoorOrientation.South: return Vector3.down;
            case DoorOrientation.West: return Vector3.left;
            default: return Vector3.zero;
        }
    }
}
