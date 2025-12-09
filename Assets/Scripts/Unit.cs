using UnityEngine;


public enum UnitType
{
    Infantry,
    Tank,
    Artillery,
    BattleHelicopter,
}



public class Unit : MonoBehaviour
{

    [Header("Type")]
    public UnitType unitType = UnitType.Infantry;

    // Team 0 = Player 1 (Blue Team)
    // Team 1 = Player 2 (Red Team)
    [Header("Sprites per Team")]
    public SpriteRenderer bodyRenderer;
    public Sprite team0Sprite;
    public Sprite team1Sprite;
    public Sprite team2Sprite;
    public Sprite team3Sprite;
    public Sprite inactive;

    [Header("Audio")]
    public AudioClip hitSound;
    public AudioSource audioSource;

    [Header("Basic Info")]
    public string unitName = "Infantry";
    public int team = 0; 

    // Units stats, set as public so we can edit them on the fly to see which is most optimal
    // The default stats listed here are based on the original infantry test
    [Header("Stats")]
    public int maxHP = 10;
    public int currentHP = 10;
    public int moveRange = 3;
    public int attackRange = 1;
    public int cost = 1000;

    // Gives us the transform of our hp bar (We want to change the scaling of it, see UpdateHealthBar for more details)
    [Header("HP Bar")]
    public Transform healthBarFill;

    // Used by the GameController class, but makes sure that a unit cannot double action for the turn
    [HideInInspector] public bool hasActedThisTurn = false;

    private void Awake()
    {
        currentHP = maxHP;
        UpdateHealthBar(currentHP);
        RefreshTeamVisual();
        audioSource = GetComponent<AudioSource>();
    }

    // Function that calculates how much damage we should be taking (Called by the GameController class)
    public void TakeDamage(int amount)
    {
        currentHP -= amount;
        if (currentHP < 0) currentHP = 0;

        UpdateHealthBar(currentHP);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // When die destroy the object that was created (deallocation :nerd:)
    private void Die()
    {
        Destroy(gameObject);
    }

    private void Update()
    {
        UpdateHealthBar(currentHP);
    }

    // Function that lets us change the sprite depending on the team 
    // Team 0 is Player 1 (Blue)
    // Team 1 is Player 2 (Red)
    // There is also a default sprite renderer option for a neutral unit, but not really used as much (i might get rid of it and just set it default blue)
    public void RefreshTeamVisual()
    {
        if (bodyRenderer == null)
            bodyRenderer = GetComponent<SpriteRenderer>();

        if (bodyRenderer == null)
            return;

        Sprite chosen = null;

        if (hasActedThisTurn)
            chosen = inactive;
        else if (team == 0)
            chosen = team0Sprite;
        else if (team == 1)
            chosen = team1Sprite;
        else if (team == 2)
            chosen = team2Sprite;
        else if (team == 3)
            chosen = team3Sprite;

        if (chosen != null)
            bodyRenderer.sprite = chosen;
    }


    // Simple function that divides the currenthp by what the units max hp is suppose to be. This will get a value like 0.5 (currenthp = 5, maxhp = 10)
    // Then changes the hp bar's scale, which is a number based on 0-1 (0 being dead, 1 being maxhp)
    private void UpdateHealthBar(int inHP)
    {
        Vector3 initScale = healthBarFill.localScale;

        float HPBar = (float)inHP / (float)maxHP;

        initScale.x = HPBar;

        healthBarFill.localScale = initScale;
    }
}
