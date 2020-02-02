using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSelect : MonoBehaviour
{
    public void NewGame() {
        SceneManager.LoadScene("NewGame", LoadSceneMode.Single);
    }

    public void ContinueGame() {
        SceneManager.LoadScene("ContinueGame", LoadSceneMode.Single);
    }
}
