using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimerCountDown : MonoBehaviour {

    private Text timerText;

    void Start() {
        timerText = GetComponent<Text> ();
    }

    void Update() {
        Vars.levelTimer -= Time.deltaTime;
        timerText.text = "" + (int)Vars.levelTimer;
        if(Vars.levelTimer <= 0) {
            GameObject.Find("GameManager").GetComponent<GameMenus> ().GameOver();
            GetComponent<TimerCountDown> ().enabled = false;
        }
    }
}
