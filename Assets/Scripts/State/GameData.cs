using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    public float[] playerposition;
    public string activeroom;
    public bool[] collected;
    public string[] flags;
    public int[] boolStorageUID;
    public bool[] boolStorageVals;

    public GameData(RoomManager manager) 
    {
        playerposition = new float[3];
        playerposition[0] = manager.ActivePlayer.transform.position.x;
        playerposition[1] = manager.ActivePlayer.transform.position.y;
        playerposition[2] = manager.ActivePlayer.transform.position.z;

        activeroom = manager.ActiveRoomID;

        collected = new bool[1];
        collected[0] = manager.collected[0];

        flags = manager.flags.ToArray();
        boolStorageUID = new int[manager.SceneBools.Count];
        boolStorageVals = new bool[manager.SceneBools.Count];
        int i = 0;
        foreach (var kvp in manager.SceneBools) {
            boolStorageUID[i] = kvp.Key;
            boolStorageVals[i] = kvp.Value;
            i++;
        }
    }

}
