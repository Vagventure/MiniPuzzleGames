namespace RollingMaze
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine.SceneManagement;
	using UnityEngine;
	using UnityEngine.EventSystems;
	using PuzzleGame.Core;

	public class GameMenus : MonoBehaviour
	{

		public GameObject LevelCompletedDialog;
		public GameObject PauseButton;
		public GameObject PauseMenu;
		private AudioSource buttonClickSound;
		public GameObject MainMenu;
		public GameObject LevelSelectMenu;
		public GameObject SoundOffImage;

		void Start()
		{
			//PlayerPrefs.DeleteAll();
			Application.targetFrameRate = 300;
			Vars.levelScore = 0;
			buttonClickSound = GameObject.Find("buttonClickSound").GetComponent<AudioSource>();

			if (SceneManager.GetActiveScene().name.Equals("mainMenu"))
			{
				if (AudioListener.volume == 1)
				{
					SoundOffImage.SetActive(false);
				}
				else
				{
					SoundOffImage.SetActive(true);
				}
			}
		}

		public void ShowMainMenu()
		{
			buttonClickSound.Play();
			MainMenu.SetActive(true);
			LevelSelectMenu.SetActive(false);
		}

		public void ShowLevelSelectMenu()
		{
			buttonClickSound.Play();
			MainMenu.SetActive(false);
			LevelSelectMenu.SetActive(true);
		}

		public void SoundOnOff()
		{
			if (AudioListener.volume == 1)
			{
				AudioListener.volume = 0;
				SoundOffImage.SetActive(true);
			}
			else
			{
				AudioListener.volume = 1;
				SoundOffImage.SetActive(false);
			}
			buttonClickSound.Play();
		}

		public void LoadLevel()
		{
			buttonClickSound.Play();
			SceneManager.LoadScene(EventSystem.current.currentSelectedGameObject.name);
		}

		public void NextLevel()
		{
			buttonClickSound.Play();
			if (LevelFlowController.Instance != null)
			{
				LevelFlowController.Instance.RequestNextLevel();
				return;
			}
			string nextLevelNumber = (Int32.Parse(SceneManager.GetActiveScene().name) + 1).ToString();
			SceneManager.LoadScene(nextLevelNumber);
		}

		public void ExitToMainMenu()
		{
			buttonClickSound.Play();
			Time.timeScale = 1;
			if (LevelFlowController.Instance != null)
			{
				LevelFlowController.Instance.RequestMainMenu();
				return;
			}
			SceneManager.LoadScene("mainMenu");
		}

		public void LevelCompleted(float timer)
		{
			Invoke("ShowLevelCompleteDialog", timer);
		}

		private void ShowLevelCompleteDialog()
		{
			LevelCompletedDialog.SetActive(true);
			PauseButton.SetActive(false);
			GameObject.Find("levelCompleteSound").GetComponent<AudioSource>().Play();
			GameObject.Find("Level").GetComponent<LevelRotate>().enabled = false;
			Destroy(GameObject.Find("Balls"));

			int stars = 0;
			if (Vars.levelScore >= 200) stars = 1;
			if (Vars.levelScore >= 500) stars = 2;
			if (Vars.levelScore > 900) stars = 3;

			if (LevelFlowController.Instance != null)
			{
				LevelFlowController.Instance.NotifyLevelCompleted(stars);
			}
			else
			{
				int currLevel = Int32.Parse(SceneManager.GetActiveScene().name);
				if (PlayerPrefs.GetInt("levelUnlock", 0) < currLevel)
					PlayerPrefs.SetInt("levelUnlock", currLevel + 1);
				if (stars > PlayerPrefs.GetInt("level" + currLevel + "Stars"))
					PlayerPrefs.SetInt("level" + currLevel + "Stars", stars);
				if (PlayerPrefs.GetInt("levelUnlock") <= currLevel)
					PlayerPrefs.SetInt("levelUnlock", currLevel + 1);
			}
		}

		public void ShowPauseMenu()
		{
			GameObject.Find("pauseSound").GetComponent<AudioSource>().Play();
			PauseMenu.SetActive(true);
			PauseButton.SetActive(false);
			Time.timeScale = 0;
		}

		public void HidePauseMenu()
		{
			buttonClickSound.Play();
			PauseMenu.SetActive(false);
			PauseButton.SetActive(true);
			Time.timeScale = 1;
		}

		public void Reply()
		{
			buttonClickSound.Play();
			Time.timeScale = 1;
			if (LevelFlowController.Instance != null)
			{
				LevelFlowController.Instance.RequestRestart();
				return;
			}
			SceneManager.LoadScene(SceneManager.GetActiveScene().name);
		}

		public void Exit()
		{
			buttonClickSound.Play();
			Application.Quit();
		}
	}
}
