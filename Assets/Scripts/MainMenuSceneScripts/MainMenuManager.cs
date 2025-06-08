using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // Public variable to hold the name of the game scene to load.
    // You can set this in the Unity Inspector.
    public string gameSceneName = "MainMenu"; // IMPORTANT: Change this to your actual game scene name

    public void StartGame()
    {
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            Debug.Log("Starting game, loading scene: " + gameSceneName);
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("Game Scene Name is not set in the MainMenuManager script!");
        }
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();

        // If running in the Unity Editor, Application.Quit() might not work as expected.
        // This line will stop a Play Mode session in the editor.
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}