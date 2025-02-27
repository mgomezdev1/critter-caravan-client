using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AsyncUtils
{
    public static Task<Scene> LoadSceneAsync(int buildIndex)
    {
        Debug.Log($"Fetching scene with index {buildIndex}");
        Scene targetScene = SceneManager.GetSceneByBuildIndex(buildIndex);
        if (targetScene.isLoaded) {
            Debug.Log($"Scene with index {buildIndex} is already loaded. Retrieving...");
            return Task.FromResult(targetScene); 
        }

        TaskCompletionSource<Scene> resolution = new();

        void HandleLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"Scene {scene.buildIndex} loaded in mode {mode}");
            if (targetScene.isLoaded) { 
                resolution.SetResult(targetScene);
                SceneManager.sceneLoaded -= HandleLoaded;
                return;
            }

            if (scene.buildIndex != buildIndex) return;

            resolution.SetResult(scene);
            SceneManager.sceneLoaded -= HandleLoaded;
        }

        Debug.Log($"Loading scene {buildIndex}");
        SceneManager.sceneLoaded += HandleLoaded;
        SceneManager.LoadSceneAsync(buildIndex);

        return resolution.Task;
    }

    public static async Task<T1> WhenAll<T1>(Task<T1> task1, params Task[] noReturnTasks)
    {
        await Task.WhenAll(noReturnTasks.Concat(new[] { task1 }));
        return (task1.Result);
    }

    public static async Task<(T1,T2)> WhenAll<T1,T2>(Task<T1> task1, Task<T2> task2, params Task[] noReturnTasks)
    {
        await Task.WhenAll(noReturnTasks.Concat(new Task[] { task1, task2 }));
        return (task1.Result, task2.Result);
    }
    public static async Task<(T1, T2, T3)> WhenAll<T1, T2, T3>(Task<T1> task1, Task<T2> task2, Task<T3> task3, params Task[] noReturnTasks)
    {
        await Task.WhenAll(noReturnTasks.Concat(new Task[] { task1, task2, task3 }));
        return (task1.Result, task2.Result, task3.Result);
    }
    public static async Task<(T1, T2, T3, T4)> WhenAll<T1, T2, T3, T4>(Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4, params Task[] noReturnTasks)
    {
        await Task.WhenAll(noReturnTasks.Concat(new Task[] { task1, task2, task3, task4 }));
        return (task1.Result, task2.Result, task3.Result, task4.Result);
    }
    public static async Task<(T1, T2, T3, T4, T5)> WhenAll<T1, T2, T3, T4, T5>(Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4, Task<T5> task5, params Task[] noReturnTasks)
    {
        await Task.WhenAll(noReturnTasks.Concat(new Task[] { task1, task2, task3, task4, task5 }));
        return (task1.Result, task2.Result, task3.Result, task4.Result, task5.Result);
    }
}