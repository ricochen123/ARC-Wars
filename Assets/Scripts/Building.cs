using UnityEngine;

public enum BuildingType
{
    City,
    Factory,
    HQ,
    Airport
}

public class Building : MonoBehaviour
{
    public BuildingType type = BuildingType.City;
    public Sprite neutralSprite;
    public Sprite player1Sprite;
    public Sprite player2Sprite;
    public Sprite player3Sprite;
    public Sprite player4Sprite;
    // -1 = neutral, 0 = Player 1, 1 = Player 2
    public int team = -1;

    [Header("Capture")]
    public int maxCapturePoints = 20;
    public int currentCapturePoints = 20;

    // Which team is currently trying to capture this (optional)
    private int capturingTeam = -2;   // -2 = nobody

    [Header("Visuals")]
    public SpriteRenderer spriteRenderer;


    [Header("Capture Bar")]
    public Transform captureBarFill;

    private void Awake()
    {
        currentCapturePoints = maxCapturePoints;
        UpdateSprite();
        UpdateCaptureBar(currentCapturePoints);
    }

    public void Capture(Unit capturingUnit)
    {
        team = capturingUnit.team;
        currentCapturePoints = maxCapturePoints;
        capturingTeam = -2; 
        UpdateSprite();
        UpdateCaptureBar(currentCapturePoints);
    }

    public bool ApplyCapture(Unit unit)
    {
        // If a different team starts capturing, reset the bar and switch capturingTeam
        if (capturingTeam != unit.team)
        {
            capturingTeam = unit.team;
            currentCapturePoints = maxCapturePoints;
        }

        int capturePower = unit.currentHP;         // set to unit.currentHP if christian wants it to scale based on hp instead
        currentCapturePoints -= capturePower;
        if (currentCapturePoints < 0) currentCapturePoints = 0;

        UpdateCaptureBar(currentCapturePoints);

        if (currentCapturePoints <= 0)
        {
            Capture(unit);
            return true; // captured this turn
        }
        unit.hasActedThisTurn = true; // Makes sure the unit cant attack while capturing lol
        return false; // still capturing over multiple turns
    }

private void UpdateSprite()
{
    if (spriteRenderer == null) return;

    switch (team)
    {
        case -1:
            if (neutralSprite != null)
                spriteRenderer.sprite = neutralSprite;
            break;
        case 0:
            if (player1Sprite != null)
                spriteRenderer.sprite = player1Sprite;
            break;
        case 1:
            if (player2Sprite != null)
                spriteRenderer.sprite = player2Sprite;
            break;
        case 2:
            if (player3Sprite != null)
                spriteRenderer.sprite = player3Sprite;
            break;
        case 3:
            if (player4Sprite != null)
                spriteRenderer.sprite = player4Sprite;
            break;
    }
}

    private void UpdateCaptureBar(int inHP)
    {
        Vector3 initScale = captureBarFill.localScale;

        float HPBar = (float)inHP / (float)maxCapturePoints;

        initScale.x = HPBar;

        captureBarFill.localScale = initScale;
    }
}
