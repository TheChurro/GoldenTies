using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class McGuffin : MonoBehaviour
{
    public int guffinindex;
    // Start is called before the first frame update
    void Start()
    {
        GameData data = SaveSystem.Loadgame();
        if (data.collected[guffinindex])
        {
            Destroy(this.gameObject);
        }
    }
    
    void OnTriggerEnter2D(Collider2D col)
    {
        var ceram = GameObject.Find("NumCeramics").GetComponent<CeramicIndicator>();
        ceram.ceramicnumber++;
        ceram.UpdateScore();

        var manager = GameObject.Find("Main Camera").GetComponent<RoomManager>();
        manager.collected[guffinindex] = true;
        SaveSystem.Savegame(ceram, manager);
        Destroy(this.gameObject);
    }
}
