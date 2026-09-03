namespace RollingMaze
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    public class LevelEndTimer : MonoBehaviour {

    	public GameObject lEndTimer;
    	private Text levelEndTimerText;
    	private float timer = 6;

    	private float textAlphaTimer = 0;
    	private float textAlpha = 0;

    	void Start() {
    		levelEndTimerText = lEndTimer.GetComponent<Text>();
    		lEndTimer.SetActive(true);
    	}

    	void Update () {
    		timer -= Time.deltaTime;
    		levelEndTimerText.text = "" + (int)timer;
    		if(timer <= 1) {
    			GameObject.Find("GameManager").GetComponent<GameMenus> ().LevelCompleted(0); 
    			GameObject[] allBalls = GameObject.FindGameObjectsWithTag("ball");
    			for(int i = 0; i < allBalls.Length; i++) {
    				allBalls[i].GetComponent<Rigidbody2D>().isKinematic  = true;
    				allBalls[i].GetComponent<Rigidbody2D>().linearVelocity = new Vector2(0, 0);
    			}
    			GetComponent<LevelEndTimer> ().enabled = false;
    			lEndTimer.SetActive(false);
    		}
    		if(timer <= 4) {	
    			textAlphaTimer += Time.deltaTime;
    			if(textAlphaTimer >= 0.01f) {
    				textAlphaTimer = 0;
    				textAlpha+=0.01f;
    				levelEndTimerText.color = new Color(1, 1, 1, textAlpha);	
    			}
    		}
		
    	}
    }
}
