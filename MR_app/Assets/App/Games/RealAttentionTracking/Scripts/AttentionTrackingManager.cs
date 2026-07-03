using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


[System.Serializable]
public class AttentionEventData
{
    public string event_type;      
    public string details;         
    public int iteration;          
    public float time_since_start; 
}


public class AttentionTrackingManager : MonoBehaviour, IAttentionLevelManager
{
    public enum TestState { Scanning, Memorizing, WaitingForTurn, Guessing, Evaluating }
    public TestState currentState = TestState.Scanning;

    [Header("UI Nastavení (HUD)")]
    public GameObject statusTextPrefab;
    private TMP_Text spawnedStatusText;
    private GameObject spawnedCanvasObj;

    [Header("Tlačítka (Náhrada dotyku)")]
    public GameObject confirmationButtonPrefab;
    public float buttonOffsetZ = 0.15f;
    public float buttonOffsetY = 0.05f;

    [Header("Nastavení skeneru (Mřížka)")]
    public float scanAreaSize = 1.0f;
    public int gridResolution = 10;
    public float minTableHeight = 0.4f;
    public float maxTableHeight = 1.2f;
    public float spawnHeightOffset = 0.05f;

    [Header("Herní Logika")]
    public GameObject[] testPrefabs;
    public float memorizeTime = 10.0f;
    public float minDistance = 0.3f;

    // Dynamické proměnné
    private int currentLevel;
    private int maxIterations;
    private int currentIteration;
    private int itemsToSpawn;
    private int itemsToSwap;
    private bool isGameRunning = false;

    private List<GameObject> spawnedItems = new List<GameObject>();
    private List<GameObject> spawnedButtons = new List<GameObject>();
    private List<int> correctTargetIndices = new List<int>();
    private List<Vector3> itemSpawnPositions = new List<Vector3>();

    private int successfulGuessesThisRound = 0;
    private Vector3 originalLookDirection;

    // Uložené centrum stolu pro další iterace
    private Vector3 savedTableCenter;
    private Vector3 savedLookDirection;

    // TELEMETRIE
    private float roundStartTime;
    private float levelGlobalStartTime;

    private bool hasSeenTableThisRound = false;
    private Coroutine messageCoroutine;

    // SPUŠTĚNÍ HRY Z CONTROLLERU
    public void StartLevel(int level, int totalIterations)
    {
        currentLevel = level;

        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.TotalIterations = totalIterations;
            SessionManager.Instance.CurrentIteration = 1;
        }

