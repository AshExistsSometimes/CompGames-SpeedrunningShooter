using UnityEngine;
using UnityEngine.SceneManagement;

public class WinScreenManager : MonoBehaviour
{
    // Called when "Return to Main Menu" button is clicked
    public void ReturnToMainMenu()
    {
        // Load the first scene in build settings
        SceneManager.LoadScene(0);
    }

    // Called when "Quit Game" button is clicked
    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }
}
