using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    public int ceramics;
    public float[] playerposition;
    public string activeroom;
    public bool[] collected;
    public string[] flags;

    public GameData(CeramicIndicator indicator,RoomManager manager) 
    {
        ceramics = indicator.ceramicnumber;

        playerposition = new float[3];
        playerposition[0] = manager.ActivePlayer.transform.position.x;
        playerposition[1] = manager.ActivePlayer.transform.position.y;
        playerposition[2] = manager.ActivePlayer.transform.position.z;

        activeroom = manager.ActiveRoomID;

        collected = new bool[1];
        collected[0] = manager.collected[0];

        flags = manager.ActivePlayer.GetComponent<PlayerController>().flags.ToArray();
    }

}
