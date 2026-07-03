using UnityEngine;
using TMPro;
using System.Collections;

public class RotationLevel2Manager : MonoBehaviour, IRotationLevelManager
{
    [Header("Nastavení Hry")]
    public GameObject cubePrefab;
    public GameObject canvasPrefab;

    [Tooltip("Šedý nebo prázdný materiál, který skryje barvy")]
    public Material hiddenMaterial;

    [Tooltip("Tolerance natoèení (0.85 je doporuèeno)")]
    [Range(0.5f, 1.0f)]
    public float angleTolerance = 0.85f;

    private GameObject spawnedCanvas;
    private TMP_Text taskText;
    private bool isPaused = false;

    private RotationCube activeCube;

    private Material targetMaterial1;
    private Material targetMaterial2;
    private Material finalTargetMaterial;

    private bool isGameRunning = false;
    private bool isMemorizing = true;
    private Material[] savedRoundMaterials;

    // SJEDNOCENÉ PROMÌNNÉ
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

            // --- GLOBÁLNÍ LOG ÚSPÌCHU ---
            SessionManager.Instance?.SendSystemStatus("active", "RotationCube", 2, maxIterations);
        }
        else
        {
            // --- GLOBÁLNÍ LOG CHYBY ---
            SessionManager.Instance?.SendSystemStatus("error_no_space", "RotationCube", 2, maxIterations);

            
            Debug.LogError("Nelze spawnout kostku, v prostoru pøed hráèem nìco pøekáží!");
        }
    }

    public void StopGame()
    {
        isGameRunning = false;

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
        isMemorizing = true;

        if (spawnedCanvas != null)
        {
            RotationButtonTrigger[] buttons = spawnedCanvas.GetComponentsInChildren<RotationButtonTrigger>();
            foreach (var btn in buttons) btn.ResetColor();
        }

        activeCube.InitializeRandomColors();

        float[] angles = { 0f, 90f, 180f, 270f };
        activeCube.transform.rotation = Quaternion.Euler(
            angles[Random.Range(0, angles.Length)],
            angles[Random.Range(0, angles.Length)],
            angles[Random.Range(0, angles.Length)]
        );

        savedRoundMaterials = activeCube.cubeRenderer.materials;

        targetMaterial1 = null;
        targetMaterial2 = null;

        for (int i = 0; i < 50; i++)
        {
            Material tempMat = activeCube.GetRandomTargetMaterial();
            Vector3 targetDirection = activeCube.GetWorldDirectionOfMaterial(tempMat);

            if (Vector3.Dot(targetDirection, Vector3.up) < angleTolerance)
            {
                if (targetMaterial1 == null)
                {
                    targetMaterial1 = tempMat;
                }
                else if (targetMaterial1.name != tempMat.name && targetMaterial2 == null)
                {
                    targetMaterial2 = tempMat;
                    break;
                }
            }
        }

        UpdateTaskText();
        string logColor1 = GetCzechColorName(targetMaterial1.name);
        string logColor2 = GetCzechColorName(targetMaterial2.name);
        LogEvent("Player_Turn_Start", $"Memorize:{logColor1}+{logColor2}");
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

        if (isMemorizing)
        {
            isMemorizing = false;
            finalTargetMaterial = (Random.value > 0.5f) ? targetMaterial1 : targetMaterial2;

            string logFinalColor = GetCzechColorName(finalTargetMaterial.name);
            LogEvent("Rotation_Phase_Start", $"Target:{logFinalColor}");

            Material[] greyMats = new Material[6];
            for (int i = 0; i < 6; i++) greyMats[i] = hiddenMaterial;
            activeCube.cubeRenderer.materials = greyMats;

            if (pressedButton != null) pressedButton.ResetColor();
            UpdateTaskText();
            return;
        }

        Vector3 targetDirection = activeCube.GetWorldDirectionOfMaterial(finalTargetMaterial);
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
            activeCube.cubeRenderer.materials = savedRoundMaterials;

            if (taskText != null) taskText.text = "<color=green>Výbornì!</color>\nSprávnì natoèeno.";
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
            if (taskText != null) taskText.text = "<color=red>Špatnì!</color>\nTady ta barva nebyla.\nZkuste to znovu.";
            yield return new WaitForSeconds(4f);

            isPaused = false;
            UpdateTaskText();
        }
    }

    void FinishLevel()
    {
        if (taskText != null) taskText.text = "<color=green>Skvìlá práce!</color>\nÚroveò dokonèena.";
        Invoke("StopGame", 3f);
    }

    private void UpdateTaskText()
    {
        int currentIt = SessionManager.Instance != null ? SessionManager.Instance.CurrentIteration : currentIteration;
        int totalIt = SessionManager.Instance != null ? SessionManager.Instance.TotalIterations : maxIterations;

        if (taskText != null)
        {
            if (isMemorizing)
            {
                string color1 = GetCzechColorName(targetMaterial1.name);
                string color2 = GetCzechColorName(targetMaterial2.name);
                taskText.text = $"Kolo {currentIt}/{totalIt}\nZapamatujte si polohu barev:\n<b>{color1}</b> a <b>{color2}</b>";
            }
            else
            {
                string finalColor = GetCzechColorName(finalTargetMaterial.name);
                taskText.text = $"Kolo {currentIt}/{totalIt}\nOtoète nahoru skrytou barvu:\n<b>{finalColor}</b";
            }
        }
    }

    private string GetCzechColorName(string rawName)
    {
        string clean = rawName.ToLower();
        if (clean.Contains("red")) return "Zelená";
        if (clean.Contains("blue")) return "Èerná";
        if (clean.Contains("yellow")) return "Modrá";
        if (clean.Contains("green")) return "Bílá";
        if (clean.Contains("black")) return "Èervená";
        if (clean.Contains("white")) return "Žlutá";
        return clean.ToUpper();
    }

    void OnDestroy()
    {
        if (spawnedCanvas != null) Destroy(spawnedCanvas);
    }
}