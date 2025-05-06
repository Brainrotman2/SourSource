using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGen2 : MonoBehaviour
{
    [Header("Grid Settings")]
    public GameObject hexTilePrefab;
    public GameObject playerMarkerPrefab;
    public int gridRadius = 5;
    public float hexRadius = 1f;

    [Header("Player Settings")]
    public int numberOfPlayers = 6;
    public Color[] playerColors;
    public float blinkDuration = 0.2f;
    public int blinkCount = 4;

    [Header("Turn Settings")]
    public float playerTurnDuration = 3f;
    public float worldPhaseDuration = 6f;

    [Header("Camera Settings")]
    public float cameraDistance = 35f;
    public float cameraHeight = 15f;
    public float cameraTiltAngle = 35f;
    public float cameraTransitionSpeed = 2f;
    public float worldCameraRotateSpeed = 10f;

    [Header("World Event Token Settings")]
    public GameObject worldEventTokenPrefab;
    private GameObject worldEventTokenInstance;
    private List<List<Vector2Int>> hexRings = new List<List<Vector2Int>>();
    private int currentRing = 0;
    private int currentRingIndex = 0;

    private Dictionary<Vector2Int, GameObject> hexTiles = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<int, Vector2Int> playerStartPositions = new Dictionary<int, Vector2Int>();
    private Dictionary<int, GameObject> playerMarkers = new Dictionary<int, GameObject>();
    private Camera mainCamera;
    private int currentPlayerIndex = -1;
    private bool isCameraTransitioning = false;
    private Vector3 targetCameraPosition;
    private Quaternion targetCameraRotation;
    private float worldCameraAngle = 0f;

    private List<Vector2Int> directions = new List<Vector2Int>
    {
        new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1),
        new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1)
    };

    void Start()
    {
        mainCamera = Camera.main;
        GenerateHexGrid();
        GenerateHexRings();
        PlacePlayers();
        PlaceWorldEventToken();
        StartCoroutine(GameLoop());
    }

    void Update()
    {
        if (currentPlayerIndex == -1)
        {
            worldCameraAngle += Time.deltaTime * worldCameraRotateSpeed;
            float rad = Mathf.Deg2Rad * worldCameraAngle;
            Vector3 offset = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)) * cameraDistance;
            Vector3 pos = Vector3.zero + offset + Vector3.up * cameraHeight;
            mainCamera.transform.position = pos;
            mainCamera.transform.LookAt(Vector3.zero);
        }
        else if (isCameraTransitioning)
        {
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetCameraPosition, Time.deltaTime * cameraTransitionSpeed);
            mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, targetCameraRotation, Time.deltaTime * cameraTransitionSpeed);
            if (Vector3.Distance(mainCamera.transform.position, targetCameraPosition) < 0.1f &&
                Quaternion.Angle(mainCamera.transform.rotation, targetCameraRotation) < 1f)
            {
                isCameraTransitioning = false;
            }
        }
    }

    IEnumerator GameLoop()
    {
        while (true)
        {
            currentPlayerIndex = -1;
            yield return WorldPhase();

            for (int i = 0; i < numberOfPlayers; i++)
            {
                currentPlayerIndex = i;
                SetCameraToPlayer(i);
                yield return new WaitForSeconds(playerTurnDuration);
                SimulatePlayerAction(i);
            }
        }
    }

    IEnumerator WorldPhase()
    {
        worldCameraAngle = 0;
        float timer = 0f;

        int steps = Random.Range(1, 7);
        for (int i = 0; i < steps; i++)
        {
            currentRingIndex = (currentRingIndex + 1) % hexRings[currentRing].Count;

            Vector2Int hexCoord = hexRings[currentRing][currentRingIndex];
            if (hexTiles.ContainsKey(hexCoord))
            {
                Vector3 target = HexToWorld(hexCoord.x, hexCoord.y) + Vector3.up * 0.5f;
                yield return StartCoroutine(MoveTokenToPosition(worldEventTokenInstance, target, 0.25f));
            }
        }

        Vector2Int claimedHex = hexRings[currentRing][currentRingIndex];
        if (hexTiles.ContainsKey(claimedHex))
        {
            hexTiles[claimedHex].GetComponent<Renderer>().material.color = Color.magenta;
        }

        // Move inward after one full rotation
        if (currentRingIndex == 0)
        {
            currentRing++;
            if (currentRing >= hexRings.Count) currentRing = 0;
            currentRingIndex = 0;
        }

        while (timer < worldPhaseDuration)
        {
            timer += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator MoveTokenToPosition(GameObject token, Vector3 targetPos, float duration)
    {
        Vector3 start = token.transform.position;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float height = Mathf.Sin(t * Mathf.PI) * 0.5f;
            token.transform.position = Vector3.Lerp(start, targetPos, t) + Vector3.up * height;
            yield return null;
        }
        token.transform.position = targetPos;
    }

    void SimulatePlayerAction(int attackerIndex)
    {
        List<int> possibleTargets = new List<int>();
        for (int i = 0; i < numberOfPlayers; i++)
        {
            if (i != attackerIndex) possibleTargets.Add(i);
        }

        int targetIndex = possibleTargets[Random.Range(0, possibleTargets.Count)];
        StartCoroutine(BlinkPlayer(targetIndex));
    }

    IEnumerator BlinkPlayer(int playerIndex)
    {
        if (!playerMarkers.ContainsKey(playerIndex)) yield break;

        Renderer rend = playerMarkers[playerIndex].GetComponent<Renderer>();
        Color originalColor = playerColors[playerIndex];

        for (int i = 0; i < blinkCount; i++)
        {
            rend.material.color = Color.red;
            yield return new WaitForSeconds(blinkDuration);
            rend.material.color = originalColor;
            yield return new WaitForSeconds(blinkDuration);
        }
    }

    void SetCameraToPlayer(int index)
    {
        if (!playerStartPositions.ContainsKey(index)) return;
        Vector3 playerPos = HexToWorld(playerStartPositions[index].x, playerStartPositions[index].y);
        Vector3 direction = (playerPos - Vector3.zero).normalized;
        Vector3 camPos = playerPos + direction * cameraDistance + Vector3.up * cameraHeight;

        targetCameraPosition = camPos;
        targetCameraRotation = Quaternion.LookRotation(Vector3.zero - camPos, Vector3.up);
        isCameraTransitioning = true;
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

    void GenerateHexRings()
    {
        hexRings.Clear();
        for (int radius = gridRadius - 1; radius > 0; radius--)
        {
            List<Vector2Int> ring = new List<Vector2Int>();
            Vector2Int hex = new Vector2Int(0, -radius);
            foreach (var dir in directions)
            {
                for (int i = 0; i < radius; i++)
                {
                    ring.Add(hex);
                    hex += dir;
                }
            }
            hexRings.Add(ring);
        }
    }

    void PlaceWorldEventToken()
    {
        if (hexRings.Count == 0) return;

        Vector2Int start = hexRings[0][0];
        Vector3 pos = HexToWorld(start.x, start.y) + Vector3.up * 0.5f;
        Transform parentTile = hexTiles[start].transform;
        worldEventTokenInstance = Instantiate(worldEventTokenPrefab, parentTile);
        worldEventTokenInstance.transform.localPosition = new Vector3(0f, 0.5f, 0f); // above the tile
        worldEventTokenInstance.transform.localRotation = Quaternion.identity;
        currentRing = 0;
        currentRingIndex = 0;
    }

    void PlacePlayers()
    {
        float angleStep = 360f / numberOfPlayers;
        int ringDistance = gridRadius - 1;

        for (int i = 0; i < numberOfPlayers; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector2 worldOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ringDistance;
            Vector2Int axial = WorldToHex(worldOffset);
            Vector2Int startHex = FindNearestValidHex(axial);

            if (hexTiles.ContainsKey(startHex))
            {
                hexTiles[startHex].GetComponent<Renderer>().material.color = playerColors[i];
                playerStartPositions[i] = startHex;

                Vector3 world = HexToWorld(startHex.x, startHex.y);
                GameObject marker = Instantiate(playerMarkerPrefab, hexTiles[startHex].transform);
                marker.transform.localPosition = new Vector3(0f, 0.1f, 0f); // Slightly above the tile
                marker.transform.localRotation = Quaternion.identity;
                marker.GetComponent<Renderer>().material.color = playerColors[i];
                playerMarkers[i] = marker;
            }
        }
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

    Vector2Int FindNearestValidHex(Vector2Int start)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (hexTiles.ContainsKey(current)) return current;

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
}

