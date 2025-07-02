using UnityEngine;
using TMPro; // Required if you chose TextMeshPro. If using Legacy UI Text, use: using UnityEngine.UI;
using System.Text; // For StringBuilder, which is efficient for string manipulation
using System.Collections.Generic; // For Queue

public class InGameConsole : MonoBehaviour
{
    [Tooltip("Assign the UI Text (TextMeshPro or Legacy) component here.")]
    public TextMeshProUGUI consoleText; // Change to 'public UnityEngine.UI.Text consoleText;' if using Legacy UI Text

    [Tooltip("Maximum number of lines to display in the console.")]
    [Range(5, 100)] // Allows setting a value between 5 and 100 in the Inspector
    public int maxLines = 15; // Default maximum lines to display

    // StringBuilder for efficient text concatenation
    private StringBuilder logBuilder = new StringBuilder();
    // Queue to store log messages, allowing us to easily remove old ones
    private Queue<string> logQueue = new Queue<string>();

    // This method is called when the GameObject becomes enabled and active.
    // We subscribe to the logMessageReceived event here.
    void OnEnable()
    {
        // Subscribe to Unity's log message event.
        // This method will be called every time Debug.Log, Debug.LogWarning, Debug.LogError is used.
        Application.logMessageReceived += HandleLog;
        Debug.Log("InGameConsole: Subscribed to log messages."); // This log will also appear!
    }

    // This method is called when the GameObject becomes disabled or inactive.
    // It's crucial to unsubscribe to prevent memory leaks and errors.
    void OnDisable()
    {
        // Unsubscribe from the log message event.
        Application.logMessageReceived -= HandleLog;
        Debug.Log("InGameConsole: Unsubscribed from log messages.");
    }

    // This method is the callback for Application.logMessageReceived.
    // It receives the log message, its stack trace, and its type (Log, Warning, Error).
    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Format the log message with its type for clarity
        string formattedLog = $"[{type}] {logString}";

        // Add the new formatted log message to the end of the queue
        logQueue.Enqueue(formattedLog);

        // If the number of log messages exceeds the maximum allowed lines,
        // remove the oldest message from the beginning of the queue.
        while (logQueue.Count > maxLines)
        {
            logQueue.Dequeue();
        }

        // Clear the StringBuilder to rebuild the console text from scratch
        logBuilder.Clear();

        // Iterate through all messages currently in the queue
        foreach (string log in logQueue)
        {
            // Append each log message followed by a new line
            logBuilder.AppendLine(log);
        }

        // Check if the consoleText UI element has been assigned in the Inspector
        if (consoleText != null)
        {
            // Update the UI Text component with the new consolidated log string
            consoleText.text = logBuilder.ToString();
        }
        else
        {
            // If consoleText is not assigned, log a warning to the Unity console
            // (This warning will not appear in our in-game console itself, as consoleText is null)
            Debug.LogWarning("InGameConsole: consoleText UI element is not assigned! Logs will not be displayed on Canvas.");
        }
    }
}