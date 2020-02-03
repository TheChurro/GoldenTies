using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlagGiver : MonoBehaviour, OnFlagsChanged
{
    public string flag;
    void Start() {
        RoomManager manager = GameObject.Find("Main Camera").GetComponent<RoomManager>();
        if (manager.flags.Contains(flag)) {
            Destroy(this.gameObject);
        } else {
            manager.RegisterFlagsChangedHandler(this);
        }
    }

    public void FlagsChanged(RoomManager manager) {
        if (manager.flags.Contains(flag) && this != null && this.gameObject != null) {
            Destroy(this.gameObject);
        }
    }
}