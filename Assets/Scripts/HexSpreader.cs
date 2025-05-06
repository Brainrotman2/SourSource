using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HexSpreader : MonoBehaviour
{
    public GameObject hexPrefab;
    public float hexRadius = 1f;
    public int targetTiles = 300; // Should match 1 + 3r(r+1), e.g., 19, 37, 61, etc.
    public Color[] playerColors; // Must be 6 unique colors set in Inspector
    public float turnTime = 30f; // Seconds per player turn

    private Dictionary<Vector2Int, GameObject> hexMap = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<int, Player> players = new Dictionary<int, Player>();
    private Queue<int> turnQueue = new Queue<int>();
    private int gridRadius;

    private class Player
    {
        public int id;
        public Color color;
        public Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        public HashSet<Vector2Int> owned = new HashSet<Vector2Int>();
    }

    void Start()
    {
        gridRadius = GetBestFitRadius(targetTiles);
        GenerateGrid();
        InitializePlayers();
        StartCoroutine(SpreadRoutine());
    }

    void GenerateGrid()
    {
        for (int q = -gridRadius; q <= gridRadius; q++)
        {
            for (int r = -gridRadius; r <= gridRadius; r++)
            {
                Vector2Int coord = new Vector2Int(q, r);
                if (HexDistance(Vector2Int.zero, coord) <= gridRadius)
                {
                    Vector3 pos = HexToWorld(q, r);
                    GameObject hex = Instantiate(hexPrefab, pos, Quaternion.identity, this.transform);
                    hex.GetComponent<Renderer>().material.color = Color.white;
                    hexMap[coord] = hex;
                }
            }
        }
    }

    void InitializePlayers()
    {
        for (int i = 0; i < 6; i++)
        {
            float angle = i * Mathf.PI * 2 / 6;
            int q = Mathf.RoundToInt(gridRadius * Mathf.Cos(angle));
            int r = Mathf.RoundToInt(gridRadius * Mathf.Sin(angle));
            Vector2Int startHex = new Vector2Int(q, r);

            if (!hexMap.ContainsKey(startHex))
            {
                Debug.LogWarning($"Adjusted starting hex {startHex} not found. Searching fallback...");
                startHex = FindNearestFreeHex(startHex);
            }

            Player p = new Player
            {
                id = i,
                color = playerColors[i]
            };

            p.frontier.Enqueue(startHex);
            p.owned.Add(startHex);
            ColorHex(startHex, p.color);

            players[i] = p;
            turnQueue.Enqueue(i);
        }
    }

    IEnumerator SpreadRoutine()
    {
        while (true)
        {
            if (AllTilesClaimed())
                yield break;

            int currentPlayerId = turnQueue.Dequeue();
            Player player = players[currentPlayerId];

            if (player.frontier.Count > 0)
            {
                Vector2Int from = player.frontier.Dequeue();
                foreach (var neighbor in GetNeighbors(from))
                {
                    if (hexMap.ContainsKey(neighbor) && !IsClaimed(neighbor))
                    {
                        player.owned.Add(neighbor);
                        player.frontier.Enqueue(neighbor);
                        ColorHex(neighbor, player.color);
                        break; // Only one spread per turn
                    }
                }
            }

            turnQueue.Enqueue(currentPlayerId);
            yield return new WaitForSeconds(turnTime);
        }
    }

    bool IsClaimed(Vector2Int coord)
    {
        return hexMap[coord].GetComponent<Renderer>().material.color != Color.white;
    }

    void ColorHex(Vector2Int coord, Color color)
    {
        if (hexMap.TryGetValue(coord, out GameObject hex))
        {
            hex.GetComponent<Renderer>().material.color = color;
        }
    }

    Vector3 HexToWorld(int q, int r)
    {
        float x = hexRadius * Mathf.Sqrt(3) * (q + r / 2f);
        float z = hexRadius * 1.5f * r;
        return new Vector3(x, 0, z);
    }

    int HexDistance(Vector2Int a, Vector2Int b)
    {
        int dq = Mathf.Abs(a.x - b.x);
        int dr = Mathf.Abs(a.y - b.y);
        int ds = Mathf.Abs((-a.x - a.y) - (-b.x - b.y));
        return (dq + dr + ds) / 2;
    }

    int GetBestFitRadius(int target)
    {
        int bestRadius = 1;
        int closest = int.MaxValue;

        for (int r = 1; r < 50; r++)
        {
            int tiles = 1 + 3 * r * (r + 1);
            int diff = Mathf.Abs(tiles - target);
            if (diff < closest)
            {
                closest = diff;
                bestRadius = r;
            }
        }

        return bestRadius;
    }

    List<Vector2Int> GetNeighbors(Vector2Int hex)
    {
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1),
            new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1)
        };

        List<Vector2Int> result = new List<Vector2Int>();
        foreach (var dir in directions)
        {
            result.Add(hex + dir);
        }

        return result;
    }

    bool AllTilesClaimed()
    {
        foreach (var hex in hexMap.Values)
        {
            if (hex.GetComponent<Renderer>().material.color == Color.white)
                return false;
        }
        return true;
    }

    Vector2Int FindNearestFreeHex(Vector2Int from)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        queue.Enqueue(from);
        visited.Add(from);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (hexMap.ContainsKey(current) && !IsClaimed(current))
                return current;

            foreach (var neighbor in GetNeighbors(current))
            {
                if (!visited.Contains(neighbor))
                {
                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                }
            }
        }

        return Vector2Int.zero;
    }
}
