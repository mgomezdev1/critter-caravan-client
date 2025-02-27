using UnityEngine;
using System.Collections;
using Networking;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class SceneHelper : SingletonBehaviour<SceneHelper>
{
	protected override void Awake()
	{
		base.Awake();
		DontDestroyOnLoad(gameObject);
	}

    public int loading = 0;
    public string state = "";
    public int stateRetries = 0;

    public static async Task LoadLevel(ILevel level, GameMode gameMode = GameMode.Play)
    {
        Instance.state = "Fetching World";
        var levelData = await level.FetchWorldData();
        Debug.Log($"Level data fetched for loading");
        LoadLevel(levelData, gameMode);
    }
    public static void LoadLevel(WorldSaveData level, GameMode gameMode = GameMode.Play)
    {
        Instance.state = "Loading Scene";
        // Scene scene = await AsyncUtils.LoadSceneAsync(2);
        // Debug.Log($"Scene Loaded.");
        // SceneManager.SetActiveScene(scene);
        Instance.loading = 2;
        Instance.StartCoroutine(Instance.LoadSceneWithCheck(2, gameMode));
        Debug.Log($"Scene Activated. Waiting for manager.");
    }

    IEnumerator LoadSceneWithCheck(int buildIndex, GameMode gameMode)
    {
        Debug.Log("Starting async scene load...");
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(buildIndex);

        state = "Checking for Scene...";
        if (asyncOp == null)
        {
            Debug.LogError("SceneManager.LoadSceneAsync returned null!");
            yield break;
        }

        state = "Loading Scene Started";
        stateRetries = 0;
        while (!asyncOp.isDone)
        {
            Debug.Log($"Loading progress: {asyncOp.progress}");
            yield return null;
            stateRetries++;
        }

        Debug.Log("Scene loaded successfully!");
        state = "Loading Scene Completed";
        stateRetries = 0;

        WorldManager manager = null;
        while (manager == null)
        {
            yield return null;
            manager = WorldManager.Instance;
            stateRetries++;
        }

        state = "Waiting for World Manager to Start";
        stateRetries = 0;
        while (!manager.didStart)
        {
            yield return null;
            stateRetries++;
        }

        state = "Setting Game Mode";
        WorldManager.SetGameMode(gameMode);
        state = "Execution Complete";
        Debug.Log("World loading completed!");
    }
}