        StartGame(); // ČISTÉ VOLÁNÍ bez prázdného JSONu
    }

    // PRIVÁTNÍ METODA bez zbytečných parametrů
    private void StartGame()
    {
        StopGame();
        maxIterations = SessionManager.Instance != null ? SessionManager.Instance.TotalIterations : 3;
        currentIteration = 1;
        isGameRunning = true;
        levelGlobalStartTime = Time.time;

        if (currentLevel == 1) { itemsToSpawn = 3; itemsToSwap = 1; }
        else if (currentLevel == 2) { itemsToSpawn = 4; itemsToSwap = 1; }
        else if (currentLevel == 3) { itemsToSpawn = 4; itemsToSwap = 2; }
        else { itemsToSpawn = 3; itemsToSwap = 1; }

        CreateStatusText($"Hledám stůl pro Level {currentLevel}...");
        StartCoroutine(AutoScanRoutine());
    }

    public void StopGame()
    {
        isGameRunning = false;
        StopAllCoroutines();
        CleanupItems();
        if (spawnedCanvasObj != null) Destroy(spawnedCanvasObj);
    }

    private void CleanupItems()
    {
        foreach (var item in spawnedItems) { if (item != null) Destroy(item); }
        foreach (var btn in spawnedButtons) { if (btn != null) Destroy(btn); }
        spawnedItems.Clear();
        spawnedButtons.Clear();
        correctTargetIndices.Clear();
        itemSpawnPositions.Clear();
        successfulGuessesThisRound = 0;
        hasSeenTableThisRound = false;
    }

    IEnumerator AutoScanRoutine()
    {
        int scanAttempts = 0;
        while (currentState == TestState.Scanning && isGameRunning)
        {
            yield return new WaitForSeconds(1f);
            scanAttempts++;

            if (scanAttempts >= 8)
            {
                UpdateStatusText("<color=red>Stůl nenalezen.</color>\nPodívejte se před sebe a restartujte hru.");
                

                // --- GLOBÁLNÍ LOG CHYBY ---
                SessionManager.Instance?.SendSystemStatus("No_table_Found", "AttentionTracking", currentLevel, maxIterations);

                currentState = TestState.Evaluating;
                isGameRunning = false; // ZASTAVÍ HRU!
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

        if (validTablePoints.Count > 5)
        {
            dominantTableHeight /= validTablePoints.Count;
            savedTableCenter = CalculateCentroid(validTablePoints);
            savedTableCenter.y = dominantTableHeight;
            savedLookDirection = headForwardLevel;

            currentState = TestState.Memorizing;
            UpdateStatusText("Stůl nalezen!");

            // --- GLOBÁLNÍ LOG ÚSPĚCHU ---
            SessionManager.Instance?.SendSystemStatus("active", "AttentionTracking", currentLevel, maxIterations);

            StartCoroutine(GameSequenceRoutine());
        }
    }

    Vector3 CalculateCentroid(List<Vector3> points)
    {
        Vector3 centroid = Vector3.zero;
        foreach (Vector3 p in points) centroid += p;
        return centroid / points.Count;
    }

    IEnumerator GameSequenceRoutine()
    {
        yield return new WaitForSeconds(2.0f);

        while (currentIteration <= maxIterations && isGameRunning)
        {
            if (currentIteration > 1)
            {
                CleanupItems();
                UpdateStatusText($"Připravte se na {currentIteration}. kolo ze {maxIterations}...");
                yield return new WaitForSeconds(3.0f);
            }

            yield return StartCoroutine(PlaySingleRound());

            // Nahlášení splněného kola
            if (SessionManager.Instance != null && currentIteration < maxIterations)
            {
                SessionManager.Instance.NextIteration();
            }

            currentIteration++;
        }

        if (isGameRunning)
        {
            CleanupItems();
            UpdateStatusText("<color=green>Test pozornosti dokončen!</color>");
            yield return new WaitForSeconds(4.0f);
            StopGame();
        }
    }

    IEnumerator PlaySingleRound()
    {
        CleanupItems();
        UpdateStatusText($"Kolo {currentIteration}/{maxIterations}\nZapamatujte si tyto předměty.");

        // OŠETŘENÍ CHYB (Space Optimizer)
        if (!SpawnItemsAroundCenter(savedTableCenter, savedLookDirection))
        {
            UpdateStatusText("<color=red>Nedostatek místa na stole.</color>\nUvolněte prostor.");
           

            // --- GLOBÁLNÍ LOG CHYBY ---
            SessionManager.Instance?.SendSystemStatus("error_no_space", "AttentionTracking", currentLevel, maxIterations);

            currentState = TestState.Evaluating;
            isGameRunning = false; // TÍMTO ZABRÁNÍME FALEŠNÉ VÝHŘE!
            yield break;
        }

        originalLookDirection = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z).normalized;
        currentState = TestState.Memorizing;

        yield return new WaitForSeconds(memorizeTime);

        UpdateStatusText("Prosím, otočte se zády ke stolu!");
        currentState = TestState.WaitingForTurn;

        while (currentState == TestState.WaitingForTurn || currentState == TestState.Guessing)
        {
            yield return null;
        }

        yield return new WaitForSeconds(3.0f);
    }

    // Návratová hodnota bool pro fail-safe kontrolu
    bool SpawnItemsAroundCenter(Vector3 center, Vector3 lookDirection)
    {
        List<GameObject> availablePrefabs = new List<GameObject>(testPrefabs);
        Transform head = Camera.main.transform;
        Quaternion headRotation = Quaternion.Euler(0, head.eulerAngles.y, 0);

        float totalWidth = (itemsToSpawn - 1) * minDistance;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < itemsToSpawn; i++)
        {
            Vector3 localOffset = new Vector3(startX + (i * minDistance), 0, 0);
            Vector3 targetPos = center + (headRotation * localOffset);

            // OPRAVENO: Kontrolujeme prostor 15 cm NAD stolem a zmenšili jsme poloměr na 5 cm (0.05f)
            if (SpaceOptimizer.Instance != null && !SpaceOptimizer.Instance.IsPositionSafe(center, targetPos + Vector3.up * 0.15f, 0.05f))
            {
                // Pokud je ve vzduchu nad stolem reálná překážka, teprve tehdy nepokračujeme
                return false;
            }

            Vector3 finalPos = new Vector3(targetPos.x, center.y + spawnHeightOffset, targetPos.z);
            Vector3 rayOrigin = new Vector3(targetPos.x, center.y + 0.3f, targetPos.z);

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 0.6f))
            {
                if (Vector3.Dot(hit.normal, Vector3.up) > 0.9f && hit.point.y >= minTableHeight && hit.point.y <= maxTableHeight)
                {
                    finalPos.y = hit.point.y + spawnHeightOffset;
                }
            }

            int randomIndex = Random.Range(0, availablePrefabs.Count);
            GameObject prefabToSpawn = availablePrefabs[randomIndex];
            availablePrefabs.RemoveAt(randomIndex);

            // OPRAVA SIROTKŮ: Transform jako potomek manažera
            GameObject newItem = Instantiate(prefabToSpawn, finalPos, prefabToSpawn.transform.rotation, transform);
            spawnedItems.Add(newItem);
            itemSpawnPositions.Add(finalPos);
        }
        return true;
    }

    void Update()
    {
        if (spawnedCanvasObj != null && isGameRunning)
        {
            Transform head = Camera.main.transform;
            Vector3 targetPos = head.position + (head.forward * 1.0f);
            targetPos.y -= 0.1f;

            spawnedCanvasObj.transform.position = Vector3.Lerp(spawnedCanvasObj.transform.position, targetPos, Time.deltaTime * 5f);
            spawnedCanvasObj.transform.LookAt(head);
            spawnedCanvasObj.transform.Rotate(0, 180, 0);
        }

        if (currentState == TestState.WaitingForTurn || currentState == TestState.Guessing)
        {
            Vector3 currentLookDir = Camera.main.transform.forward;
            currentLookDir.y = 0;
            currentLookDir.Normalize();

            float angle = Vector3.Angle(originalLookDirection, currentLookDir);

            if (currentState == TestState.WaitingForTurn && angle > 120f)
            {
                PerformSecretSwap();

                string text = (itemsToSwap > 1) ? "Otočte se a potvrďte všechny nové věci!" : "Otočte se a potvrďte novou věc!";
                UpdateStatusText(text);

                currentState = TestState.Guessing;
                hasSeenTableThisRound = false;
            }
            else if (currentState == TestState.Guessing && angle < 45f && !hasSeenTableThisRound)
            {
                hasSeenTableThisRound = true;

                // TELEMETRIE: Pacient konečně uviděl stůl! Teď se začíná měřit čistý reakční čas.
                roundStartTime = Time.time;
                LogEvent("Player_Turn_Start", "Saw_Swapped_Table");

                if (successfulGuessesThisRound == 0)
                {
                    UpdateStatusText(itemsToSwap > 1 ? "Potvrďte změněné předměty." : "Potvrďte změněný předmět.");
                }
            }
        }
    }

    void PerformSecretSwap()
    {
        correctTargetIndices.Clear();
        List<int> indicesToSwap = new List<int>();

        while (indicesToSwap.Count < itemsToSwap)
        {
            int r = Random.Range(0, spawnedItems.Count);
            if (!indicesToSwap.Contains(r)) indicesToSwap.Add(r);
        }

        List<string> forbiddenNames = new List<string>();
        foreach (var item in spawnedItems)
        {
            forbiddenNames.Add(item.name.Replace("(Clone)", "").Trim());
        }

        foreach (int targetIndex in indicesToSwap)
        {
            GameObject oldItem = spawnedItems[targetIndex];
            Vector3 perfectSlotPosition = itemSpawnPositions[targetIndex];

            oldItem.SetActive(false);
            Destroy(oldItem);

            GameObject newPrefab = testPrefabs[0];
            for (int i = 0; i < 50; i++)
            {
                newPrefab = testPrefabs[Random.Range(0, testPrefabs.Length)];
                string cleanName = newPrefab.name.Replace("(Clone)", "").Trim();

                if (!forbiddenNames.Contains(cleanName))
                {
                    forbiddenNames.Add(cleanName);
                    break;
                }
            }

            GameObject swappedItem = Instantiate(newPrefab, perfectSlotPosition, newPrefab.transform.rotation, transform);
            spawnedItems[targetIndex] = swappedItem;
            correctTargetIndices.Add(targetIndex);
        }

        // Tvorba tlačítek pro odpověď
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            Vector3 basePos = itemSpawnPositions[i];
            Vector3 buttonPos = basePos - (savedLookDirection * buttonOffsetZ);
            buttonPos.y += buttonOffsetY;

            Quaternion baseRot = Quaternion.LookRotation(-savedLookDirection);
            Quaternion buttonRot = baseRot * Quaternion.Euler(0, 180, 0);

            GameObject newButton = Instantiate(confirmationButtonPrefab, buttonPos, buttonRot, transform);
            spawnedButtons.Add(newButton);

            ItemButton btnScript = newButton.GetComponent<ItemButton>();
            if (btnScript != null)
            {
                // ČISTÉ ROZHRANÍ
                btnScript.SetupButton(i, this);
            }
        }
    }

    // IMPLEMENTOVANÁ METODA ROZHRANÍ
    public void OnSeniorClickedItem(int itemIndex)
    {
        if (currentState != TestState.Guessing) return;

        if (messageCoroutine != null) StopCoroutine(messageCoroutine);

        if (correctTargetIndices.Contains(itemIndex))
        {
            successfulGuessesThisRound++;
            correctTargetIndices.Remove(itemIndex);

            if (spawnedButtons[itemIndex] != null) spawnedButtons[itemIndex].SetActive(false);

            if (successfulGuessesThisRound >= itemsToSwap)
            {
                currentState = TestState.Evaluating;
                UpdateStatusText("<color=green>Výborně! Nalezeno.</color>");
                LogEvent("Attention_Guess_Correct", $"Item_{itemIndex}");
            }
            else
            {
                UpdateStatusText($"<color=yellow>Správně!</color> Ještě najděte {itemsToSwap - successfulGuessesThisRound} další...");
                LogEvent("Attention_Guess_Correct_Partial", $"Item_{itemIndex}");
            }
        }
        else
        {
            messageCoroutine = StartCoroutine(ShowTemporaryErrorMessage("<color=red>Chyba!</color> Zkuste to znovu.", 2.5f));
            LogEvent("Attention_Guess_Incorrect", $"Item_{itemIndex}");

            if (spawnedButtons[itemIndex] != null) spawnedButtons[itemIndex].SetActive(false);
        }
    }

    IEnumerator ShowTemporaryErrorMessage(string message, float duration)
    {
        UpdateStatusText(message);
        yield return new WaitForSeconds(duration);

        if (currentState == TestState.Guessing)
        {
            if (successfulGuessesThisRound == 0)
                UpdateStatusText(itemsToSwap > 1 ? "Které předměty se změnily?" : "Který předmět se změnil?");
            else
                UpdateStatusText($"<color=yellow>Správně!</color> Ještě najděte {itemsToSwap - successfulGuessesThisRound} další...");
        }
    }

    // ODESÍLÁNÍ NA LÉKAŘSKÝ TABLET
    private void LogEvent(string eventType, string details)
    {
        if (SessionManager.Instance == null) return;

        AttentionEventData eventPayload = new AttentionEventData
        {
            event_type = eventType,
            details = details,
            iteration = SessionManager.Instance.CurrentIteration,
            time_since_start = Time.time - levelGlobalStartTime
        };

        SessionManager.Instance.SendLog(eventPayload);
    }

    void CreateStatusText(string msg)
    {
        if (statusTextPrefab != null && spawnedCanvasObj == null)
        {
            spawnedCanvasObj = Instantiate(statusTextPrefab);
            spawnedStatusText = spawnedCanvasObj.GetComponentInChildren<TMP_Text>();
        }
        UpdateStatusText(msg);
    }

    void UpdateStatusText(string msg)
    {
        if (spawnedStatusText != null) spawnedStatusText.text = msg;
    }

    // ÚKLID CANVASU A VŠECH OBJEKTŮ
    void OnDestroy()
    {
        StopGame();
    }
}