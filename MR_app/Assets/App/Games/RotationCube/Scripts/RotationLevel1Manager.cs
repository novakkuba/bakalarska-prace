using UnityEngine;
using TMPro;
using System.Collections;

[System.Serializable]
public class RotationEventData
{
    public string event_type;      // Player_Turn_Start, Cube_Grab, Cube_Release, Button_Correct_Press, Button_Wrong_Press
    public string details;         // Informace (jaká barva se hledá, nebo míra shody natočení)
    public int iteration;          // Číslo aktuálního kola
    public float time_since_start; // Čas od startu úrovně
}

// ODSTRANĚNO IMiniGame - teď je to krásně čisté
public class RotationLevel1Manager : MonoBehaviour, IRotationLevelManager
{
    [Header("Lokální Testování")]
    public bool autoStartLocalTest = true;
    public int testTotalIterations = 3;

    [Header("Nastavení Hry")]
    public GameObject cubePrefab;
    public GameObject canvasPrefab;

    [Tooltip("Tolerance natočení (0.85 je doporučeno)")]
    [Range(0.5f, 1.0f)]
    public float angleTolerance = 0.85f;

    private GameObject spawnedCanvas;
    private TMP_Text taskText;
    private bool isPaused = false;

    private RotationCube activeCube;
    private Material currentTargetMaterial;
    private bool isGameRunning = false;

    private float levelStartTime; 

