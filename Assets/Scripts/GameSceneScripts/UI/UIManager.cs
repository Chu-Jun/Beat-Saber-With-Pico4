using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI Canvas")]
    [SerializeField] private GameObject gameplayCanvas;
    [SerializeField] private GameObject gameOverCanvas;
    
    [Header("UI Canvas Components")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboCountText;
    [SerializeField] private TextMeshProUGUI multiplierText;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    
    [Header("Component Dependencies")]
    [SerializeField] private GameManager gameManager;
    
    [Header("Game Configuration")]
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private float healthLossOnMiss = 10f;
    [SerializeField] private float healthLossOnBadCut = 5f;
    [SerializeField] private int[] comboThresholds = {2, 4, 8};
    [SerializeField] private int[] multiplierValues = {1, 2, 4, 8};
    
    // Game state
    private float currentHealth;
    private int currentScore = 0;
    private int currentCombo = 0;
    private int currentMultiplier = 1;
    
    // Events
    public event System.Action OnPlayerHealthDepleted;
    
    // Properties for external access
    public int GetCurrentScore() => currentScore;
    public int GetCurrentCombo() => currentCombo;
    public int GetCurrentMultiplier() => currentMultiplier;
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public bool IsHealthDepleted() => currentHealth <= 0;
    
    private void Start()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
            
        ResetGameState();
    }
    
    public void ResetGameState()
    {
        currentHealth = maxHealth;
        currentScore = 0;
        currentCombo = 0;
        currentMultiplier = 1;
        
        UpdateHealthBar();
        UpdateScore();
        UpdateCombo();
        UpdateMultiplier();
        
        // Show gameplay UI, hide game over UI
        if (gameplayCanvas != null) gameplayCanvas.SetActive(true);
        if (gameOverCanvas != null) gameOverCanvas.SetActive(false);
    }
    
    // Scoring and gameplay methods
    public void OnNoteHit(int noteScore)
    {
        currentCombo++;
        
        int finalScore = noteScore * currentMultiplier;
        currentScore += finalScore;
        
        UpdateMultiplierBasedOnCombo();
        
        UpdateScore();
        UpdateCombo();
        UpdateMultiplier();
    }
    
    public void OnNoteCut(float cutAngle, float cutDistance, bool centerCut)
    {
        int score = CalculateBeatSaberScore(cutAngle, cutDistance, centerCut);
        OnNoteHit(score);
    }
    
    public void OnNoteMiss()
    {
        currentCombo = 0;
        currentMultiplier = 1;
        currentHealth = Mathf.Max(0, currentHealth - healthLossOnMiss);

        UpdateHealthBar();
        UpdateCombo();
        UpdateMultiplier();
        
        if (currentHealth <= 0)
        {
            OnPlayerHealthDepleted?.Invoke();
        }
    }
    
    public void OnBadCut()
    {
        currentCombo = 0;
        currentMultiplier = 1;
        currentHealth = Mathf.Max(0, currentHealth - healthLossOnBadCut);
        
        UpdateHealthBar();
        UpdateCombo();
        UpdateMultiplier();
        
        if (currentHealth <= 0)
        {
            OnPlayerHealthDepleted?.Invoke();
        }
    }
    
    private int CalculateBeatSaberScore(float cutAngle, float cutDistance, bool centerCut)
    {
        // Beat Saber scoring: 70 (angle) + 30 (accuracy) + 15 (center) = 115 max
        int angleScore = Mathf.RoundToInt(Mathf.Clamp01(cutAngle / 100f) * 70f);
        int accuracyScore = Mathf.RoundToInt(Mathf.Clamp01(1f - cutDistance) * 30f);
        int centerScore = centerCut ? 15 : 0;
        
        return angleScore + accuracyScore + centerScore;
    }
    
    private void UpdateMultiplierBasedOnCombo()
    {
        int newMultiplier = 1;
        
        for (int i = comboThresholds.Length - 1; i >= 0; i--)
        {
            if (currentCombo >= comboThresholds[i])
            {
                newMultiplier = multiplierValues[i + 1];
                break;
            }
        }
        
        if (newMultiplier != currentMultiplier)
        {
            currentMultiplier = newMultiplier;
            Debug.Log($"Multiplier increased to {currentMultiplier}x!");
        }
    }
    
    // UI Update methods
    private void UpdateHealthBar()
    {
        if (healthBar == null) return;
        
        float healthPercentage = currentHealth / maxHealth;
        healthBar.value = healthPercentage;
        
        var fillImage = healthBar.fillRect.GetComponent<Image>();
        if (fillImage != null)
        {
            if (healthPercentage > 0.5f)
                fillImage.color = Color.green;
            else if (healthPercentage > 0.25f)
                fillImage.color = Color.yellow;
            else
                fillImage.color = Color.red;
        }
    }
    
    private void UpdateScore()
    {
        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString("N0");
        }
    }
    
    private void UpdateCombo()
    {
        if (comboCountText == null) return;
        
        comboCountText.text = currentCombo.ToString();
        comboCountText.gameObject.SetActive(true);
        
        float scale = Mathf.Min(1.5f, 1f + (currentCombo * 0.01f));
        comboCountText.transform.localScale = Vector3.one * scale;
    }
    
    private void UpdateMultiplier()
    {
        if (multiplierText == null) return;
        
        multiplierText.text = "x" + currentMultiplier.ToString();
        
        // Beat Saber multiplier colors
        switch (currentMultiplier)
        {
            case 1:
                multiplierText.color = new Color(0.8f, 0.8f, 0.8f); // Gray
                break;
            case 2:
                multiplierText.color = Color.yellow;
                break;
            case 4:
                multiplierText.color = new Color(1f, 0.5f, 0f); // Orange
                break;
            case 8:
                multiplierText.color = Color.green;
                break;
        }
    }
    
    // Game Over Screen methods
    public void ShowGameOverScreen(int finalScore)
    {
        if (finalScoreText != null)
        {
            finalScoreText.text = $"Final Score: {finalScore:N0}";
        }
        
        if (gameplayCanvas != null) gameplayCanvas.SetActive(false);
        if (gameOverCanvas != null) gameOverCanvas.SetActive(true);
    }
    
    public void HideGameOverScreen()
    {
        if (gameplayCanvas != null) gameplayCanvas.SetActive(true);
        if (gameOverCanvas != null) gameOverCanvas.SetActive(false);
    }
    
    // UI Action methods - called by buttons
    public void RestartLevel()
    {
        ResetGameState();
        
        if (gameManager != null)
        {
            gameManager.RestartCurrentLevel();
        }
        else
        {
            Debug.LogError("UIManager: GameManager reference not found for restart");
        }
    }
    
    public void GoToMainMenu()
    {
        if (gameManager != null)
        {
            gameManager.GoToMainMenu();
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
