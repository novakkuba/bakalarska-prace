using UnityEngine;
using TMPro;
using System.Collections;

public class RotationLevel3Manager : MonoBehaviour, IRotationLevelManager
{
    [Header("Nastavení Hry")]
    public GameObject cubePrefab;            // Velká hratelná kostka
    public GameObject referenceCubePrefab;   // Malá nehratelná kostka (vzor)
    public GameObject canvasPrefab;

    [Tooltip("Tolerance natočení (0.85 je doporučeno)")]
    [Range(0.5f, 1.0f)]
    public float angleTolerance = 0.85f;

    private GameObject spawnedCanvas;
    private TMP_Text taskText;
    private bool isPaused = false;

    private RotationCube activeCube;
    private GameObject referenceCube;
    private Renderer refCubeRenderer;

    private bool isGameRunning = false;

    // SJEDNOCENÉ PROMĚNNÉ
    private int maxIterations;
    private int currentIteration;
    private float levelGlobalStartTime;
    private float levelStartTime;

    public void StartLevel(int totalIterations)
    {
        maxIterations = totalIterations;
        currentIteration = 1;

        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.TotalIterations = totalIterations;
            SessionManager.Instance.CurrentIteration = 1;
        }

        StartGame();
    }

    private void StartGame()
    {
        StopGame();
        maxIterations = SessionManager.Instance != null ? SessionManager.Instance.TotalIterations : 3;
        currentIteration = 1;
        isGameRunning = true;
        levelGlobalStartTime = Time.time;
        levelStartTime = Time.time;

        Transform head = Camera.main.transform;
        Vector3 headForwardLevel = new Vector3(head.forward.x, 0, head.forward.z).normalized;
        Quaternion headRotation = Quaternion.Euler(0, head.eulerAngles.y, 0);
        Vector3 finalPos = head.position + (headForwardLevel * 0.45f) + (Vector3.down * 0.40f);

        int layerMask = ~(1 << 2);
        if (SpaceOptimizer.Instance == null || !Physics.CheckSphere(finalPos, 0.2f, layerMask))
        {
            GameObject cubeObj = Instantiate(cubePrefab, finalPos, Quaternion.identity, transform);
            activeCube = cubeObj.GetComponent<RotationCube>();
            if (activeCube != null) activeCube.SetupCube(this);

            Vector3 refOffset = new Vector3(-0.4f, 0.2f, 0f);
            referenceCube = Instantiate(referenceCubePrefab, finalPos + (headRotation * refOffset), Quaternion.identity, transform);
            refCubeRenderer = referenceCube.GetComponentInChildren<Renderer>();

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
            SessionManager.Instance?.SendSystemStatus("active", "RotationCube", 3, maxIterations);
        }
        else
        {
            // --- GLOBÁLNÍ LOG CHYBY ---
            SessionManager.Instance?.SendSystemStatus("error_no_space", "RotationCube", 3, maxIterations);

            
            Debug.LogError("Nelze spawnout kostky, v prostoru před hráčem něco překáží!");
        }
    }


    public void StopGame()
    {
        isGameRunning = false;
        StopAllCoroutines();

        if (activeCube != null)
        {
            activeCube.gameObject.SetActive(false);
            Destroy(activeCube.gameObject);
        }
        if (referenceCube != null)
        {
            referenceCube.gameObject.SetActive(false);
            Destroy(referenceCube);
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

        if (spawnedCanvas != null)
        {
            RotationButtonTrigger[] buttons = spawnedCanvas.GetComponentsInChildren<RotationButtonTrigger>();
            foreach (var btn in buttons) btn.ResetColor();
        }

        activeCube.InitializeRandomColors();

        if (refCubeRenderer != null)
        {
            refCubeRenderer.materials = activeCube.cubeRenderer.materials;
        }
        else
        {
            Debug.LogError("Chyba: Nenašel se Renderer na malé kostce!");
        }

        float[] angles = { 0f, 90f, 180f, 270f };

        activeCube.transform.rotation = Quaternion.Euler(
            angles[Random.Range(0, angles.Length)],
            angles[Random.Range(0, angles.Length)],
            angles[Random.Range(0, angles.Length)]
        );

        int pojistka = 0;
        do
        {
            referenceCube.transform.rotation = Quaternion.Euler(
                angles[Random.Range(0, angles.Length)],
                angles[Random.Range(0, angles.Length)],
                angles[Random.Range(0, angles.Length)]
            );
            pojistka++;
        }
        while (CheckIfMatched() && pojistka < 15);

        UpdateTaskText();
        LogEvent("Player_Turn_Start", "Match_Reference_Cube");
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

        float upMatch = Vector3.Dot(activeCube.transform.up, referenceCube.transform.up);
        float forwardMatch = Vector3.Dot(activeCube.transform.forward, referenceCube.transform.forward);
        bool isMatched = (upMatch > angleTolerance && forwardMatch > angleTolerance);

        string matchDetails = $"Up:{upMatch:F2}_Fwd:{forwardMatch:F2}";

        if (isMatched)
        {
            LogEvent("Button_Correct_Press", matchDetails);
            if (pressedButton != null) pressedButton.ShowSuccess();
            StartCoroutine(HandleFeedback(true));
        }
        else
        {
            LogEvent("Button_Wrong_Press", matchDetails);
            if (pressedButton != null) pressedButton.ShowErrorAndBlink();
            StartCoroutine(HandleFeedback(false));
        }
    }

    private bool CheckIfMatched()
    {
        float upMatch = Vector3.Dot(activeCube.transform.up, referenceCube.transform.up);
        float forwardMatch = Vector3.Dot(activeCube.transform.forward, referenceCube.transform.forward);
        return (upMatch > angleTolerance && forwardMatch > angleTolerance);
    }

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
            if (taskText != null) taskText.text = "<color=green>Výborně!</color>\nSkvělá práce.";
            activeCube.HighlightCorrect(true);
            yield return new WaitForSeconds(4f);

            if (SessionManager.Instance != null && SessionManager.Instance.CurrentIteration < SessionManager.Instance.TotalIterations)
            {
                SessionManager.Instance.NextIteration();
                currentIteration = SessionManager.Instance.CurrentIteration;
                NextRound();
            }
            else FinishLevel();
        }
        else
        {
            if (taskText != null) taskText.text = "<color=red>Špatně!</color>\nZkuste to znovu.";
            yield return new WaitForSeconds(4f);

            isPaused = false;
            UpdateTaskText();
        }
    }

    void FinishLevel()
    {
        if (taskText != null) taskText.text = "<color=green>Hotovo!</color>";
        Invoke("StopGame", 3f);
    }

    private void UpdateTaskText()
    {
        int currentIt = SessionManager.Instance != null ? SessionManager.Instance.CurrentIteration : currentIteration;
        int totalIt = SessionManager.Instance != null ? SessionManager.Instance.TotalIterations : maxIterations;

        if (taskText != null)
        {
            taskText.text = $"Kolo {currentIt}/{totalIt}\nNatočte kostku\n<b>podle vzoru vlevo</b>";
        }
    }

    void OnDestroy()
    {
        if (spawnedCanvas != null) Destroy(spawnedCanvas);
    }
}