    // Tuto metodu volá tvůj hlavní kontroler
    public void StartLevel(int totalIterations)
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.TotalIterations = totalIterations;
            SessionManager.Instance.CurrentIteration = 1;
        }
        StartGame(); // Volání naší vnitřní metody bez JSONu
    }

    private void StartGame()
    {
        StopGame();
        levelStartTime = Time.time;

        Transform head = Camera.main.transform;
        Vector3 headForwardLevel = new Vector3(head.forward.x, 0, head.forward.z).normalized;
        Vector3 finalPos = head.position + (headForwardLevel * 0.45f) + (Vector3.down * 0.40f);

        int layerMask = ~(1 << 2);
        if (SpaceOptimizer.Instance == null || !Physics.CheckSphere(finalPos, 0.1f, layerMask))
        {
            GameObject cubeObj = Instantiate(cubePrefab, finalPos, Quaternion.identity, transform);
            activeCube = cubeObj.GetComponent<RotationCube>();
            if (activeCube != null) activeCube.SetupCube(this);

            if (canvasPrefab != null)
            {
                spawnedCanvas = Instantiate(canvasPrefab);
                taskText = spawnedCanvas.GetComponentInChildren<TMP_Text>();
                if (taskText != null) taskText.color = Color.white;

                RotationButtonTrigger[] buttons = spawnedCanvas.GetComponentsInChildren<RotationButtonTrigger>();
                foreach (var btn in buttons) btn.SetupButton(this);
            }

            isGameRunning = true;
            isPaused = false;
            NextRound();

            // --- GLOBÁLNÍ LOG ÚSPĚCHU ---
            int totalIt = SessionManager.Instance != null ? SessionManager.Instance.TotalIterations : testTotalIterations;
            SessionManager.Instance?.SendSystemStatus("active", "RotationCube", 1, totalIt);
        }
        else
        {
            // --- GLOBÁLNÍ LOG CHYBY ---
            int totalIt = SessionManager.Instance != null ? SessionManager.Instance.TotalIterations : testTotalIterations;
            SessionManager.Instance?.SendSystemStatus("error_no_space", "RotationCube", 1, totalIt);

            
            Debug.LogError("Nelze spawnout kostku, před hráčem je překážka!");
        }
    }

    public void StopGame()
    {
        isGameRunning = false;

        // OPRAVA: Okamžité vypnutí (SetActive) před smazáním
        if (activeCube != null)
        {
            activeCube.gameObject.SetActive(false);
            Destroy(activeCube.gameObject);
        }
        if (spawnedCanvas != null)
        {
            spawnedCanvas.gameObject.SetActive(false);
            Destroy(spawnedCanvas);
        }
    }

    void NextRound()
    {
        isPaused = false;

        // Reset všech tlačítek na plátně
        if (spawnedCanvas != null)
        {
            RotationButtonTrigger[] buttons = spawnedCanvas.GetComponentsInChildren<RotationButtonTrigger>();
            foreach (var btn in buttons) btn.ResetColor();
        }

        activeCube.InitializeRandomColors();

        float[] angles = { 0f, 90f, 180f, 270f };
        float randomX = angles[Random.Range(0, angles.Length)];
        float randomY = angles[Random.Range(0, angles.Length)];
        float randomZ = angles[Random.Range(0, angles.Length)];

        activeCube.transform.rotation = Quaternion.Euler(randomX, randomY, randomZ);

        for (int i = 0; i < 15; i++)
        {
            currentTargetMaterial = activeCube.GetRandomTargetMaterial();
            Vector3 targetDirection = activeCube.GetWorldDirectionOfMaterial(currentTargetMaterial);
            if (Vector3.Dot(targetDirection, Vector3.up) < angleTolerance) break;
        }

        UpdateTaskText();

        string czechColor = GetCzechColorName(currentTargetMaterial.name);
        LogEvent("Player_Turn_Start", czechColor);
    }

    void Update()
    {
        if (!isGameRunning || activeCube == null) return;

        if (spawnedCanvas != null)
        {
            Transform head = Camera.main.transform;
            Vector3 targetCanvasPos = activeCube.transform.position + new Vector3(0, 0.45f, 0f);
            spawnedCanvas.transform.position = Vector3.Lerp(spawnedCanvas.transform.position, targetCanvasPos, Time.deltaTime * 5f);
            spawnedCanvas.transform.LookAt(head.position);
            spawnedCanvas.transform.Rotate(0, 180, 0);
        }
    }

    public void CheckAnswerButtonClicked(RotationButtonTrigger pressedButton = null)
    {
        if (isPaused) return;

        Vector3 targetDirection = activeCube.GetWorldDirectionOfMaterial(currentTargetMaterial);
        float match = Vector3.Dot(targetDirection, Vector3.up);

        if (match > angleTolerance)
        {
            LogEvent("Button_Correct_Press", match.ToString("F2"));
            if (pressedButton != null) pressedButton.ShowSuccess();
            StartCoroutine(HandleFeedback(true));
        }
        else
        {
            LogEvent("Button_Wrong_Press", match.ToString("F2"));
            if (pressedButton != null) pressedButton.ShowErrorAndBlink();
            StartCoroutine(HandleFeedback(false));
        }
    }

    // TELEMETRICKÝ ODESÍLATEL
    public void LogEvent(string eventType, string details)
    {
        if (SessionManager.Instance == null) return;

        RotationEventData eventPayload = new RotationEventData
        {
            event_type = eventType,
            details = details,
            iteration = SessionManager.Instance.CurrentIteration,
            time_since_start = Time.time - levelStartTime
        };

        SessionManager.Instance.SendLog(eventPayload);
    }

    private IEnumerator HandleFeedback(bool isCorrect)
    {
        isPaused = true;

        if (isCorrect)
        {
            if (taskText != null) taskText.text = "<color=green>Výborně!</color>\nSprávně natočeno.\n<size=70%>Čekejte na další kolo...</size>";
            activeCube.HighlightCorrect(true);

            yield return new WaitForSeconds(5f);

            if (SessionManager.Instance != null && SessionManager.Instance.CurrentIteration < SessionManager.Instance.TotalIterations)
            {
                SessionManager.Instance.NextIteration();
                NextRound();
            }
            else FinishLevel();
        }
        else
        {
            if (taskText != null) taskText.text = "<color=red>Špatně!</color>\nZkuste to prosím znovu.";
            yield return new WaitForSeconds(5f);

            isPaused = false;
            UpdateTaskText();
        }
    }

    void FinishLevel()
    {
        if (taskText != null) taskText.text = "<color=green>Skvělá práce!</color>\nÚroveň dokončena.";
        Invoke("StopGame", 3f);
    }

    private void UpdateTaskText()
    {
        int currentIt = SessionManager.Instance != null ? SessionManager.Instance.CurrentIteration : 1;
        int totalIt = SessionManager.Instance != null ? SessionManager.Instance.TotalIterations : testTotalIterations;

        string czechColor = GetCzechColorName(currentTargetMaterial.name);
        if (taskText != null)
            taskText.text = $"Kolo {currentIt}/{totalIt}\nOtočte barvu nahoru:\n<b>{czechColor}</b>";
    }

    private string GetCzechColorName(string rawName)
    {
        string clean = rawName.ToLower();
        if (clean.Contains("red")) return "Zelená";
        if (clean.Contains("blue")) return "Černá";
        if (clean.Contains("yellow")) return "Modrá";
        if (clean.Contains("green")) return "Bílá";
        if (clean.Contains("black")) return "Červená";
        if (clean.Contains("white")) return "Žlutá";
        return clean.ToUpper();
    }

    // ÚKLID SIROTKŮ
    void OnDestroy()
    {
        if (spawnedCanvas != null) Destroy(spawnedCanvas);
    }
}