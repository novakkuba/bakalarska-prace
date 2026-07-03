using UnityEngine;
using TMPro;
using System.Collections;


public class LocationLevel2Manager : MonoBehaviour, ILocationLevelManager
{
    [Header("Fáze 1: Cíl (Zrychlený èas)")]
    public GameObject targetPrefab;
    public float showTime = 1.5f; // ZKRÁCENÝ ÈAS PRO LEVEL 2
    public Material ghostMaterial;

    [Header("Fáze 2: Hádání")]
    [Tooltip("Prefab obsahující uchopitelnou kostku, Canvas a tlaèítko (ButtonTrigger)")]
    public GameObject guessPrefab;

    [Header("Fáze 3: Zpìtná vazba (Odmìna)")]
    [Tooltip("Prefab s Canvasem a textem, který vyskoèí po potvrzení")]
    public GameObject feedbackCanvasPrefab;

    private int maxIterations;
    private int currentIteration;
    private bool isGameRunning = false;
    private bool waitingForPlayer = false;

    private Vector3 secretPosition;
    private GameObject currentTarget;
    private GameObject currentGuess;

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
        StartGame(); // Volání èisté interní metody
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
        if (currentTarget != null) Destroy(currentTarget);
        if (currentGuess != null) Destroy(currentGuess);
    }

    IEnumerator GameSequenceRoutine()
    {
        yield return new WaitForSeconds(4.0f);

        while (currentIteration <= maxIterations && isGameRunning)
        {
            Debug.Log($"--- LOCATION RECALL LEVEL 2: Iterace {currentIteration}/{maxIterations} ---");
            yield return StartCoroutine(PlaySingleRound());

            if (SessionManager.Instance != null && currentIteration < maxIterations)
            {
                SessionManager.Instance.NextIteration();
            }
            currentIteration++;
        }

        if (isGameRunning)
        {
            yield return StartCoroutine(ShowFeedbackMessage("<color=green>Výbornì!</color>\nVšechna kola zrychleného testu splnìna.", 4.0f));
            StopGame();
        }
    }

    IEnumerator PlaySingleRound()
    {
        Transform head = Camera.main.transform;

        Vector3? spawnPos = null;
        if (SpaceOptimizer.Instance != null)
        {
            spawnPos = SpaceOptimizer.Instance.GetSafePosition(head, 1.5f, 2.5f, 1.0f, 1.4f, 0.8f);
        }

        
        if (!spawnPos.HasValue)
        {
            
            SessionManager.Instance?.SendSystemStatus("error_no_space", "LocationRecall", 2, maxIterations);

            
            Debug.LogError("Nedostatek místa pro spawn Location kostky!");

            
            yield return StartCoroutine(ShowFeedbackMessage("<color=red>Nedostatek místa.</color>\nOdstupte od pøekážky.", 3.0f));

            isGameRunning = false;
            yield break; 
        }

        SessionManager.Instance?.SendSystemStatus("active", "LocationRecall", 2, maxIterations);

        Vector3 finalPos = spawnPos.Value;
        Vector3 lookDirection = head.position - finalPos;
        lookDirection.y = 0;

        currentTarget = Instantiate(targetPrefab, finalPos, Quaternion.LookRotation(lookDirection), transform);
        secretPosition = currentTarget.transform.position;

        yield return new WaitForSeconds(showTime);

        currentTarget.GetComponent<Renderer>().enabled = false;
        yield return new WaitForSeconds(2.0f);

        Quaternion headRotation = Quaternion.Euler(0, head.eulerAngles.y, 0);
        Vector3 guessOffset = new Vector3(0, 1.3f - head.position.y, 0.4f);
        Vector3 playerPos = head.position + (headRotation * guessOffset);

        Vector3 guessLook = head.position - playerPos;
        guessLook.y = 0;

        currentGuess = Instantiate(guessPrefab, playerPos, Quaternion.LookRotation(guessLook), transform);

        ButtonTrigger btn = currentGuess.GetComponentInChildren<ButtonTrigger>();
        if (btn != null)
        {
            btn.SetupButton(this);
        }
        else
        {
            Debug.LogError("LocationLevel2: Nenašel jsem ButtonTrigger na guessPrefab!");
        }

        startTime = Time.time;
        waitingForPlayer = true;

        LogEvent("Player_Turn_Start", 0f, 0f);

        while (waitingForPlayer)
        {
            yield return null;
        }

        if (currentTarget != null)
        {
            Renderer targetRenderer = currentTarget.GetComponent<Renderer>();
            targetRenderer.enabled = true;
            if (ghostMaterial != null) targetRenderer.material = ghostMaterial;
        }

        if (currentIteration < maxIterations)
        {
            yield return StartCoroutine(ShowFeedbackMessage("<color=green>Výbornì!</color>\nPøipravte se na další kolo...", 4.0f));
        }
        else
        {
            yield return new WaitForSeconds(4.0f);
        }

        Destroy(currentTarget);
        Destroy(currentGuess);
    }

    IEnumerator ShowFeedbackMessage(string message, float duration)
    {
        if (feedbackCanvasPrefab != null)
        {
            Transform head = Camera.main.transform;
            Vector3 targetPos = head.position + (head.forward * 1.0f);
            targetPos.y += 0.2f;

            GameObject feedbackObj = Instantiate(feedbackCanvasPrefab, targetPos, Quaternion.identity, transform);
            feedbackObj.transform.LookAt(head);
            feedbackObj.transform.Rotate(0, 180, 0);

            TMP_Text txt = feedbackObj.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = message;

            yield return new WaitForSeconds(duration);

            if (feedbackObj != null) Destroy(feedbackObj);
        }
        else
        {
            yield return new WaitForSeconds(duration);
        }
    }

    public void OnConfirmPlacement()
    {
        if (!waitingForPlayer) return;
        waitingForPlayer = false;

        Vector3 playerGuessPosition = currentGuess.transform.position;
        float errorDistance = Vector3.Distance(playerGuessPosition, secretPosition);
        float errorCm = errorDistance * 100f;
        float timeTaken = Time.time - startTime;

        LogEvent("Placement_Confirmed", errorCm, timeTaken);
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
}