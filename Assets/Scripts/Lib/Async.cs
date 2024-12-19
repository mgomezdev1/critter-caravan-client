using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public static class AsyncUtils
{
    public static Task<Scene> LoadSceneAsync(int buildIndex)
    {
        Scene scene = SceneManager.GetSceneByBuildIndex(buildIndex);
        if (scene.isLoaded) { return Task.FromResult(scene); }

        TaskCompletionSource<Scene> resolution = new();

        void HandleLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex != buildIndex) return;

            resolution.SetResult(scene);
            SceneManager.sceneLoaded -= HandleLoaded;
        }
        SceneManager.sceneLoaded += HandleLoaded;
        SceneManager.LoadSceneAsync(buildIndex);

        return resolution.Task;
    }
}