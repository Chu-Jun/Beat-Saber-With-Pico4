using UnityEngine;
using TMPro; // Required for TextMeshPro. If using Legacy UI Text, use: using UnityEngine.UI;
using System.Collections; // Required for Coroutines

public class InGameConsole : MonoBehaviour
{
    [Tooltip("Assign the UI Text (TextMeshPro or Legacy) component here.")]
    public TextMeshProUGUI consoleText; // Change to 'public UnityEngine.UI.Text consoleText;' if using Legacy UI Text

    [Tooltip("Keyword to filter for miss messages. Only messages containing this keyword will be displayed.")]
    public string missKeyword = "MISSED_BLOCK"; // You can change this keyword in the Inspector

    [Tooltip("How long the miss message should be displayed before fading out.")]
    [Range(0.5f, 5.0f)]
    public float displayDuration = 2.0f; // Message display duration in seconds

    // Private field to hold the currently running coroutine for hiding the text
    private Coroutine hideTextCoroutine;

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
        Debug.Log("InGameConsole: Subscribed to log messages. Filtering for misses.");

        // Ensure text is initially empty or hidden
        if (consoleText != null)
        {
            consoleText.text = "";
            consoleText.gameObject.SetActive(false); // Start hidden
        }
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
        Debug.Log("InGameConsole: Unsubscribed from log messages.");

        // Stop any running coroutine if disabled
        if (hideTextCoroutine != null)
        {
            StopCoroutine(hideTextCoroutine);
        }
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // IMPORTANT: Only process logs that contain our specific missKeyword.
        if (!logString.Contains(missKeyword))
        {
            return; // If the log string doesn't contain the keyword, ignore it.
        }

        // Format the log message: remove keyword and trim
        string displayedLog = logString.Replace(missKeyword, "").Trim();

        // Check if the consoleText UI element has been assigned in the Inspector
        if (consoleText != null)
        {
            // Stop any existing hide coroutine to immediately show the new message
            if (hideTextCoroutine != null)
            {
                StopCoroutine(hideTextCoroutine);
            }

            consoleText.text = displayedLog;
            consoleText.gameObject.SetActive(true); // Make sure it's visible

            // Start the coroutine to hide the text after displayDuration
            hideTextCoroutine = StartCoroutine(HideTextAfterDelay(displayDuration));
        }
        else
        {
            Debug.LogWarning("InGameConsole: consoleText UI element is not assigned! Logs will not be displayed on Canvas.");
        }
    }

    private IEnumerator HideTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (consoleText != null)
        {
            // Clear the text and make the GameObject inactive
            consoleText.text = "";
            consoleText.gameObject.SetActive(false);
        }
        hideTextCoroutine = null; // Clear the reference to the coroutine
    }
}