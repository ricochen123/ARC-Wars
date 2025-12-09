using UnityEngine;

[CreateAssetMenu(menuName = "Units/UnitKind")]
public class UnitKind : ScriptableObject
{
    [Header("Display")]
    public string unitName;
    public Sprite team1;
    public Sprite team2;
    public Sprite team3;
    public Sprite team4;
    public Sprite inactive;

    [Header("Stats")]
    public int maxHP = 10;
    public int moveRange = 3;
    public int attackRange = 1;
    public int attackPower = 5;
    public int defense = 1;
    public int cost = 1000;

    [Header("Behavior Flags")]
    public bool canCapture = false;
    public bool isArmored = false;
}
