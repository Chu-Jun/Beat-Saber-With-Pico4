using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboCountText;
    [SerializeField] private TextMeshProUGUI multiplierText;
    
    [Header("UI Panels for Positioning")]
    [SerializeField] private RectTransform healthBarPanel;
    [SerializeField] private RectTransform leftPanel;
    [SerializeField] private RectTransform rightPanel;
    
    [Header("Game Values")]
    [SerializeField] private float maxHealth = 50f; // Beat Saber uses 50 energy
    private float currentHealth;
    private int currentScore = 0;
    private int currentCombo = 0;
    private int currentMultiplier = 1;
    
    [Header("Beat Saber Settings")]
    [SerializeField] private float healthLossOnMiss = 10f;
    [SerializeField] private float healthLossOnBadCut = 5f;
    [SerializeField] private int[] comboThresholds = {4, 8, 16}; // Beat Saber thresholds for 2x, 4x, 8x
    [SerializeField] private int[] multiplierValues = {1, 2, 4, 8};
    
    // [Header("Score Settings")]
    // [SerializeField] private int maxNoteScore = 115; // Max score per note in Beat Saber
    
    private void Start()
    {
        InitializeUI();
        SetupUIPositions();
    }
    
    private void InitializeUI()
    {
        currentHealth = maxHealth;
        currentScore = 0;
        currentCombo = 0;
        currentMultiplier = 1;
        
        UpdateHealthBar();
        UpdateScore();
        UpdateCombo();
        UpdateMultiplier();
    }
    
    private void SetupUIPositions()
    {
        // Position UI elements in Beat Saber style
        Canvas canvas = GetComponent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        
        // Health bar at top center
        if (healthBarPanel != null)
        {
            healthBarPanel.anchorMin = new Vector2(0.5f, 1f);
            healthBarPanel.anchorMax = new Vector2(0.5f, 1f);
            healthBarPanel.anchoredPosition = new Vector2(0, -50);
            healthBarPanel.sizeDelta = new Vector2(400, 20);
        }
        
        // Combo count at left
        if (leftPanel != null)
        {
            leftPanel.anchorMin = new Vector2(0f, 0.8f);
            leftPanel.anchorMax = new Vector2(0f, 0.8f);
            leftPanel.anchoredPosition = new Vector2(50, 0);
            leftPanel.sizeDelta = new Vector2(200, 100);
        }
        
        // Score and multiplier at right
        if (rightPanel != null)
        {
            rightPanel.anchorMin = new Vector2(1f, 0.8f);
            rightPanel.anchorMax = new Vector2(1f, 0.8f);
            rightPanel.anchoredPosition = new Vector2(-50, 0);
            rightPanel.sizeDelta = new Vector2(200, 120);
        }
    }
    
    // Call this when player hits a note with specific score
    public void OnNoteHit(int noteScore)
    {
        // Increase combo
        currentCombo++;
        
        // Add score with current multiplier
        int finalScore = noteScore * currentMultiplier;
        currentScore += finalScore;
        
        // Update multiplier based on combo (Beat Saber style)
        UpdateMultiplierBasedOnCombo();
        
        // Update UI immediately for real-time feedback
        UpdateScore();
        UpdateCombo();
        UpdateMultiplier();
    }
    
    // Call this when player cuts a note (with cut quality scoring)
    public void OnNoteCut(float cutAngle, float cutDistance, bool centerCut)
    {
        // Beat Saber scoring system
        int score = CalculateBeatSaberScore(cutAngle, cutDistance, centerCut);
        OnNoteHit(score);
    }
    
    // Call this when player misses a note
    public void OnNoteMiss()
    {
        // Break combo
        currentCombo = 0;
        currentMultiplier = 1;

        // Lose health
        currentHealth = Mathf.Max(0, currentHealth - healthLossOnMiss);

        // OnNoteHit(100);

        // Update UI
        UpdateHealthBar();
        UpdateCombo();
        UpdateMultiplier();
        
        // Check for fail condition
        if (currentHealth <= 0)
        {
            OnLevelFailed();
        }
    }
    
    // Call this for bad cuts that don't miss but aren't good
    public void OnBadCut()
    {
        // Break combo but less health loss
        currentCombo = 0;
        currentMultiplier = 1;
        
        // Smaller health loss
        currentHealth = Mathf.Max(0, currentHealth - healthLossOnBadCut);
        
        // Update UI
        UpdateHealthBar();
        UpdateCombo();
        UpdateMultiplier();
        
        if (currentHealth <= 0)
        {
            OnLevelFailed();
        }
    }
    
    private int CalculateBeatSaberScore(float cutAngle, float cutDistance, bool centerCut)
    {
        // Beat Saber scoring breakdown:
        // - Up to 70 points for cut angle (0-70)
        // - Up to 30 points for cut accuracy (0-30)  
        // - Up to 15 points for center cut (0-15)
        
        int angleScore = Mathf.RoundToInt(Mathf.Clamp01(cutAngle / 100f) * 70f);
        int accuracyScore = Mathf.RoundToInt(Mathf.Clamp01(1f - cutDistance) * 30f);
        int centerScore = centerCut ? 15 : 0;
        
        return angleScore + accuracyScore + centerScore;
    }
    
    private void UpdateMultiplierBasedOnCombo()
    {
        int newMultiplier = 1;
        
        // Beat Saber multiplier thresholds: 4, 8, 16 combo for 2x, 4x, 8x
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
            // Optional: Add multiplier increase effect
            OnMultiplierIncrease();
        }
    }
    
    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            float healthPercentage = currentHealth / maxHealth;
            healthBar.value = healthPercentage;
            
            // Beat Saber health bar colors
            Image fillImage = healthBar.fillRect.GetComponent<Image>();
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
    }
    
    private void UpdateScore()
    {
        if (scoreText != null)
        {
            // Format score with commas for readability
            scoreText.text = currentScore.ToString("N0");
        }
    }
    
    private void UpdateCombo()
    {
        if (comboCountText != null)
        {
            if (currentCombo >= 4) // Only show combo after reaching first multiplier threshold
            {
                comboCountText.text = currentCombo.ToString();
                comboCountText.gameObject.SetActive(true);
                
                // Scale text based on combo (visual feedback)
                float scale = Mathf.Min(1.5f, 1f + (currentCombo * 0.01f));
                comboCountText.transform.localScale = Vector3.one * scale;
            }
            else
            {
                comboCountText.gameObject.SetActive(false);
            }
        }
    }
    
    private void UpdateMultiplier()
    {
        if (multiplierText != null)
        {
            multiplierText.text = "x" + currentMultiplier.ToString();
            
            // Beat Saber multiplier colors
            switch (currentMultiplier)
            {
                case 1:
                    multiplierText.color = new Color(0.8f, 0.8f, 0.8f); // Gray
                    break;
                case 2:
                    multiplierText.color = new Color(1f, 1f, 0f); // Yellow
                    break;
                case 4:
                    multiplierText.color = new Color(1f, 0.5f, 0f); // Orange
                    break;
                case 8:
                    multiplierText.color = new Color(0f, 1f, 0f); // Green
                    break;
            }
        }
    }
    
    private void OnMultiplierIncrease()
    {
        // Optional: Add visual/audio feedback for multiplier increase
        Debug.Log($"Multiplier increased to {currentMultiplier}x!");
    }
    
    private void OnLevelFailed()
    {
        Debug.Log("Level Failed! Health depleted.");
        // Implement level failure logic
    }
    
    // Public getters for other systems
    public int GetCurrentScore() => currentScore;
    public int GetCurrentCombo() => currentCombo;
    public int GetCurrentMultiplier() => currentMultiplier;
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public bool IsHealthDepleted() => currentHealth <= 0;
}