using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;


public class CorsiLevel2Manager : MonoBehaviour, ICorsiLevelManager
{
    public enum GameState { Scanning, ShowingSequence, WaitingForPlayer, Evaluating }
    public GameState currentState = GameState.Scanning;

    [Header("Nastavení Corsi desky")]
    [Tooltip("Sem vlož svùj nový MusicCorsiBlockPrefab!")]
    public GameObject blockPrefab;
    public int totalBlocks = 9;
    public float boardWidth = 0.5f;
    public float boardDepth = 0.35f;
    public float minBlockSpacing = 0.12f;

    [Header("Hudební nastavení (Level 2)")]
    private readonly float[] pentatonicPitches = { 1.0f, 1.125f, 1.25f, 1.5f, 1.666f, 2.0f };

    [Header("UI a Skener")]
    public GameObject statusTextPrefab;
    public float scanAreaSize = 1.5f;
    public int gridResolution = 15;
    public float minTableHeight = 0.4f;
    public float maxTableHeight = 1.2f;
    public float spawnHeightOffset = 0.02f;

    private List<MusicCorsiBlock> spawnedBlocks = new List<MusicCorsiBlock>();
    private List<int> targetSequence = new List<int>();
    private int playerInputIndex = 0;

    private int maxIterations;
    private int currentIteration;

    private GameObject spawnedCanvasObj;
    private TMP_Text statusText;
    private bool isGameRunning = false;

    private float levelStartTime; // TELEMETRIE

    public void StartLevel(int totalIterations)
    {
        maxIterations = totalIterations;
        currentIteration = 1;
        isGameRunning = true;
        levelStartTime = Time.time;

        StartCoroutine(AutoScanRoutine());
    }

    public void StopGame()
    {
        isGameRunning = false;
        StopAllCoroutines();
        foreach (var b in spawnedBlocks) { if (b != null) Destroy(b.gameObject); }
        spawnedBlocks.Clear();
        if (spawnedCanvasObj != null) Destroy(spawnedCanvasObj);
    }

    
    IEnumerator AutoScanRoutine()
    {
        CreateStatusText("Hledám stùl...");
        int scanAttempts = 0;

        while (currentState == GameState.Scanning)
        {
            yield return new WaitForSeconds(1f);
            scanAttempts++;

            if (scanAttempts >= 8)
            {
                UpdateStatusText("<color=red>Stùl nenalezen.</color>\nPodívejte se pøed sebe a restartujte hru.");
                int totalIt = SessionManager.Instance != null ? SessionManager.Instance.TotalIterations : 0;
                SessionManager.Instance?.SendSystemStatus("No_Table_Found", "CorsiBlocks", 2, totalIt);
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
        float step = scanAreaSize / gridResolution;
        float startX = roughCenter.x - (scanAreaSize / 2f);
        float startZ = roughCenter.z - (scanAreaSize / 2f);

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
            Vector3 exactTableCenter = CalculateCentroid(validTablePoints);
            exactTableCenter.y = dominantTableHeight + spawnHeightOffset;

            Quaternion tableRotation = Quaternion.Euler(0, head.eulerAngles.y, 0);

            // ZACHYCENÍ CHYBY SPAWNOVÁNÍ
            if (SpawnCorsiBoard(exactTableCenter, tableRotation))
            {
                currentState = GameState.ShowingSequence;
                UpdateStatusText("Stùl nalezen. Poslouchejte!");

                // --- GLOBÁLNÍ LOG ÚSPÌCHU (Obtížnost 2) ---
                SessionManager.Instance?.SendSystemStatus("active", "CorsiBlocks", 2, maxIterations);

                StartCoroutine(PlayIterationRoutine());
            }
            else
            {
                UpdateStatusText("<color=red>Nedostatek místa na stole.</color>\nUvolnìte stùl a restartujte hru.");
                

                // --- GLOBÁLNÍ LOG CHYBY (Obtížnost 2) ---
                SessionManager.Instance?.SendSystemStatus("error_no_space", "CorsiBlocks", 2, maxIterations);

                currentState = GameState.Evaluating;
            }
        }
    }

    Vector3 CalculateCentroid(List<Vector3> points)
    {
        Vector3 centroid = Vector3.zero;
        foreach (Vector3 p in points) centroid += p;
        return centroid / points.Count;
    }

    
    bool SpawnCorsiBoard(Vector3 center, Quaternion rotation)
    {
        foreach (var b in spawnedBlocks) { if (b != null) Destroy(b.gameObject); }
        spawnedBlocks.Clear();

        int attempts = 0;
        int maxAttempts = 500; // Zvýšeno na 500 pokusù

        while (spawnedBlocks.Count < totalBlocks && attempts < maxAttempts)
        {
            attempts++;

            float randX = Random.Range(-boardWidth / 2f, boardWidth / 2f);
            float randZ = Random.Range(-boardDepth / 2f, boardDepth / 2f);
            Vector3 localOffset = new Vector3(randX, 0, randZ);
            Vector3 targetPos = center + (rotation * localOffset);

            bool tooClose = false;
            foreach (var b in spawnedBlocks)
            {
                if (Vector3.Distance(targetPos, b.transform.position) < minBlockSpacing)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            
            if (SpaceOptimizer.Instance != null)
            {
                if (!SpaceOptimizer.Instance.IsPositionSafe(center, targetPos + Vector3.up * 0.15f, 0.03f))
                {
                    continue;
                }
            }

            Vector3 rayOrigin = new Vector3(targetPos.x, center.y + 0.2f, targetPos.z);
            int layerMask = ~(1 << 2);
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 0.4f, layerMask))
            {
                // Zvýšená tolerance na výšku (15 cm)
                if (Mathf.Abs(hit.point.y - (center.y - spawnHeightOffset)) < 0.15f)
                {
                    
                    Vector3 finalPos = new Vector3(targetPos.x, center.y, targetPos.z);

                    // OPRAVA SIROTKÙ: pøidán transform
                    GameObject newObj = Instantiate(blockPrefab, finalPos, rotation, transform);

                    // TADY BYLA OPRAVENA CHYBA: Získáváme MusicCorsiBlock
                    MusicCorsiBlock blockScript = newObj.GetComponent<MusicCorsiBlock>();
                    if (blockScript != null)
                    {
                        blockScript.SetupBlock(this, spawnedBlocks.Count);

                        // HUDEBNÍ LOGIKA: Nastavení správného tónu
                        float assignedPitch = pentatonicPitches[blockScript.ID % pentatonicPitches.Length];
                        blockScript.SetupTone(assignedPitch);

                        spawnedBlocks.Add(blockScript);
                    }
                }
            }
        }

        if (spawnedBlocks.Count < totalBlocks)
        {
            foreach (var b in spawnedBlocks) { if (b != null) Destroy(b.gameObject); }
            spawnedBlocks.Clear();
            return false;
        }

        return true;
    }

