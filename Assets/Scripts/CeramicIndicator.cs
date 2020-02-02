using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CeramicIndicator : MonoBehaviour
{
    public int ceramicnumber;
    public TextMeshProUGUI typer;

    // Start is called before the first frame update
    void Start()
    {
        UpdateScore();
    }
    public void UpdateScore()
    {
        typer.text = ceramicnumber.ToString();
    }
}