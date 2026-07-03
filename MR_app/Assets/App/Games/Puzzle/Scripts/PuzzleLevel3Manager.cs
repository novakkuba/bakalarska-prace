using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class DistractorSet
{
    [Tooltip("Sem vlož dílky, které se mají v této iteraci vygenerovat jako falešné.")]
    public GameObject[] distractors;
}

// IMPLEMENTOVÁNO ROZHRANÍ
public class PuzzleLevel3Manager : MonoBehaviour, IPuzzleLevelManager
{
    public enum GameState { Scanning, Playing, Evaluating }
    public GameState currentState = GameState.Scanning;

    [Header("Nastavení Iterací")]
    public GameObject[] iterationPrefabs;

    [Header("Nastavení Distraktorù (Návnady)")]
    public DistractorSet[] distractorsPerIteration;

    [Header("Nastavení Rotace Pøedlohy")]
    public float templateRotationOffsetX = 0f;
    public float templateRotationOffsetY = -90f;
    public float templateRotationOffsetZ = 0f;

    [Header("Skener Stolu (Originál z Corsi)")]
    public float scanAreaSize = 1.0f;
    public int gridResolution = 10;
    public float minTableHeight = 0.4f;
    public float maxTableHeight = 1.2f;

    [Header("Bezpeèné Spawnování Dílkù")]
    public float spawnAreaSize = 0.6f;
    public float pieceRadius = 0.1f;
    public int maxSpawnAttempts = 40;

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

    private float levelStartTime; // TELEMETRIE

    public void StartLevel(int iterations)
    {
        totalIterations = iterations;
        currentIterationIndex = 0;
        isGameRunning = true;
        levelStartTime = Time.time;

        StartCoroutine(AutoScanRoutine());
    }

    IEnumerator AutoScanRoutine()
    {
        CreateStatusText("Skenuji stùl pøed vámi...");
        int scanAttempts = 0; // Poèítadlo vteøin

        while (currentState == GameState.Scanning)
        {
            yield return new WaitForSeconds(1f);
            scanAttempts++;

            // Pokud to nenašlo stùl ani po 8 vteøinách
            
            if (scanAttempts >= 8)
            {
                UpdateStatusText("<color=red>Stùl nenalezen.</color>\nPodívejte se na stùl a restartujte hru.");
                

                // --- GLOBÁLNÍ LOG (Obtížnost 3) ---
                SessionManager.Instance?.SendSystemStatus("No_Table_Found", "MrPuzzle", 3, totalIterations);

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
            UpdateStatusText("Stùl nalezen! Pøipravuji hlavolam...");

            // --- GLOBÁLNÍ LOG (Obtížnost 3) ---
            SessionManager.Instance?.SendSystemStatus("active", "MrPuzzle", 3, totalIterations);

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
        float finalYRotation = Camera.main.transform.eulerAngles.y + templateRotationOffsetY;
        Quaternion rotationFacingPlayer = Quaternion.Euler(templateRotationOffsetX, finalYRotation, templateRotationOffsetZ);

        // OPRAVA SIROTKÙ: currentSpawnedPuzzle je dítìtem manažera
        currentSpawnedPuzzle = Instantiate(iterationPrefabs[index], spawnPos, rotationFacingPlayer, transform);

        PuzzlePiece[] pieces = currentSpawnedPuzzle.GetComponentsInChildren<PuzzlePiece>();
        totalPiecesInCurrentPuzzle = pieces.Length;

        Vector3 tableScatterCenter = exactTableCenter + new Vector3(0, 0.15f, 0);
        Vector3 scatterArea = new Vector3(spawnAreaSize, 0, spawnAreaSize);

        // 1. Spawnování SPRÁVNÝCH dílkù
        foreach (PuzzlePiece piece in pieces)
        {
            piece.SetupPiece(this);
            Vector3? safePosOrNull = GetValidSpawnPosition(tableScatterCenter, scatterArea);

            if (safePosOrNull == null)
            {
                UpdateStatusText("<color=red>Nedostatek místa na stole.</color>\nUvolnìte stùl a restartujte hru.");
                

                // --- GLOBÁLNÍ LOG (Obtížnost 3) ---
                SessionManager.Instance?.SendSystemStatus("error_no_space", "MrPuzzle", 3, totalIterations);

                currentState = GameState.Evaluating;
                return;
            }

            piece.transform.position = safePosOrNull.Value;
            piece.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        }

        // 2. SPAWNOVÁNÍ DISTRAKTORÙ S TELEMETRIÍ
        if (index < distractorsPerIteration.Length && distractorsPerIteration[index].distractors != null)
        {
            foreach (GameObject distractorPrefab in distractorsPerIteration[index].distractors)
            {
                if (distractorPrefab == null) continue;

                Vector3? distractorPosOrNull = GetValidSpawnPosition(tableScatterCenter, scatterArea);

                // ÚPLNÌ STEJNÁ POJISTKA I TADY:
                if (distractorPosOrNull == null)
                {
                    UpdateStatusText("<color=red>Nedostatek místa na stole.</color>\nUvolnìte stùl a restartujte hru.");
                    LogEvent("Spawn_Error", "Table_Full");

                    // --- GLOBÁLNÍ LOG (Obtížnost 3) ---
                    SessionManager.Instance?.SendSystemStatus("error_no_space", "MrPuzzle", 3, totalIterations);

                    currentState = GameState.Evaluating;
                    return;
                }

                Quaternion distractorRot = Quaternion.Euler(0, Random.Range(0, 360), 0);
                GameObject spawnedDistractor = Instantiate(distractorPrefab, distractorPosOrNull.Value, distractorRot, currentSpawnedPuzzle.transform);

                PuzzlePiece pieceScript = spawnedDistractor.GetComponent<PuzzlePiece>();
                if (pieceScript != null)
                {
                    pieceScript.isDistractor = true;
                    pieceScript.SetupPiece(this);
                }
            }
        }
    }

    // Pøidali jsme otazník k Vector3?
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
        // Nenašlo to za 30-40 pokusù místo. Stùl je pravdìpodobnì plný nepoøádku.
        return null;
    }

    public void PiecePlaced()
    {
        if (currentState != GameState.Playing) return;

        placedPiecesCount++;
        if (placedPiecesCount >= totalPiecesInCurrentPuzzle)
        {
            currentState = GameState.Evaluating;
            UpdateStatusText("<color=green>Výbornì!</color>");
            StartCoroutine(WaitAndNextIteration());
        }
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
        SessionManager.Instance.SendLog(eventPayload);
    }

    IEnumerator WaitAndNextIteration()
    {
        yield return new WaitForSeconds(2.0f);

        if (SessionManager.Instance != null) SessionManager.Instance.NextIteration();

        currentState = GameState.Playing;
        currentIterationIndex++;
        PlayIteration(currentIterationIndex);
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
        
        if (spawnedCanvasObj != null)
        {
            Destroy(spawnedCanvasObj);
        }
    }
}