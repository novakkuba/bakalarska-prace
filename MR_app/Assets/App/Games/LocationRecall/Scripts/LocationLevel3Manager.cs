using UnityEngine;
using TMPro;
using System.Collections;

// IMiniGame odstranìno
public class LocationLevel3Manager : MonoBehaviour, ILocationLevelManager
{
    [Header("Fáze 1: Cíle (Dva objekty)")]
    public float showTime = 4.0f;
    public float hiddenTime = 2.0f;

    public Material ghostMaterialA;
    public Material ghostMaterialB;

    public GameObject targetPrefabA;
    public GameObject targetPrefabB;

    [Header("Fáze 2: Hádání")]
    public GameObject guessPrefabA;
    public GameObject guessPrefabB;

    [Tooltip("Levitující Canvas s nápisem a tlaèítkem Potvrdit (obsahuje ButtonTrigger)")]
    public GameObject controlPanelPrefab;

    private int maxIterations;
    private int currentIteration;
    private bool isGameRunning = false;
    private bool waitingForPlayer = false;

    private Vector3 secretPositionA;
    private Vector3 secretPositionB;
    private GameObject currentTargetA;
    private GameObject currentTargetB;
    private GameObject currentGuessA;
    private GameObject currentGuessB;
    private GameObject currentControlPanel;

    // TELEMETRIE
    private float startTime;
    private float levelGlobalStartTime;

    public void StartLevel(int totalIterations)
    {
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

        StartCoroutine(GameSequenceRoutine());
    }

    public void StopGame()
    {
        isGameRunning = false;
        waitingForPlayer = false;
        StopAllCoroutines();
        CleanupRoundObjects();
    }

    private void CleanupRoundObjects()
    {
        if (currentTargetA != null) Destroy(currentTargetA);
        if (currentTargetB != null) Destroy(currentTargetB);
        if (currentGuessA != null) Destroy(currentGuessA);
        if (currentGuessB != null) Destroy(currentGuessB);
        if (currentControlPanel != null) Destroy(currentControlPanel);
    }

    IEnumerator GameSequenceRoutine()
    {
        yield return new WaitForSeconds(2.0f);

        while (currentIteration <= maxIterations && isGameRunning)
        {
            Debug.Log($"--- LOCATION RECALL LEVEL 3: Iterace {currentIteration}/{maxIterations} ---");
            yield return StartCoroutine(PlaySingleRound());

            if (SessionManager.Instance != null && currentIteration < maxIterations)
            {
                SessionManager.Instance.NextIteration();
            }
            currentIteration++;
        }

        if (isGameRunning)
        {
            StopGame();
        }
    }

