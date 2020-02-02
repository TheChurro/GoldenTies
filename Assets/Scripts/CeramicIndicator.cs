using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CeramicIndicator : MonoBehaviour, OnFlagsChanged
{
    public GameObject firstVasePiece;
    public GameObject secondVasePiece;
    public GameObject completedVasePiece;
    public RoomManager manager;
    void Start() {
        FlagsChanged(manager);
        manager.RegisterFlagsChangedHandler(this);
    }
    public void FlagsChanged(RoomManager manager)
    {
        if (manager.flags.Contains("Vase 1")) firstVasePiece.SetActive(true);
        else                                  firstVasePiece.SetActive(false);
        if (manager.flags.Contains("Vase 2")) secondVasePiece.SetActive(true);
        else                                  secondVasePiece.SetActive(false);
        if (manager.flags.Contains("Vase Complete")) completedVasePiece.SetActive(true);
        else                                         completedVasePiece.SetActive(false);
    }
}