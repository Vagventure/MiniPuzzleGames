using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallLogic : MonoBehaviour {

	public GameObject score;
	private bool levelEnd = false;
	GameObject target;

	void OnTriggerEnter2D(Collider2D col) {
		if(col.gameObject.name == "Hole") {
			GameObject.Find("Level").GetComponent<LevelRotate> ().enabled = false;
			GameObject.Find("GameManager").GetComponent<GameMenus> ().LevelCompleted();
			Destroy(GetComponent<Rigidbody2D> ());
			target = col.gameObject;
			levelEnd = true;
		}else if(col.gameObject.name == "obstacle" || col.gameObject.name == "LevelEndCollider") {
			GameObject.Find("GameManager").GetComponent<GameMenus> ().GameOver();
		}
    }

	void FixedUpdate() {
		if(levelEnd) {
        	transform.position = Vector2.MoveTowards(transform.position, target.transform.position, 0.02f);
		}
    }
}
