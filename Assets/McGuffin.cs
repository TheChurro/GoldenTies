using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class McGuffin : MonoBehaviour
{
    // Start is called before the first frame update
    void OnTriggerEnter2D(Collider2D col)
    {
        var ceram = GameObject.Find("NumCeramics").GetComponent<CeramicIndicator>();
        ceram.ceramicnumber++;
        ceram.UpdateScore();
        Destroy(this.gameObject);
    }
}
