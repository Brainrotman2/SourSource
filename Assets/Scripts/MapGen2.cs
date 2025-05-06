using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapGen2 : MonoBehaviour
{
    public GameObject hexTilePrefab;
    public int gridRadius = 5;
    public float hexRadius = 1f;

    public int numberOfPlayers = 6;
    public Color[] playerColors;
    public TextMeshProUGUI phaseText;

    public float playerTurnDuration = 2f;
    public float worldTurnDuration = 1f;
    public int maxHealth = 20;

    private Dictionary<Vector2Int, GameObject> hexTiles = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<int, List<Vector2Int>> playerTiles = new Dictionary<int, List<Vector2Int>>();
    private Dictionary<int, int> playerHealth = new Dictionary<int, int>();

    private Dictionary<int, int> retaliationDamage = new Dictionary<int, int>();
    private Dictionary<int, int> retaliationFrom = new Dictionary<int, int>();

    private List<Vector2Int> directions = new List<Vector2Int>
    {
        new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1),
        new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1)
    };

    private int currentPlayerIndex = 0;
    private bool inWorldPhase = false;
    private float phaseTimer;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        GenerateHexGrid();
        PlacePlayers();
        InitializeHealth();
        phaseTimer = playerTurnDuration;
        MoveCameraToPlayer(currentPlayerIndex);
        UpdateUI();
    }

    void Update()
    {
        phaseTimer -= Time.deltaTime;
        if (phaseTimer <= 0)
        {
            if (inWorldPhase)
            {
                RunWorldPhase();
                currentPlayerIndex = 0;
                inWorldPhase = false;
                phaseTimer = playerTurnDuration;
                MoveCameraToPlayer(currentPlayerIndex);
            }
            else
            {
                StartCoroutine(HandleTurn());
            }
            UpdateUI();
        }
    }

    void GenerateHexGrid()
    {
        for (int q = -gridRadius; q <= gridRadius; q++)
        {
            int r1 = Mathf.Max(-gridRadius, -q - gridRadius);
            int r2 = Mathf.Min(gridRadius, -q + gridRadius);
            for (int r = r1; r <= r2; r++)
            {
                if (q == 0 && r == 0) continue;
                Vector3 pos = HexToWorld(q, r);
                GameObject hex = Instantiate(hexTilePrefab, pos, Quaternion.identity, transform);
                hexTiles[new Vector2Int(q, r)] = hex;
            }
        }
    }

    void PlacePlayers()
    {
        float angleStep = 360f / numberOfPlayers;
        float distance = gridRadius - 1;

        for (int i = 0; i < numberOfPlayers; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector2 worldPos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            Vector2Int axial = WorldToHex(worldPos);
            Vector2Int startHex = FindNearestValidHex(axial, i);

            if (hexTiles.ContainsKey(startHex))
            {
                hexTiles[startHex].GetComponent<Renderer>().material.color = playerColors[i];
                playerTiles[i] = new List<Vector2Int> { startHex };
            }
        }
    }

    void InitializeHealth()
    {
        for (int i = 0; i < numberOfPlayers; i++)
            playerHealth[i] = maxHealth;
    }

    IEnumerator HandleTurn()
    {
        int current = currentPlayerIndex;

        if (retaliationDamage.ContainsKey(current))
        {
            int attacker = retaliationFrom[current];
            int damageToRetaliate = retaliationDamage[current];

            MoveCameraToPlayer(current);
            yield return new WaitForSeconds(0.5f);
            yield return RunRetaliationPhase(attacker, current, damageToRetaliate);

            retaliationDamage.Remove(current);
            retaliationFrom.Remove(current);
            yield return new WaitForSeconds(0.5f);
        }

        if (playerHealth[current] <= 0)
        {
            currentPlayerIndex++;
            phaseTimer = playerTurnDuration;
            UpdateUI();
            yield break;
        }

        int defender;
        int attempts = 100;
        do
        {
            defender = Random.Range(0, numberOfPlayers);
            attempts--;
        } while ((defender == current || playerHealth[defender] <= 0) && attempts > 0);

        MoveCameraToPlayer(current);
        yield return new WaitForSeconds(1f);

        int damage = Random.Range(1, 6);
        playerHealth[defender] = Mathf.Max(0, playerHealth[defender] - damage);

        retaliationDamage[defender] = damage;
        retaliationFrom[defender] = current;

        yield return new WaitForSeconds(0.5f);
        StartCoroutine(FlashPlayerTiles(defender));
        yield return new WaitForSeconds(1f);

        currentPlayerIndex++;
        if (currentPlayerIndex >= numberOfPlayers)
        {
            inWorldPhase = true;
            phaseTimer = worldTurnDuration;
            StartCoroutine(SpinCamera());
        }
        else
        {
            phaseTimer = playerTurnDuration;
            MoveCameraToPlayer(currentPlayerIndex);
        }

        UpdateUI();
    }

    IEnumerator RunRetaliationPhase(int attacker, int defender, int damageReceived)
    {
        yield return new WaitForSeconds(0.5f);
        MoveCameraToPlayer(defender);
        yield return new WaitForSeconds(1f);

        int defense = Random.Range(1, 6);
        int reducedDamage = Mathf.Max(0, damageReceived - defense);

        playerHealth[attacker] = Mathf.Max(0, playerHealth[attacker] - reducedDamage);
        StartCoroutine(FlashPlayerTiles(attacker));
        yield return new WaitForSeconds(1f);
    }

    void RunWorldPhase()
    {
        foreach (var kvp in playerTiles)
        {
            int player = kvp.Key;
            List<Vector2Int> ownedTiles = kvp.Value;
            HashSet<Vector2Int> newTiles = new HashSet<Vector2Int>();

            foreach (Vector2Int tile in ownedTiles)
            {
                foreach (Vector2Int dir in directions)
                {
                    Vector2Int neighbor = tile + dir;
                    if (!hexTiles.ContainsKey(neighbor)) continue;
                    if (IsTileClaimed(neighbor)) continue;
                    if (GetSliceIndex(neighbor) != player) continue;

                    newTiles.Add(neighbor);
                }
            }

            foreach (var tile in newTiles)
            {
                hexTiles[tile].GetComponent<Renderer>().material.color = playerColors[player];
                playerTiles[player].Add(tile);
            }
        }
    }

    bool IsTileClaimed(Vector2Int coord)
    {
        foreach (var list in playerTiles.Values)
            if (list.Contains(coord)) return true;
        return false;
    }

    int GetSliceIndex(Vector2Int coord)
    {
        float angle = Mathf.Atan2(coord.y, coord.x) * Mathf.Rad2Deg;
        angle = (angle + 360f) % 360f;
        float slice = 360f / numberOfPlayers;
        return Mathf.FloorToInt(angle / slice);
    }

    Vector3 HexToWorld(int q, int r)
    {
        float x = hexRadius * (Mathf.Sqrt(3f) * q + Mathf.Sqrt(3f) / 2f * r);
        float z = hexRadius * (3f / 2f * r);
        return new Vector3(x, 0, z);
    }

    Vector2Int WorldToHex(Vector2 world)
    {
        float q = (Mathf.Sqrt(3f) / 3f * world.x - 1f / 3f * world.y) / hexRadius;
        float r = (2f / 3f * world.y) / hexRadius;
        return HexRound(new Vector2(q, r));
    }

    Vector2Int HexRound(Vector2 hex)
    {
        float x = hex.x;
        float z = hex.y;
        float y = -x - z;

        int rx = Mathf.RoundToInt(x);
        int ry = Mathf.RoundToInt(y);
        int rz = Mathf.RoundToInt(z);

        float dx = Mathf.Abs(rx - x);
        float dy = Mathf.Abs(ry - y);
        float dz = Mathf.Abs(rz - z);

        if (dx > dy && dx > dz) rx = -ry - rz;
        else if (dy > dz) ry = -rx - rz;
        else rz = -rx - ry;

        return new Vector2Int(rx, rz);
    }

    Vector2Int FindNearestValidHex(Vector2Int start, int playerIndex)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (hexTiles.ContainsKey(current) && GetSliceIndex(current) == playerIndex)
                return current;

            foreach (var dir in directions)
            {
                Vector2Int next = current + dir;
                if (!visited.Contains(next))
                {
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
        }

        return start;
    }

    IEnumerator FlashPlayerTiles(int player)
    {
        foreach (Vector2Int coord in playerTiles[player])
            hexTiles[coord].GetComponent<Renderer>().material.color = Color.red;

        yield return new WaitForSeconds(0.3f);

        foreach (Vector2Int coord in playerTiles[player])
            hexTiles[coord].GetComponent<Renderer>().material.color = playerColors[player];
    }

    IEnumerator SpinCamera()
    {
        float duration = 3f;
        float angleStep = 360f / numberOfPlayers;
        float startAngle = angleStep * currentPlayerIndex;
        float endAngle = angleStep * 0;

        float radius = (gridRadius + 2) * hexRadius;
        float t = 0f;

        while (t < duration)
        {
            float angle = Mathf.Lerp(startAngle, endAngle + 360f, t / duration) % 360f;
            float rad = angle * Mathf.Deg2Rad;

            Vector3 pos = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * radius;
            pos.y = 10;
            mainCamera.transform.position = pos;
            mainCamera.transform.LookAt(Vector3.zero);

            t += Time.deltaTime;
            yield return null;
        }

        MoveCameraToPlayer(0); // reset for first player
    }

    void MoveCameraToPlayer(int index)
    {
        float angleStep = 360f / numberOfPlayers;
        float angle = angleStep * index * Mathf.Deg2Rad;

        float radius = (gridRadius + 2) * hexRadius;
        Vector3 position = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
        position.y = 10;

        mainCamera.transform.position = position;
        mainCamera.transform.LookAt(Vector3.zero);
    }

    void UpdateUI()
    {
        string text = inWorldPhase ? "World Phase\n" : $"Player {currentPlayerIndex + 1} Turn\n";
        for (int i = 0; i < numberOfPlayers; i++)
            text += $"P{i + 1}: {playerHealth[i]} HP\n";
        phaseText.text = text;
    }
}