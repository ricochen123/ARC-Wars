// WHY MAKE MANY SCRIPT WHEN ONE DO TRICK.

// Commander Abilities:
// Commander 1: +40% attack damage
// Commander 2: Extra $500 income from buildings
// Commander 3: Units heal for 2hp every start of turn
// Commander 4: Takes 2 less damage

using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;

public class GameController : MonoBehaviour
{

    [Header("Board")]
    public Camera cam; // Main Camera (Only used in HandleMouseInput() for the mouse position relative to the camera
    public GridLayout grid; // Our map
    public Tilemap groundTilemap; // The actual part of our map that we can interact with
    public Tilemap highlightTilemap; // What we draw our moveHighlightTile to 
    public TileBase moveHighlightTile; // Sprite that will be drawn to our highlightTilemap (for our moverange)

    [Header("Building Count Display")]
    public TextMeshProUGUI playerBuildingCountText; // Player 1 has X buildings player 2 has x buildings yippee

    // midcell would technically be 0,0
    // mincell is bottom left of map (default map would make it -18, -10)
    // maxcell is top right of map (default map would make it 18, 10)
    [Header("Movement Limits (cell coords)")]
    public Vector3Int minCell;
    public Vector3Int maxCell;

    [Header("Turn Info")]
    public int currentPlayer;
    public int[] players;

    [Header("Gold")]
    public int startingGold;
    public int[] playerGold;
    public TextMeshProUGUI[] playerGoldText;

    [Header("Capture Attack Wait UI")]
    public GameObject CapAtkWaitPanel;
    // We make these buttons public because we want to be able to toggle them off/on depending on if the action is do-able or not
    public Button CaptureButton;
    public Button AttackButton;
    public Button WaitButton;

    [Header("Airport Purchase UI")]
    public GameObject airportMenuPanel;
    public Unit helicopterPrefab;

    [Header("Factory Purchase UI")]
    public GameObject factoryMenuPanel;
    public Unit infantryPrefab;
    public Unit tankPrefab;
    public Unit artilleryPrefab;

    [Header("Game Logic")]
    private bool isGameOver;
    public TextMeshProUGUI gameOverText;


    private Unit selectedUnit;
    private Vector3 old_unit_position; 
    private bool isChoosingAttackTarget = false;
    private Building currentFactory;

    private void Awake()
    {
        //Game States
        isGameOver = false;

        //Game Settings
        currentPlayer = 0;
        startingGold = 5000;
        
    }

    private void Start()
    {
        //these two lines successfully pull player info (# of players, which commanders were picked)
        var controller = SceneController.instance;
        players = controller.players;

        playerGold = new int[players.Length];

        for (int i = 0; i < playerGold.Length; i++)
        {
            playerGold[i] = startingGold;
        }


        TextMeshProUGUI[] allTextMeshes = FindObjectsOfType<TextMeshProUGUI>();

        var selectedTextMeshes = allTextMeshes
            .Where(text => text.CompareTag("gold")) // Select based on tag
            .OrderBy(text => text.name) // Optional: Order by name or another criteria
            .ToArray();

        playerGoldText = new TextMeshProUGUI[selectedTextMeshes.Length];
        for (int i = 0; i < selectedTextMeshes.Length; i++)
        {
            playerGoldText[i] = selectedTextMeshes[i];
        }


        if (factoryMenuPanel != null)
            factoryMenuPanel.SetActive(false);

        if (airportMenuPanel != null)
            airportMenuPanel.SetActive(false);

        if (CapAtkWaitPanel != null)
            CapAtkWaitPanel.SetActive(false);

        UpdateGoldUI();
        StartPlayerTurn();
        UpdatePlayerBuildingCounts();

        if (gameOverText != null)
            gameOverText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (isGameOver)
            return;   // ignore all input after game over
          
        HandleMouseInput();
    }

