using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // Called when "Tutorial" button is clicked
    public void StartTutorial()
    {
        // Load scene with build index 1
        SceneManager.LoadScene(1);
    }

    // Called when "Start Game" button is clicked
    public void StartGame()
    {
        // Load scene with build index 2
        SceneManager.LoadScene(2);
    }

    // Called when "Quit Game" button is clicked
    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }
}
