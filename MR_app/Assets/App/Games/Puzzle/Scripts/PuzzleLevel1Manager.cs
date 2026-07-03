using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class PuzzleEventData
{
    public string event_type;      
    public string piece_name;      
    public int iteration;          
    public float time_since_start; 
}

public class PuzzleLevel1Manager : MonoBehaviour, IPuzzleLevelManager
{
    public enum GameState { Scanning, Playing, Evaluating }
    public GameState currentState = GameState.Scanning;

    [Header("Nastavení Iterací")]
    public GameObject[] iterationPrefabs;

    [Header("Nastavení Rotace Předlohy")]
    public float templateRotationOffset = -90f;

    [Header("Skener Stolu (Originál z Corsi)")]
    public float scanAreaSize = 1.0f;
    public int gridResolution = 10;
    public float minTableHeight = 0.4f;
    public float maxTableHeight = 1.2f;

    [Header("Bezpečné Spawnování Dílků")]
    public float spawnAreaSize = 0.5f;
    public float pieceRadius = 0.1f; // Tato hodnota se dynamicky předá do Space Optimizeru
    public int maxSpawnAttempts = 30;

    [Header("UI Text")]
    public GameObject statusTextPrefab;

    private int totalIterations;
    private int currentIterationIndex = 0;
    private int totalPiecesInCurrentPuzzle = 0;
    private int placedPiecesCount = 0;

    private GameObject currentSpawnedPuzzle;
    private Vector3 exactTableCenter;

    private GameObject spawnedCanvasObj;
    private TMP_Text statusText;
    private bool isGameRunning = false;

    private float levelStartTime; // Pro přesné měření kognitivního času

    public void StartLevel(int iterations)
    {
        totalIterations = iterations;
        currentIterationIndex = 0;
        isGameRunning = true;
        levelStartTime = Time.time; // Odstartujeme stopky levelu

        StartCoroutine(AutoScanRoutine());
    }

    IEnumerator AutoScanRoutine()
    {
        CreateStatusText("Skenuji stůl před vámi...");
        int scanAttempts = 0; // Počítadlo vteřin

        while (currentState == GameState.Scanning)
        {
            yield return new WaitForSeconds(1f);
            scanAttempts++;

            // Pokud to nenašlo stůl ani po 8 vteřinách
            if (scanAttempts >= 8)
            {
                UpdateStatusText("<color=red>Stůl nenalezen.</color>\nPodívejte se na stůl a restartujte hru.");
                

                // --- GLOBÁLNÍ LOG SELHÁNÍ SKENOVÁNÍ ---
                SessionManager.Instance?.SendSystemStatus("No_Table_Found", "MrPuzzle", 1, totalIterations);

                currentState = GameState.Evaluating;
                yield break;
            }

            GridScanForTable();
        }
    }

