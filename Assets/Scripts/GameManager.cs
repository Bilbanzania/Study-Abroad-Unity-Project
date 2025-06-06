using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI")]
    public Slider scenarioSlider;
    public Button startSimulationButton;
    public Button goToResultsButton;
    public TextMeshProUGUI currentParametersText;
    public Button togglePauseResumeButton;
    public TextMeshProUGUI togglePauseResumeButtonText;

    [Header("Scene Config")]
    public string resultSceneName = "ResultScene";

    [Header("Agent System")]
    public List<AgentSpawner> agentSpawners;
    public Transform taitoStationExitPoint;

    [Header("Study Rooms")]
    public List<StudyRoom> allConfigurableStudyRooms;

    [Header("Scenario Params: Agent (Slider Controlled)")]
    public float agentSpeed_Worst = 20.0f;
    public float agentSpeed_Best = 10.0f;
    public float agentSpawnInterval_Worst = 10f;
    public float agentSpawnInterval_Best = 5f;
    public int agentsPerBatch_Worst = 20;
    public int agentsPerBatch_Best = 10;

    [Header("Scenario Params: Train (Slider Controlled)")]
    public float trainMaxSpeed_Best = 10f;
    public float trainMaxSpeed_Worst = 3f;

    [Header("ScenarioParams: Fixed Agent & Sim Behavior")]
    public float fixedStudyDuration = 20f;
    public float fixedMaxWaitTime = 6f;
    public float fixedGpaBoostRate = 0.01f;
    public float fixedGpaWaitingPenaltyRate = 0.005f;
    public float fixedGpaLeaveNoStudyPenalty = 0.2f;
    public float minArcadeMoneySpent = 1000f;
    public float maxArcadeMoneySpent = 5000f;

    [HideInInspector] public float currentAgentSpeed;
    [HideInInspector] public float currentTrainMaxSpeed;
    [HideInInspector] public float currentStudyDuration;
    [HideInInspector] public float currentMaxWaitTime;
    [HideInInspector] public float currentGpaBoostRate;
    [HideInInspector] public float currentGpaWaitingPenaltyRate;
    [HideInInspector] public float currentGpaLeaveNoStudyPenalty;
    [HideInInspector] public int currentAgentsPerBatch;

    private float currentAgentSpawnInterval;
    private int actualActiveRoomsCount = 0;
    private float agentSpawnTimer;

    public bool IsSimulationRunning { get; private set; } = false;
    private bool isManuallyPaused = false;

    private List<float> sessionFinalGPAs = new List<float>();
    private int sessionStudentsStudied = 0;
    private int sessionStudentsLeftUnstudied = 0;
    private float totalArcadeMoneySpentByLeavers = 0f;
    private int leaversWhoSpentMoney = 0;

    private const string BasePrefsKey = "SimV4_";
    private const string PrefsAvgGPA = BasePrefsKey + "AvgGPA";
    private const string PrefsStudiedCount = BasePrefsKey + "StudiedCount";
    private const string PrefsLeftUnstudiedCount = BasePrefsKey + "LeftUnstudiedCount";
    private const string PrefsScenarioVal = BasePrefsKey + "ScenarioVal";
    private const string PrefsSpawnedCount = BasePrefsKey + "SpawnedCount";
    private const string PrefsActiveRooms = BasePrefsKey + "ActiveRooms";
    private const string PrefsAvgArcadeMoney = BasePrefsKey + "AvgArcadeMoney";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        IsSimulationRunning = false;

        currentStudyDuration = fixedStudyDuration;
        currentMaxWaitTime = fixedMaxWaitTime;
        currentGpaBoostRate = fixedGpaBoostRate;
        currentGpaWaitingPenaltyRate = fixedGpaWaitingPenaltyRate;
        currentGpaLeaveNoStudyPenalty = fixedGpaLeaveNoStudyPenalty;

        if (scenarioSlider != null)
        {
            scenarioSlider.onValueChanged.AddListener(UpdateParamsFromSlider);
            UpdateParamsFromSlider(scenarioSlider.value);
        }
        else
        {
            DeriveSliderControlledParams(0.0f);
        }

        ApplyStudyRoomSettings();
        DisplayCurrentParameters();

        if (startSimulationButton != null) startSimulationButton.onClick.AddListener(InitiateSimulation);
        if (goToResultsButton != null)
        {
            goToResultsButton.onClick.AddListener(EndSimulationAndShowResults);
            goToResultsButton.interactable = false;
        }
        if (togglePauseResumeButton != null)
        {
            togglePauseResumeButton.interactable = false;
            if (togglePauseResumeButtonText != null) togglePauseResumeButtonText.text = "Pause";
        }
        Time.timeScale = 1f;
    }

    public void UpdateParamsFromSlider(float sliderValue)
    {
        DeriveSliderControlledParams(sliderValue);
    }

    void DeriveSliderControlledParams(float normalizedValFromBestToWorst)
    {
        currentAgentSpeed = Mathf.Lerp(agentSpeed_Best, agentSpeed_Worst, normalizedValFromBestToWorst);
        currentAgentsPerBatch = Mathf.RoundToInt(Mathf.Lerp(agentsPerBatch_Best, agentsPerBatch_Worst, normalizedValFromBestToWorst));
        currentAgentSpawnInterval = Mathf.Lerp(agentSpawnInterval_Best, agentSpawnInterval_Worst, normalizedValFromBestToWorst);
        currentTrainMaxSpeed = Mathf.Lerp(trainMaxSpeed_Best, trainMaxSpeed_Worst, normalizedValFromBestToWorst);

        UpdateAllExistingAgentParameters();
        UpdateAllExistingTrainParameters();
        DisplayCurrentParameters();
    }

    void ApplyStudyRoomSettings()
    {
        if (allConfigurableStudyRooms == null) { actualActiveRoomsCount = 0; return; }
        actualActiveRoomsCount = 0;
        foreach (StudyRoom room in allConfigurableStudyRooms)
        {
            if (room != null)
            {
                room.gameObject.SetActive(true);
                room.SetMaxStudyTime(currentStudyDuration);
                actualActiveRoomsCount++;
            }
        }
    }

    void UpdateAllExistingAgentParameters()
    {
        AgentAI[] agents = FindObjectsByType<AgentAI>(FindObjectsSortMode.None);
        foreach (AgentAI agent in agents)
        {
            if (agent != null) agent.UpdateParametersFromGameManager();
        }
    }

    void UpdateAllExistingTrainParameters()
    {
        MoveAtoB[] trains = FindObjectsByType<MoveAtoB>(FindObjectsSortMode.None);
        foreach (MoveAtoB train in trains)
        {
            if (train != null)
            {
                train.UpdateMaxSpeedFromGameManager(currentTrainMaxSpeed);
            }
        }
    }

    void DisplayCurrentParameters()
    {
        if (currentParametersText != null)
            currentParametersText.text = $"Active Rooms: {actualActiveRoomsCount}\nAgent Speed: {currentAgentSpeed:F1}\nTrain Max Speed: {currentTrainMaxSpeed:F1}\nStudy: {currentStudyDuration:F0}s, Wait: {currentMaxWaitTime:F0}s\nSpawn Batch: {currentAgentsPerBatch}\nGM Spawn Interval: {currentAgentSpawnInterval:F1}s";
    }

    public void InitiateSimulation()
    {
        IsSimulationRunning = true;
        isManuallyPaused = false;
        Time.timeScale = 1f;
        agentSpawnTimer = currentAgentSpawnInterval * 0.1f;

        sessionFinalGPAs.Clear();
        sessionStudentsStudied = 0;
        sessionStudentsLeftUnstudied = 0;
        totalArcadeMoneySpentByLeavers = 0f;
        leaversWhoSpentMoney = 0;

        if (scenarioSlider != null) UpdateParamsFromSlider(scenarioSlider.value);
        else DeriveSliderControlledParams(0.0f);

        ApplyStudyRoomSettings();
        // DisplayCurrentParameters() is called by DeriveSliderControlledParams

        if (startSimulationButton != null) startSimulationButton.interactable = false;
        if (scenarioSlider != null) scenarioSlider.interactable = true;
        if (goToResultsButton != null) goToResultsButton.interactable = true;
        if (togglePauseResumeButton != null)
        {
            togglePauseResumeButton.interactable = true;
            if (togglePauseResumeButtonText != null) togglePauseResumeButtonText.text = "Pause";
        }
    }

    void Update()
    {
        if (!IsSimulationRunning || Time.timeScale <= 0) return;

        if (agentSpawners != null && agentSpawners.Any(s => s != null))
        {
            agentSpawnTimer -= Time.deltaTime;
            if (agentSpawnTimer <= 0)
            {
                TriggerAgentSpawners(currentAgentsPerBatch);
                agentSpawnTimer = currentAgentSpawnInterval;
            }
        }
    }

    void TriggerAgentSpawners(int totalAgentsInBatch)
    {
        if (agentSpawners == null || !agentSpawners.Any()) return;

        List<AgentSpawner> activeSpawners = agentSpawners.Where(s => s != null && s.gameObject.activeInHierarchy).ToList();
        if (!activeSpawners.Any()) return;

        int agentsPerSpawner = Mathf.Max(1, Mathf.FloorToInt((float)totalAgentsInBatch / activeSpawners.Count));
        int remainder = totalAgentsInBatch % activeSpawners.Count;

        for (int i = 0; i < activeSpawners.Count; i++)
        {
            int countForThisSpawner = agentsPerSpawner + (i < remainder ? 1 : 0);
            if (countForThisSpawner > 0) activeSpawners[i].SpawnAgents(countForThisSpawner);
        }
    }

    public List<StudyRoom> GetActiveStudyRooms()
    {
        if (allConfigurableStudyRooms == null) return new List<StudyRoom>();
        return allConfigurableStudyRooms.Where(r => r != null && r.gameObject.activeInHierarchy).ToList();
    }

    public Transform GetTaitoStationExitPoint()
    {
        if (taitoStationExitPoint == null) Debug.LogError("[GameManager] CRITICAL: TaitoStationExitPoint is NOT ASSIGNED!");
        return taitoStationExitPoint;
    }

    public void RecordAgentOutcome(float finalGPA, bool studied, float arcadeMoneySpent)
    {
        sessionFinalGPAs.Add(finalGPA);
        if (studied)
        {
            sessionStudentsStudied++;
        }
        else
        {
            sessionStudentsLeftUnstudied++;
            if (arcadeMoneySpent > 0)
            {
                totalArcadeMoneySpentByLeavers += arcadeMoneySpent;
                leaversWhoSpentMoney++;
            }
        }
    }

    public void EndSimulationAndShowResults()
    {
        IsSimulationRunning = false;
        isManuallyPaused = false;
        Time.timeScale = 1f;

        float avgGPA = (sessionFinalGPAs.Count > 0) ? sessionFinalGPAs.Average() : 0f;
        float avgArcadeMoney = (leaversWhoSpentMoney > 0) ? totalArcadeMoneySpentByLeavers / leaversWhoSpentMoney : 0f;

        PlayerPrefs.SetFloat(PrefsAvgGPA, avgGPA);
        PlayerPrefs.SetInt(PrefsStudiedCount, sessionStudentsStudied);
        PlayerPrefs.SetInt(PrefsLeftUnstudiedCount, sessionStudentsLeftUnstudied);
        if (scenarioSlider != null) PlayerPrefs.SetFloat(PrefsScenarioVal, scenarioSlider.value);
        else PlayerPrefs.SetFloat(PrefsScenarioVal, 0f);
        PlayerPrefs.SetInt(PrefsSpawnedCount, sessionFinalGPAs.Count);
        PlayerPrefs.SetInt(PrefsActiveRooms, actualActiveRoomsCount);
        PlayerPrefs.SetFloat(PrefsAvgArcadeMoney, avgArcadeMoney);
        PlayerPrefs.Save();

        if (startSimulationButton != null) startSimulationButton.interactable = true;
        if (scenarioSlider != null) scenarioSlider.interactable = true;
        if (goToResultsButton != null) goToResultsButton.interactable = false;
        if (togglePauseResumeButton != null)
        {
            togglePauseResumeButton.interactable = false;
            if (togglePauseResumeButtonText != null) togglePauseResumeButtonText.text = "Pause";
        }

        if (!string.IsNullOrEmpty(resultSceneName)) SceneManager.LoadScene(resultSceneName);
        else Debug.LogError("[GameManager] CRITICAL: Result Scene Name is not set!");
    }

    public void TogglePauseState()
    {
        if (!IsSimulationRunning) return;

        isManuallyPaused = !isManuallyPaused;
        if (isManuallyPaused)
        {
            Time.timeScale = 0f;
            if (togglePauseResumeButtonText != null) togglePauseResumeButtonText.text = "Resume";
        }
        else
        {
            Time.timeScale = 1f;
            if (togglePauseResumeButtonText != null) togglePauseResumeButtonText.text = "Pause";
        }
    }
}