    IEnumerator PlayIterationRoutine()
    {
        currentState = GameState.ShowingSequence;
        playerInputIndex = 0;
        targetSequence.Clear();

        int sequenceLength = 2 + currentIteration;

        yield return new WaitForSeconds(2f);
        UpdateStatusText($"Sledujte a poslouchejte ({currentIteration}/{maxIterations})");

        int lastID = -1;
        for (int i = 0; i < sequenceLength; i++)
        {
            int randomID;
            do { randomID = Random.Range(0, spawnedBlocks.Count); }
            while (randomID == lastID);

            targetSequence.Add(randomID);
            lastID = randomID;
        }

        foreach (int id in targetSequence)
        {
            spawnedBlocks[id].Highlight(Color.blue, 0.6f);
            yield return new WaitForSeconds(1.0f);
        }

        currentState = GameState.WaitingForPlayer;
        UpdateStatusText("Zopakujte poøadí");
        LogEvent("Player_Turn_Start", $"Sequence_Length_{sequenceLength}");
    }

    // --- TELEMETRIE DOTYKÙ ---
    public void PlayerSelectedBlock(int blockID)
    {
        if (currentState != GameState.WaitingForPlayer) return;

        if (blockID == targetSequence[playerInputIndex])
        {
            LogEvent("Corsi_Touch_Success", $"MusicBlock_{blockID}");

            spawnedBlocks[blockID].Highlight(Color.green, 0.3f);
            playerInputIndex++;

            if (playerInputIndex >= targetSequence.Count)
            {
                currentState = GameState.Evaluating;
                StartCoroutine(HandleRoundEnd(true));
            }
        }
        else
        {
            LogEvent("Corsi_Touch_Miss", $"MusicBlock_{blockID}");

            currentState = GameState.Evaluating;
            spawnedBlocks[blockID].Highlight(Color.red, 0.5f);
            StartCoroutine(HandleRoundEnd(false));
        }
    }

    public void LogEvent(string eventType, string blockName)
    {
        if (SessionManager.Instance == null) return;

        CorsiEventData eventPayload = new CorsiEventData
        {
            event_type = eventType,
            block_name = blockName,
            iteration = SessionManager.Instance.CurrentIteration,
            time_since_start = Time.time - levelStartTime
        };

        SessionManager.Instance.SendLog(eventPayload);
    }

    IEnumerator HandleRoundEnd(bool isCorrect)
    {
        if (isCorrect)
        {
            UpdateStatusText("<color=green>Správnì!</color>");
            yield return new WaitForSeconds(2f);

            if (SessionManager.Instance != null) SessionManager.Instance.NextIteration();

            if (currentIteration < maxIterations)
            {
                currentIteration++;
                StartCoroutine(PlayIterationRoutine());
            }
            else
            {
                UpdateStatusText("<color=green>Hotovo!</color>");
                yield return new WaitForSeconds(3f);
                StopGame();
            }
        }
        else
        {
            UpdateStatusText("<color=red>Chyba.</color> Zkuste to znovu.");
            yield return new WaitForSeconds(2f);
            StartCoroutine(PlayIterationRoutine());
        }
    }

    // UI a Èištìní plátna
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
            targetPos.y += 0.1f;

            spawnedCanvasObj.transform.position = Vector3.Lerp(spawnedCanvasObj.transform.position, targetPos, Time.deltaTime * 5f);
            spawnedCanvasObj.transform.LookAt(head);
            spawnedCanvasObj.transform.Rotate(0, 180, 0);
        }
    }

    void OnDestroy()
    {
        if (spawnedCanvasObj != null) Destroy(spawnedCanvasObj);
    }
}