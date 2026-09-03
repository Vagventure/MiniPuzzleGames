using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreCounting : MonoBehaviour {

	private float timer = 0;
	private float score = 0;

	private bool starActive1 = false;
	private bool starActive2 = false;
	private bool starActive3 = false;

	public GameObject star1;
	public GameObject star2;
	public GameObject star3;

	public GameObject starFrame1;
	public GameObject starFrame2;
	public GameObject starFrame3;

	public GameObject scoreText;
	
	void Update () {
		timer += Time.deltaTime;
		if(timer >= 0.2f)	{
			timer = 0;
			score += 1;
		}

		if(score >= 2 && !starActive1) {
			starActive1 = true;
			star1.GetComponent<Image> ().color = new Color(1, 1, 1, 1);
			starFrame1.SetActive(true);
			GameObject.Find("starSound").GetComponent<AudioSource> ().Play();
		}
		if(score >= 5 && !starActive2) {
			starActive2 = true;
			star2.GetComponent<Image> ().color = new Color(1, 1, 1, 1);
			starFrame2.SetActive(true);
			GameObject.Find("starSound").GetComponent<AudioSource> ().Play();
		}
		if(score >= 9 && !starActive3) {
			starActive3 = true;
			star3.GetComponent<Image> ().color = new Color(1, 1, 1, 1);
			starFrame3.SetActive(true);
			GameObject.Find("starSound").GetComponent<AudioSource> ().Play();
		}

		if(score >= Vars.levelTimer) {
			GetComponent<ScoreCounting> ().enabled = false;
		}
	}
}
