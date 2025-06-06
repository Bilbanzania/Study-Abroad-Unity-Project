using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ResultSceneManager : MonoBehaviour
{
    public TextMeshProUGUI scenarioValDisplay, spawnedCountDisplay, activeRoomsDisplay, avgGpaDisplay, studiedCountDisplay, leftUnstudiedDisplay, avgArcadeMoneyDisplay;
    public string titleSceneName = "TitleScene";

    private const string BasePrefsKey = "SimV4_";
    private const string PrefsAvgGPA = BasePrefsKey + "AvgGPA";
    private const string PrefsStudiedCount = BasePrefsKey + "StudiedCount";
    private const string PrefsLeftUnstudiedCount = BasePrefsKey + "LeftUnstudiedCount";
    private const string PrefsScenarioVal = BasePrefsKey + "ScenarioVal";
    private const string PrefsSpawnedCount = BasePrefsKey + "SpawnedCount";
    private const string PrefsActiveRooms = BasePrefsKey + "ActiveRooms";
    private const string PrefsAvgArcadeMoney = BasePrefsKey + "AvgArcadeMoney";

    void Start()
    {
        LoadAndDisplay();
    }

    void LoadAndDisplay()
    {
        float loadedScenarioVal = PlayerPrefs.GetFloat(PrefsScenarioVal, 0f);
        int loadedSpawnedCount = PlayerPrefs.GetInt(PrefsSpawnedCount, 0);
        int loadedActiveRooms = PlayerPrefs.GetInt(PrefsActiveRooms, 0);
        float loadedAvgGpa = PlayerPrefs.GetFloat(PrefsAvgGPA, 0f);
        int loadedStudiedCount = PlayerPrefs.GetInt(PrefsStudiedCount, 0);
        int loadedLeftUnstudied = PlayerPrefs.GetInt(PrefsLeftUnstudiedCount, 0);
        float loadedAvgArcadeMoney = PlayerPrefs.GetFloat(PrefsAvgArcadeMoney, 0f);

        if (scenarioValDisplay) scenarioValDisplay.text = $"Input Scenario: {loadedScenarioVal:F2}";
        if (spawnedCountDisplay) spawnedCountDisplay.text = $"Agents Processed: {loadedSpawnedCount}";
        if (activeRoomsDisplay) activeRoomsDisplay.text = $"Active Study Rooms: {loadedActiveRooms}";
        if (avgGpaDisplay) avgGpaDisplay.text = $"Output Avg GPA: {loadedAvgGpa:F2}";
        if (studiedCountDisplay) studiedCountDisplay.text = $"Output Studied: {loadedStudiedCount}";
        if (leftUnstudiedDisplay) leftUnstudiedDisplay.text = $"Output Left Unstudied: {loadedLeftUnstudied}";
        if (avgArcadeMoneyDisplay) avgArcadeMoneyDisplay.text = $"Avg Arcade Spend (Leavers): ¥{loadedAvgArcadeMoney:N0}";
    }

    public void GoToTitleScreenButton()
    {
        if (!string.IsNullOrEmpty(titleSceneName)) SceneManager.LoadScene(titleSceneName);
        else Debug.LogError("[ResultSceneManager] CRITICAL: Title Scene Name is not set!");
    }

    public void QuitGameButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}