    void GridScanForTable()
    {
        Transform head = Camera.main.transform;
        Vector3 headForwardLevel = new Vector3(head.forward.x, 0, head.forward.z).normalized;
        Vector3 roughCenter = head.position + (headForwardLevel * 0.5f);

        List<Vector3> validTablePoints = new List<Vector3>();
        float dominantTableHeight = 0f;
        float step = this.scanAreaSize / gridResolution;
        float startX = roughCenter.x - (this.scanAreaSize / 2f);
        float startZ = roughCenter.z - (this.scanAreaSize / 2f);

        for (int x = 0; x <= gridResolution; x++)
        {
            for (int z = 0; z <= gridResolution; z++)
            {
                Vector3 rayOrigin = new Vector3(startX + (x * step), head.position.y, startZ + (z * step));
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 2f))
                {
                    if (Vector3.Dot(hit.normal, Vector3.up) > 0.9f && hit.point.y >= minTableHeight && hit.point.y <= maxTableHeight)
                    {
                        validTablePoints.Add(hit.point);
                        dominantTableHeight += hit.point.y;
                    }
                }
            }
        }

        if (validTablePoints.Count > 10)
        {
            dominantTableHeight /= validTablePoints.Count;
            exactTableCenter = CalculateCentroid(validTablePoints);
            exactTableCenter.y = dominantTableHeight;

            currentState = GameState.Playing;
            UpdateStatusText("Stůl nalezen! Připravuji hlavolam...");

            // --- GLOBÁLNÍ LOG ÚSPĚŠNÉHO STARTU HRY ---
            SessionManager.Instance?.SendSystemStatus("active", "MrPuzzle", 1, totalIterations);

            Invoke(nameof(StartFirstIteration), 1.5f);
        }
    }

    Vector3 CalculateCentroid(List<Vector3> points)
    {
        Vector3 centroid = Vector3.zero;
        foreach (Vector3 p in points) centroid += p;
        return centroid / points.Count;
    }

    void StartFirstIteration()
    {
        PlayIteration(currentIterationIndex);
    }

    void PlayIteration(int index)
    {
        if (index >= totalIterations || index >= iterationPrefabs.Length)
        {
            UpdateStatusText("<color=green>Level kompletní!</color>");
            isGameRunning = false;
            if (spawnedCanvasObj != null) Destroy(spawnedCanvasObj, 3f);
            return;
        }

        UpdateStatusText($"Složte dílky do tvaru (Kolo {index + 1}/{totalIterations})");

        if (currentSpawnedPuzzle != null) Destroy(currentSpawnedPuzzle);
        placedPiecesCount = 0;

        Vector3 spawnPos = exactTableCenter + new Vector3(0, 0.25f, 0);

        float finalYRotation = Camera.main.transform.eulerAngles.y + templateRotationOffset;
        Quaternion rotationFacingPlayer = Quaternion.Euler(0, finalYRotation, 0);

        // OPRAVA SIROTKŮ:
        currentSpawnedPuzzle = Instantiate(iterationPrefabs[index], spawnPos, rotationFacingPlayer, transform);

        PuzzlePiece[] pieces = currentSpawnedPuzzle.GetComponentsInChildren<PuzzlePiece>();
        totalPiecesInCurrentPuzzle = pieces.Length;

        Vector3 tableScatterCenter = exactTableCenter + new Vector3(0, 0.15f, 0);
        Vector3 scatterArea = new Vector3(spawnAreaSize, 0, spawnAreaSize);

        foreach (PuzzlePiece piece in pieces)
        {
            
            piece.SetupPiece(this);

            // Pokusíme se najít bezpečné místo (všimni si otazníku)
            Vector3? safePosOrNull = GetValidSpawnPosition(tableScatterCenter, scatterArea);

            if (safePosOrNull == null)
            {
                UpdateStatusText("<color=red>Nedostatek místa na stole.</color>\nUvolněte stůl a restartujte hru.");
                

                // --- GLOBÁLNÍ LOG NEDOSTATKU MÍSTA ---
                SessionManager.Instance?.SendSystemStatus("error_no_space", "MrPuzzle", 1, totalIterations);

                currentState = GameState.Evaluating;
                return;
            }

            // Pokud je to v pohodě, rozbalíme hodnotu z nullablu a pokračujeme normálně
            Vector3 safePos = safePosOrNull.Value;
            float randomYRot = Random.Range(0, 360);
            
            Quaternion scatterRot = Quaternion.Euler(0, randomYRot, 0);

            piece.transform.position = safePos;
            piece.transform.rotation = scatterRot;
        }
    }

    private Vector3? GetValidSpawnPosition(Vector3 center, Vector3 areaSize)
    {
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            float randomX = Random.Range(center.x - areaSize.x / 2, center.x + areaSize.x / 2);
            float randomZ = Random.Range(center.z - areaSize.z / 2, center.z + areaSize.z / 2);
            Vector3 randomPos = new Vector3(randomX, center.y, randomZ);

            if (SpaceOptimizer.Instance != null)
            {
                if (SpaceOptimizer.Instance.IsPositionSafe(center, randomPos, pieceRadius))
                    return randomPos;
            }
            else
            {
                if (!Physics.CheckSphere(randomPos, pieceRadius))
                    return randomPos;
            }
        }
        // Nenašlo to za 30-40 pokusů místo. Stůl je pravděpodobně plný nepořádku.
        return null;
    }

    public void PiecePlaced()
    {
        if (currentState != GameState.Playing) return;

        placedPiecesCount++;

        if (placedPiecesCount >= totalPiecesInCurrentPuzzle)
        {
            currentState = GameState.Evaluating;
            UpdateStatusText("<color=green>Výborně!</color>");
            StartCoroutine(WaitAndNextIteration());
        }
    }

    IEnumerator WaitAndNextIteration()
    {
        yield return new WaitForSeconds(2.0f);

        
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.NextIteration();
        }

        currentState = GameState.Playing;
        currentIterationIndex++;
        PlayIteration(currentIterationIndex);
    }

    
    public void LogEvent(string eventType, string pieceName)
    {
        if (SessionManager.Instance == null) return;

        PuzzleEventData eventPayload = new PuzzleEventData
        {
            event_type = eventType,
            piece_name = pieceName,
            iteration = SessionManager.Instance.CurrentIteration,
            time_since_start = Time.time - levelStartTime
        };

        // Vystřelíme okamžitý balíček události na tablet lékaře
        SessionManager.Instance.SendLog(eventPayload);
    }

    void CreateStatusText(string msg)
    {
        if (statusTextPrefab != null && spawnedCanvasObj == null)
        {
            spawnedCanvasObj = Instantiate(statusTextPrefab);
            statusText = spawnedCanvasObj.GetComponentInChildren<TMP_Text>();
        }
        UpdateStatusText(msg);
    }

    void UpdateStatusText(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    void Update()
    {
        if (spawnedCanvasObj != null && isGameRunning)
        {
            Transform head = Camera.main.transform;
            Vector3 targetPos = head.position + (head.forward * 0.8f);
            targetPos.y += 0.05f;

            spawnedCanvasObj.transform.position = Vector3.Lerp(spawnedCanvasObj.transform.position, targetPos, Time.deltaTime * 5f);
            spawnedCanvasObj.transform.LookAt(head);
            spawnedCanvasObj.transform.Rotate(0, 180, 0);
        }
    }

    void OnDestroy()
    {
        // Když se maže tento manažer (restart/přepnutí hry), 
        // bez milosti s sebou do hrobu vezme i ten létající Canvas
        if (spawnedCanvasObj != null)
        {
            Destroy(spawnedCanvasObj);
        }
    }
}