    // This function handles getting where we clicked on the screen (using the Main Camera that was passed in)
    private void HandleMouseInput()
    {
        // check if we are in the middle of choosing an enemy so redirect the input there!
        if (isChoosingAttackTarget)
        {
            HandleAttackTargetClick();
            return;
        }

        // We dont want to do an action until we have resolved either of the menus!
        if (CapAtkWaitPanel.activeSelf || factoryMenuPanel.activeSelf)
            if (Input.GetMouseButtonDown(1))
            {
                if (selectedUnit != null) selectedUnit.transform.position = old_unit_position; // revert our guy to the old spot he was at
                CloseFactoryMenu();
                CloseAirportMenu();
                CloseCapAtkWaitMenu();
                return;
            }
            else return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPos = cam.ScreenToWorldPoint(Input.mousePosition); // Translates our mouse position to a vector3

            Vector3Int cell = grid.WorldToCell(worldPos); // Translates our mouse position to the cell position of our map
            // I cant believe there is a unity function for this !!


            if (selectedUnit == null)
            {
                TrySelectUnit(worldPos);

                if (selectedUnit == null) // if we ended up selecting a unit from TrySelectUnit, this wont run!
                {
                    TryOpenFactoryAt(worldPos);
                }
            }
            else
            {
                // checks if we clicked on ourselves (useful if standing on a building so we can apply capture)
                Vector3Int unitCell = grid.WorldToCell(selectedUnit.transform.position);

                if (unitCell == cell)
                {
                    OpenCapAtkWaitMenu();
                    return;
                }

                if (selectedUnit != null)
                {
                    TryMoveSelectedUnitTo(cell);
                }
            }

        }
        else if (Input.GetMouseButtonDown(1))
        {
            ClearSelection();
        }

    }

    private void HandleAttackTargetClick()
    {
        // Left click: try to attack a clicked enemy
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            worldPos.z = 0f;

            bool attacked = TryAttackEnemyAt(worldPos);
            if (attacked)
            {
                // Finish turn for this unit
                if (selectedUnit != null)
                {
                    selectedUnit.hasActedThisTurn = true;
                    selectedUnit.RefreshTeamVisual();
                }

                CloseCapAtkWaitMenu();
            }
            else
            {
                Debug.Log("Invalid attack target or out of range.");
            }
        }
    }


    private void TrySelectUnit(Vector3 worldPos)
    {
        // CHECKS BEFORE WE CAN SET OUR selectedUnit = unit:

        // Checks to see if our mouse click collides with the box collider of our unit
        Collider2D hit = Physics2D.OverlapPoint(worldPos);
        if (hit == null) return;

        // If it does, then set the unit variable = to the unit at that position
        Unit unit = hit.GetComponent<Unit>();
        if (unit == null) return;

        // Making sure that the team of the unit is ours, and that that specific unit hasnt made an action already this turn
        if (unit.team != currentPlayer) return;
        if (unit.hasActedThisTurn) return;

        // END CHECKS

        selectedUnit = unit;
        old_unit_position = selectedUnit.transform.position;
        ShowMoveRange(unit);
    }

    private void ShowMoveRange(Unit unit)
    {
        // resetting our highlighttilemap to make sure nothing is rendered
        if (highlightTilemap == null || moveHighlightTile == null) return;
        highlightTilemap.ClearAllTiles();

        // origin gets the world position of our unit
        // range gets the moverange set to our unit (e.g. infantry is 3*) *subject to change
        Vector3Int origin = grid.WorldToCell(unit.transform.position);
        int range = unit.moveRange;

        for (int i = origin.x - range; i <= origin.x + range; i++) // our x value (farthest left possible tile to farthest right)
        {
            for (int j = origin.y - range; j <= origin.y + range; j++) // our y value (lowest down to highest up)
            {

                // Our movement system uses the Manhattan distance, which is when given 2 points, we sum the absolute values of the difference of the 
                // x coordinates, and the difference of the y coordinates

                // Example: Point A: (1, 3) 
                //          Point B: (3, 5)
                //          Manhattan distance = |1-3| + |3-5|


                Vector3Int cell = new Vector3Int(i, j, 0);

                // Must be within our map's cell range
                if (!IsCellWithinBounds(cell))
                    continue;

                // Must be a real ground tile (cant be a tree, river, etc.)
                if (!groundTilemap.HasTile(cell))
                    continue;

                // Calculate Manhattan Distance
                int dist = Mathf.Abs(cell.x - origin.x) + Mathf.Abs(cell.y - origin.y);
                if (dist > range)
                    continue;
                if (IsPathBlocked(origin, cell))
                    continue;
                // Convert the cell to the world position (which would = the bottom left of the cell)
                // Add 0.5f to get the exact center of the cell!
                Vector3 worldCenter = grid.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0f);

                // We converted the cell to the world position in order to interact with the Physics2D methods
                // Checks to see if there is a collider in the exact center of the cell
                Collider2D hit = Physics2D.OverlapPoint(worldCenter);

                // If the collider found something, we continue on to the next number j (or i)
                if (hit != null && hit.GetComponent<Unit>() != null)
                    continue;

                // If all checks passed, we highlight the tile!
                highlightTilemap.SetTile(cell, moveHighlightTile);
                highlightTilemap.color = new Color(1f, 1f, 1f, 0.3f);
            }
        }
    }
    // Checks if a path between two cells is blocked by another unit
    private bool IsPathBlocked(Vector3Int origin, Vector3Int target)
    {
        // Simple BFS pathfinding to see if there's a route within range avoiding other units
        Queue<Vector3Int> frontier = new Queue<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

        frontier.Enqueue(origin);
        visited.Add(origin);

        int maxRange = selectedUnit.moveRange;

        while (frontier.Count > 0)
        {
            Vector3Int current = frontier.Dequeue();

            // If we've reached the target, no blocking path
            if (current == target)
                return false;

            // Stop searching beyond move range
            int dist = Mathf.Abs(current.x - origin.x) + Mathf.Abs(current.y - origin.y);
            if (dist >= maxRange)
                continue;

            // Explore 4 orthogonal directions
            Vector3Int[] dirs = {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0)
        };

            foreach (var dir in dirs)
            {
                Vector3Int next = current + dir;
                if (visited.Contains(next)) continue;
                if (!IsCellWithinBounds(next)) continue;
                if (!groundTilemap.HasTile(next)) continue;

                // Block only if the tile has an ENEMY unit.
                // Friendly units are allowed to be stepped through for pathfinding.
                Vector3 nextWorld = grid.CellToWorld(next) + new Vector3(0.5f, 0.5f, 0f);
                Collider2D blocker = Physics2D.OverlapPoint(nextWorld);
                if (blocker != null)
                {
                    Unit blockingUnit = blocker.GetComponent<Unit>();
                    if (blockingUnit != null && blockingUnit.team != selectedUnit.team)
                    {
                        // Enemy blocks the path
                        continue;
                    }
                }

                frontier.Enqueue(next);
                visited.Add(next);
            }
        }

        // No path found within range
        return true;
    }


    private void TryMoveSelectedUnitTo(Vector3Int target)
    {
        if (selectedUnit == null) return;
        if (!IsCellWithinBounds(target)) return;
        if (!groundTilemap.HasTile(target)) return;

        Vector3Int origin = grid.WorldToCell(selectedUnit.transform.position);
        int moveRange = selectedUnit.moveRange;

        int dist = Mathf.Abs(origin.x - target.x) + Mathf.Abs(origin.y - target.y);
        if (dist > moveRange) return;

        // Prevent moving through blocking units along the path
        if (IsPathBlocked(origin, target))
        {
            OpenCapAtkWaitMenu();
            return;
        }

        // Don't move onto an occupied tile
        Vector3 stepWorldCenter = grid.CellToWorld(target) + new Vector3(0.5f, 0.5f, 0f);
        Collider2D unitBlocker = Physics2D.OverlapPoint(stepWorldCenter);
        if (unitBlocker != null && unitBlocker.GetComponent<Unit>() != null)
            return;

        // Actually move unit
        // old_unit_position = selectedUnit.transform.position;
        selectedUnit.transform.position = stepWorldCenter;

        // Flip sprite direction
        SpriteRenderer sr2 = selectedUnit.GetComponent<SpriteRenderer>();
        if (sr2 != null)
        {
            if (target.x < origin.x)
                sr2.flipX = true;
            else if (target.x > origin.x)
                sr2.flipX = false;
        }

        OpenCapAtkWaitMenu();
    }


    private bool TryAttackEnemyAt(Vector3 worldPos)
    {
        if (selectedUnit == null) return false;

        // Check if we clicked on a collider
        Collider2D hit = Physics2D.OverlapPoint(worldPos);
        if (hit == null) return false;

        // Check if the collider we clicked on belongs to a Unit
        Unit target = hit.GetComponent<Unit>();
        if (target == null) return false;

        // No friendly fire!
        if (target.team == selectedUnit.team)
            return false;

        // Check if that unit that we clicked on is 
        // 1. Within our attack range
        // 2. Within our attack range using the Manhattan Distance 🤓 <- Nerd Emoji
        Vector3Int attackerCell = grid.WorldToCell(selectedUnit.transform.position);
        Vector3Int targetCell = grid.WorldToCell(target.transform.position);
        int dist = Mathf.Abs(targetCell.x - attackerCell.x) + Mathf.Abs(targetCell.y - attackerCell.y);
        if (dist > selectedUnit.attackRange)
        {
            return false;
        }

        // Calculate damage based on unit type
        int damage = CalculateDamage(selectedUnit, target);
        target.TakeDamage(damage);
        if (selectedUnit.hitSound != null)
        selectedUnit.audioSource.PlayOneShot(selectedUnit.hitSound);

        Debug.Log(
            $"{selectedUnit.unitName} ({selectedUnit.unitType}) " +
            $"attacked {target.unitName} ({target.unitType}) for {damage} damage."
        );

        selectedUnit.hasActedThisTurn = true;
        selectedUnit.RefreshTeamVisual();
        ClearSelection();

        return true;
    }

    private int CalculateDamage(Unit attacker, Unit defender)
    {
        int damage = 0;

        // Depending on which unit you are
        // And which unit you are attacking

        // You will output different damage!
        if (attacker.unitType == UnitType.Infantry)
        {
            switch (defender.unitType)
            {
                case UnitType.Infantry:
                    damage = 5;
                    break;
                case UnitType.Tank:
                    damage = 3;
                    break;
                case UnitType.Artillery:
                    damage = 4;
                    break;
            }
        }
        else if (attacker.unitType == UnitType.Tank)
        {
            switch (defender.unitType)
            {
                case UnitType.Infantry:
                    damage = 8;
                    break;
                case UnitType.Tank:
                    damage = 6;
                    break;
                case UnitType.Artillery:
                    damage = 7;
                    break;
            }
        }
        else if (attacker.unitType == UnitType.Artillery)
        {
            switch (defender.unitType)
            {
                case UnitType.Infantry:
                    damage = 7;
                    break;
                case UnitType.Tank:
                    damage = 5;
                    break;
                case UnitType.Artillery:
                    damage = 6;
                    break;
                case UnitType.BattleHelicopter:
                    damage = 4;
                    break;
            }
        }
        else if (attacker.unitType == UnitType.BattleHelicopter)
        {
            switch (defender.unitType)
            {
                case UnitType.Infantry:
                    damage = 9;
                    break;
                case UnitType.Tank:
                    damage = 7;
                    break;
                case UnitType.Artillery:
                    damage = 8;
                    break;
                case UnitType.BattleHelicopter:
                    damage = 5;
                    break;
            }
        }

        damage = Mathf.RoundToInt(damage * (attacker.currentHP / (float)attacker.maxHP));

        // Attacking commander (Commander 1)
        int attackerCommander = players[attacker.team];

        if (attackerCommander == 1)
        {
            damage = Mathf.RoundToInt(damage * 1.4f);
        }

        // Defending commander (Commander 4)
        int defenderCommander = players[defender.team];

        if (defenderCommander == 4 && damage > 0)
        {
            damage = Mathf.Max(1, damage - 2);
        }

        // Like advance wars, our attacks scale based on the attackers current health
        return damage;
    }


    // Function that tells us if our cell is within the game boundaries by returning true or false
    private bool IsCellWithinBounds(Vector3Int cell)
    {
        return cell.x >= minCell.x && cell.x <= maxCell.x &&
               cell.y >= minCell.y && cell.y <= maxCell.y;
    }


    // Function to reset everything 
    private void ClearSelection()
    {
        selectedUnit = null;
        if (highlightTilemap != null)
            highlightTilemap.ClearAllTiles();
    }

    // Function called by an "End Turn" UI button
    public void EndTurn()
    {
        if (!CapAtkWaitPanel.activeSelf)
        {
            Unit[] units = FindObjectsOfType<Unit>();
            foreach(Unit u in units)
            {
                u.hasActedThisTurn = false;
                u.RefreshTeamVisual();
            }

            //currentPlayer = (currentPlayer == 0) ? 1 : 0; // if current player is 0 change it to 1 else change it to 0

            currentPlayer++;
            if(currentPlayer == players.Length)
            {
                currentPlayer = 0;
            }


            StartPlayerTurn();
            ClearSelection();
            CloseFactoryMenu();
            CloseAirportMenu();
        }
    }

    private void StartPlayerTurn()
    {
        // Reset action flags for the current player's units
        Unit[] units = FindObjectsOfType<Unit>(); // There somehow is always a unity function for it.
        foreach (Unit u in units)
        {
            if (u.team == currentPlayer)
            {
                u.hasActedThisTurn = false;
                Debug.Log($"Current Player: {currentPlayer} -- Players[currentPlayer]: {players[currentPlayer]}");
                if (players[currentPlayer] == 3)
                {
                    u.currentHP = Mathf.Min(u.maxHP, u.currentHP + 2);
                }
            }
        }
            //switch music to current commander's theme
         var currTheme = AudioManager.instance;
        currTheme.PlayMusic(players[currentPlayer]);

        // Give income from cities owned by current player
        GiveIncomeToCurrentPlayer();
        UpdateGoldUI();
    }

    private void GiveIncomeToCurrentPlayer()
    {
        int income = 0;
        Building[] buildings = FindObjectsOfType<Building>(); // I love unity functions
        foreach (Building b in buildings)
        {
            if (b.team == currentPlayer && b.type == BuildingType.City) // Only gives money to ppl that own cities
            {
                income += 1000; // Maybe good to make this a public variable, but advance wars is fixed to 1000, so *shrug*
                if (players[currentPlayer] == 2)
                {
                    income += 500;
                }
            }
        }

        playerGold[currentPlayer] += income;
    }

    private void UpdateGoldUI()
    {
        if (playerGoldText == null) return;

        for (int i = 0; i < players.Length && i < playerGoldText.Length; i++)
        {
            playerGoldText[i].text = $"P{i + 1} Gold: {playerGold[i]}";
        }
    }


    private void HealUnit(Unit inUnit)
    {
        if (inUnit.currentHP < inUnit.maxHP)
        {
            double deduceIncome = (float)inUnit.cost * 0.2;

            // Checks if the player cant afford it and they need more than the 1hp buy, then exit the loop
            if (playerGold[currentPlayer] < deduceIncome && (inUnit.currentHP += 2) <= inUnit.maxHP) return;

            // However, if they cant afford it but they need a 1hp buy, we allow them to enter the loop
            else if (playerGold[currentPlayer] > (float)inUnit.cost * 0.1)
            {

                inUnit.currentHP += 2;
                if (inUnit.currentHP > inUnit.maxHP)
                {
                    inUnit.currentHP = inUnit.maxHP;
                    deduceIncome = (float)inUnit.cost * 0.1;
                }

                playerGold[currentPlayer] -= (int)deduceIncome;

                Debug.Log($"Healed {inUnit.unitName} and charged Player {currentPlayer + 1} ${deduceIncome}");
            }
        }
    }

    /* Not necessarry with new capture attack wait method
     * private void ResolveOngoingCaptures() // Yes i am aware this code is essentially CheckForCapturesAt(param), this function is pretty much the same except we check all buildings rather than the one building we just stepped on to
    {
        Building[] buildings = FindObjectsOfType<Building>();

        foreach (Building b in buildings)
        {
            Vector3Int cell = grid.WorldToCell(b.transform.position); // Annoying but way to convert Vector3 (b.transform.position) to Vector3Int (worldtocell)
            Vector3 worldCenter = grid.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0f);

            // Collider2D and Unit types checking to see if there is
            // 1. a collider where our supposed building is 
            // 2. that collider is indeed a unit
            Collider2D hit = Physics2D.OverlapPoint(worldCenter);
            if (hit == null) continue;

            Unit unit = hit.GetComponent<Unit>();
            if (unit == null) continue;


            // If the building is owned by the player that's turn we are on, then we want to heal instead of capture.
            if (b.team == currentPlayer)
            {
                HealUnit(unit);
                continue;
            }

            // Check if the unit is on our team, because we only want to progress building capture on the proper players turn
            if (unit.team != currentPlayer) continue;

            // If all checks pass, apply that capture or tick that building!
            bool capturedNow = b.ApplyCapture(unit);

            if (capturedNow)
            {
                UpdatePlayerBuildingCounts();
                CheckForGameOver();

                Debug.Log("Building captured by Player " + (unit.team + 1) + " at start of turn.");
            }
            else
            {
                Debug.Log("Ongoing capture: " + b.currentCapturePoints + " capture HP left on " + b.name);
            }
        }
    }*/


    private void CheckForCaptureAt(Building inBuilding)
    {
        if (inBuilding.team == currentPlayer)
        {
            HealUnit(selectedUnit);
            return;
        }

        Unit unit = selectedUnit;

        bool capturedNow = inBuilding.ApplyCapture(unit);

        if (capturedNow)
        {
            inBuilding.Capture(unit);
            Debug.Log("Building captured by Player " + (unit.team + 1));

            UpdatePlayerBuildingCounts();
        }
        else
        {
            Debug.Log("Capturing in progress... " + inBuilding.currentCapturePoints + " capture HP left.");
        }

        CheckForGameOver();
    }

    public void UpdatePlayerBuildingCounts()
    {
        int p1 = 0;
        int p2 = 0;
        int p3 = 0;
        int p4 = 0;

        Building[] buildings = FindObjectsOfType<Building>();
        foreach (Building b in buildings)
        {
            if (b.team == 0) p1++;
            else if (b.team == 1) p2++;
            else if (b.team == 2) p3++;
            else if (b.team == 3) p4++;
        }

        if (playerBuildingCountText != null)
        {
            playerBuildingCountText.text =
                "P1 Buildings: " + p1 + " --- P2 Buildings: " + p2 + " --- P3 Buildings: " + p3 + " --- P4 Buildings: " + p4;
        }
    }


    // --------- FACTORY PURCHASING ----------

    private void TryOpenFactoryAt(Vector3 worldPos)
    {
        Collider2D hit = Physics2D.OverlapPoint(worldPos);
        if (hit == null) return;

        Building building = hit.GetComponent<Building>();
        if (building == null) return;
        if (building.type != BuildingType.Factory && building.type != BuildingType.Airport) return;
        if (building.team != currentPlayer) return;

        Vector3Int cell = grid.WorldToCell(building.transform.position);
        Vector3 worldCenter = grid.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0f);
        Collider2D uHit = Physics2D.OverlapPoint(worldCenter);
        if (uHit != null && uHit.GetComponent<Unit>() != null)
        {
            Debug.Log("Factory tile is occupied by a unit.");
            return;
        }

        if (building.type == BuildingType.Factory)
            OpenFactoryMenu(building);
        else if (building.type == BuildingType.Airport)
            OpenAirportMenu(building);
    }

    private void OpenFactoryMenu(Building factory)
    {
        currentFactory = factory;
        if (factoryMenuPanel != null)
            factoryMenuPanel.SetActive(true);
    }

    private void OpenAirportMenu(Building airport)
    {
        currentFactory = airport;
        if (airportMenuPanel != null)
            airportMenuPanel.SetActive(true);
    }

    public void CloseFactoryMenu()
    {
        currentFactory = null;
        if (factoryMenuPanel != null)
            factoryMenuPanel.SetActive(false);
    }

    public void CloseAirportMenu()
    {
        currentFactory = null;
        if (airportMenuPanel != null)
            airportMenuPanel.SetActive(false);
    }

    public void BuyInfantry()
    {
        TryBuyUnit(infantryPrefab);
    }

    public void BuyTank()
    {
        TryBuyUnit(tankPrefab);
    }

    public void BuyArtillery()
    {
        TryBuyUnit(artilleryPrefab);
    }

    public void BuyHelicopter()
    {
        TryBuyUnit(helicopterPrefab);
    }

    private void TryBuyUnit(Unit prefab)
    {
        if (currentFactory == null) return;
        if (prefab == null) return;

        if (playerGold[currentPlayer] < prefab.cost)
        {
            Debug.Log("Not enough gold.");
            return;
        }

        // Spawn unit on the tile the factory lives
        Vector3Int cell = grid.WorldToCell(currentFactory.transform.position);
        Vector3 spawnPos = grid.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0f);

        // Check if a unit is already there (just in case)
        Collider2D hit = Physics2D.OverlapPoint(spawnPos);
        if (hit != null && hit.GetComponent<Unit>() != null)
        {
            Debug.Log("Cannot spawn, tile occupied.");
            return;
        }

        playerGold[currentPlayer] -= prefab.cost;
        UpdateGoldUI();

        Unit newUnit = Instantiate(prefab, spawnPos, Quaternion.identity);
        newUnit.team = currentPlayer;
        newUnit.RefreshTeamVisual();

        Debug.Log($"Player {currentPlayer + 1} bought {newUnit.unitName} for {prefab.cost} gold.");

        CloseFactoryMenu();
        CloseAirportMenu();
    }

    private void GameOver(int winnerTeam)
    {
        isGameOver = true;

        string msg = "Player " + (winnerTeam + 1) + " Wins!";
        Debug.Log(msg);

        if (gameOverText != null)
        {
            gameOverText.text = msg;
            gameOverText.gameObject.SetActive(true);
        }
    }


    private void CheckForGameOver()
    {
        int p1Buildings = 0;
        int p2Buildings = 0;
        int doubleHQCapd = 0;

        Building[] buildings = FindObjectsOfType<Building>();
        foreach (Building b in buildings)
        {

            if (b.type == BuildingType.HQ && b.team == 0) // neat trick to see if both teams still have 1 HQ each, or if one team captured both!
            {
                doubleHQCapd++;
            }

            if (b.team == 0)
                p1Buildings++;
            else if (b.team == 1)
                p2Buildings++;
        }

        // Player 1 Win Con
        if (p2Buildings == 1 && p1Buildings > 0 || doubleHQCapd == 2) // Buildings default is 1 because of HQ counting as a building!
        {
            GameOver(0);
        }

        // Player 2 Win Con
        else if (p1Buildings == 1 && p2Buildings > 0 || doubleHQCapd == 0)
        {
            GameOver(1);
        }
    }




    // ---------- Capture Attack Wait ----------

    private void OpenCapAtkWaitMenu()
    {
        bool canCapture = CanUnitCaptureHere();
        bool canAttack = HasEnemyInRange();

        CaptureButton.gameObject.SetActive(canCapture);
        AttackButton.gameObject.SetActive(canAttack);
        WaitButton.gameObject.SetActive(true);

        CapAtkWaitPanel.SetActive(true);
    }

    private void CloseCapAtkWaitMenu()
    {
        CapAtkWaitPanel.SetActive(false);
        isChoosingAttackTarget = false;
        ClearSelection();
    }

    private bool CanUnitCaptureHere()
    {
        if (selectedUnit.unitType != UnitType.Infantry) return false;
        // Check building under the unit
        Building[] buildings = FindObjectsOfType<Building>();
        Vector3Int unitCell = grid.WorldToCell(selectedUnit.transform.position);

        foreach (Building bah in buildings)
        {
            Vector3Int buildingCell = grid.WorldToCell(bah.transform.position);

            if (buildingCell == unitCell)
            {
                return true;
            }
        }

        Vector3 centerWorld = grid.CellToWorld(unitCell) + new Vector3(0.5f, 0.5f, 0f);

        Collider2D hit = Physics2D.OverlapPoint(centerWorld);
        if (hit == null) return false;

        Building b = hit.GetComponent<Building>();
        if (b == null) return false;

        // Can't capture own building; only enemy or neutral
        if (b.team == selectedUnit.team) return false;

        return true;
    }

    private bool HasEnemyInRange()
    {
        Vector3Int origin = grid.WorldToCell(selectedUnit.transform.position);
        int range = selectedUnit.attackRange;

        Unit[] allUnits = FindObjectsOfType<Unit>();
        foreach (Unit other in allUnits)
        {
            if (other == selectedUnit) continue;
            if (other.team == selectedUnit.team) continue; // enemy only

            Vector3Int otherCell = grid.WorldToCell(other.transform.position);
            int dist = Mathf.Abs(otherCell.x - origin.x) + Mathf.Abs(otherCell.y - origin.y);

            if (dist <= range)
                return true;
        }

        return false;
    }

    public void OnActionCapture()
    {
        if (selectedUnit == null) return; // just in case

        Vector3Int cell = grid.WorldToCell(selectedUnit.transform.position);
        Vector3 worldCenter = grid.CellToWorld(cell); 

        Collider2D hit = Physics2D.OverlapPoint(worldCenter);
        if (hit != null)
        {
            Building building = hit.GetComponent<Building>();
            if (building != null)
            {
                CheckForCaptureAt(building);
            }
        }

        selectedUnit.hasActedThisTurn = true;
        selectedUnit.RefreshTeamVisual();
        CloseCapAtkWaitMenu();
    }

    public void OnActionAttack() // Works with ShowAttackRange(), HandleAttackTargetClick()
    {
        CapAtkWaitPanel.SetActive(false);

        isChoosingAttackTarget = true;

        ShowAttackRange();
    }

    private void ShowAttackRange() // basically showmoverange but only showattackrange!
    {
        if (highlightTilemap == null || moveHighlightTile == null) return;
        highlightTilemap.ClearAllTiles();

        Vector3Int origin = grid.WorldToCell(selectedUnit.transform.position);
        int range = selectedUnit.attackRange;

        for (int x = origin.x - range; x <= origin.x + range; x++)
        {
            for (int y = origin.y - range; y <= origin.y + range; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);

                if (!IsCellWithinBounds(cell))
                    continue;

                int dist = Mathf.Abs(cell.x - origin.x) + Mathf.Abs(cell.y - origin.y);
                if (dist == 0 || dist > range)
                    continue;

                highlightTilemap.SetTile(cell, moveHighlightTile);
                highlightTilemap.color = new Color(1f, 0.4f, 0.4f, 0.4f);
            }
        }
    }



    public void OnActionWait()
    {
        selectedUnit.hasActedThisTurn = true;
        selectedUnit.RefreshTeamVisual();

        CloseCapAtkWaitMenu();
    }


}