    IEnumerator PlaySingleRound()
    {
        Transform head = Camera.main.transform;

        // SPACE OPTIMIZER: Bublina ve vzduchu o polomìru 1.2m pro dva objekty
        Vector3? spawnPos = null;
        if (SpaceOptimizer.Instance != null)
        {
            spawnPos = SpaceOptimizer.Instance.GetSafePosition(head, 1.5f, 2.5f, 1.0f, 1.4f, 1.2f);
        }

        
        if (!spawnPos.HasValue)
        {
            
            SessionManager.Instance?.SendSystemStatus("error_no_space", "LocationRecall", 3, maxIterations);

            
            Debug.LogError("Nedostatek místa pro spawn Location kostek!");

            // Protože Level 3 nemá FeedbackCanvas, vyrobíme doèasný text z Control Panelu
            if (controlPanelPrefab != null)
            {
                Vector3 errorPos = head.position + (head.forward * 0.45f) + (Vector3.down * 0.15f);

                // OPRAVA CHYBY CS0136: 
                Quaternion errorFacePlayerRot = Quaternion.LookRotation(head.position - errorPos);
                GameObject errorPanel = Instantiate(controlPanelPrefab, errorPos, errorFacePlayerRot, transform);

                TMP_Text panelText = errorPanel.GetComponentInChildren<TMP_Text>();
                if (panelText != null) panelText.text = "<color=red>Nedostatek místa.</color>\nOdstupte od pøekážky.";

                ButtonTrigger btnError = errorPanel.GetComponentInChildren<ButtonTrigger>();
                if (btnError != null) btnError.gameObject.SetActive(false); // Schováme tlaèítko

                yield return new WaitForSeconds(3.0f);
                Destroy(errorPanel);
            }

            isGameRunning = false;
            yield break;
        }

        SessionManager.Instance?.SendSystemStatus("active", "LocationRecall", 3, maxIterations);

        Vector3 centerPos = spawnPos.Value;
        Vector3 lookDirection = head.position - centerPos;
        lookDirection.y = 0;
        Quaternion lookRot = Quaternion.LookRotation(lookDirection);

        Vector3 offsetA = new Vector3(-0.35f, 0, 0);
        Vector3 offsetB = new Vector3(0.35f, 0, 0);

        offsetA.z += Random.Range(-0.1f, 0.1f);
        offsetB.z += Random.Range(-0.1f, 0.1f);

        secretPositionA = centerPos + (lookRot * offsetA);
        secretPositionB = centerPos + (lookRot * offsetB);

        currentTargetA = Instantiate(targetPrefabA, secretPositionA, lookRot, transform);
        currentTargetB = Instantiate(targetPrefabB, secretPositionB, lookRot, transform);

        yield return new WaitForSeconds(showTime);

        currentTargetA.GetComponent<Renderer>().enabled = false;
        currentTargetB.GetComponent<Renderer>().enabled = false;
        yield return new WaitForSeconds(hiddenTime);

        Quaternion headRotation = Quaternion.Euler(0, head.eulerAngles.y, 0);

        Vector3 panelOffset = new Vector3(0, 1.3f - head.position.y, 0.45f);
        Vector3 panelPos = head.position + (headRotation * panelOffset);

        Vector3 guessOffsetA = new Vector3(-0.25f, 1.15f - head.position.y, 0.4f);
        Vector3 guessOffsetB = new Vector3(0.25f, 1.15f - head.position.y, 0.4f);

        Vector3 playerPosA = head.position + (headRotation * guessOffsetA);
        Vector3 playerPosB = head.position + (headRotation * guessOffsetB);

        Vector3 lookAtPlayer = head.position - panelPos;
        lookAtPlayer.y = 0;

        Quaternion facePlayerRot = Quaternion.LookRotation(lookAtPlayer);

        currentControlPanel = Instantiate(controlPanelPrefab, panelPos, facePlayerRot, transform);
        currentGuessA = Instantiate(guessPrefabA, playerPosA, facePlayerRot, transform);
        currentGuessB = Instantiate(guessPrefabB, playerPosB, facePlayerRot, transform);

        ButtonTrigger btn = currentControlPanel.GetComponentInChildren<ButtonTrigger>();
        if (btn != null)
        {
            btn.SetupButton(this);
        }
        else
        {
            Debug.LogError("LocationLevel3: Nenašel jsem ButtonTrigger na controlPanelPrefab!");
        }

        startTime = Time.time;
        waitingForPlayer = true;

        LogEvent("Player_Turn_Start", 0f, 0f);

        while (waitingForPlayer)
        {
            yield return null;
        }

        if (currentControlPanel != null)
        {
            TMP_Text panelText = currentControlPanel.GetComponentInChildren<TMP_Text>();
            if (panelText != null)
            {
                if (currentIteration < maxIterations)
                    panelText.text = "<color=green>Výbornì!</color>\nPøipravte se na další kolo...";
                else
                    panelText.text = "<color=green>Fantastické!</color>\nNejtìžší úroveò splnìna.";
            }

            if (btn != null) btn.gameObject.SetActive(false);
        }

        if (currentTargetA != null && currentTargetB != null)
        {
            Renderer rendA = currentTargetA.GetComponent<Renderer>();
            Renderer rendB = currentTargetB.GetComponent<Renderer>();

            rendA.enabled = true;
            rendB.enabled = true;

            if (ghostMaterialA != null) rendA.material = ghostMaterialA;
            if (ghostMaterialB != null) rendB.material = ghostMaterialB;
        }

        yield return new WaitForSeconds(4.0f);

        CleanupRoundObjects();
    }

    public void OnConfirmPlacement()
    {
        if (!waitingForPlayer) return;
        waitingForPlayer = false;

        float errorDistanceA = Vector3.Distance(currentGuessA.transform.position, secretPositionA);
        float errorCmA = errorDistanceA * 100f;

        float errorDistanceB = Vector3.Distance(currentGuessB.transform.position, secretPositionB);
        float errorCmB = errorDistanceB * 100f;

        float timeTaken = Time.time - startTime;
        float averageError = (errorCmA + errorCmB) / 2f;

        // ODESLÁNÍ TØÍ ÈISTÝCH LOGÙ PRO DETAILNÍ ANALÝZU
        LogEvent("Placement_Confirmed_ObjA", errorCmA, timeTaken);
        LogEvent("Placement_Confirmed_ObjB", errorCmB, timeTaken);
        LogEvent("Placement_Confirmed_Average", averageError, timeTaken);
    }

    private void LogEvent(string eventType, float errorCm, float timeTaken)
    {
        if (SessionManager.Instance == null) return;

        LocationEventData eventPayload = new LocationEventData
        {
            event_type = eventType,
            error_distance_cm = Mathf.Round(errorCm * 10f) / 10f,
            time_taken_seconds = Mathf.Round(timeTaken * 10f) / 10f,
            iteration = SessionManager.Instance.CurrentIteration,
            time_since_start = Time.time - levelGlobalStartTime
        };

        SessionManager.Instance.SendLog(eventPayload);
    }

    void Update()
    {
        if (currentControlPanel != null && waitingForPlayer)
        {
            Transform head = Camera.main.transform;

            Vector3 targetPos = head.position + (head.forward * 0.45f);
            targetPos.y = head.position.y - 0.15f;

            currentControlPanel.transform.position = Vector3.Lerp(currentControlPanel.transform.position, targetPos, Time.deltaTime * 3f);
            currentControlPanel.transform.LookAt(head);
            currentControlPanel.transform.Rotate(0, 180, 0);
        }
    }

    void OnDestroy()
    {
        CleanupRoundObjects();
    }
}