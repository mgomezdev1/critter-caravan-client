using Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class LevelSelectUIManager : BaseUIManager
{
    Window levelBrowserWindow;
    Window mainMenu;
    LevelBrowser levelBrowser;

    [SerializeField] private InbuiltLevelCompendium inbuiltLevels;

    private void Awake()
    {
        Button logOutButton = Q<Button>("LogOutButton");
        if (!SessionManager.LoggedIn) logOutButton.text = "Log In";
        logOutButton.clicked += HandleLogOut;

        Button playButton = Q<Button>("PlayButton");
        Button levelBrowserButton = Q<Button>("LevelBrowserButton");
        Button levelEditorButton = Q<Button>("LevelEditorButton");

        playButton.clicked += HandleOpenPlayMenu;
        levelBrowserButton.clicked += HandleOpenLevelBrowser;
        levelEditorButton.clicked += HandleOpenLevelEditor;

        Button backToMainButton = Q<Button>("BackToMainButton");
        levelBrowserWindow = new(Q("LevelBrowser"));
        mainMenu = new(Q("MainMenu"));
        RegisterWindow(levelBrowserWindow);
        RegisterWindow(mainMenu).AddWindowButton(backToMainButton, WindowButtonBehaviour.Set);

        Button quitButton = Q<Button>("QuitButton");
        quitButton.clicked += HandleQuit;

        levelBrowser = Q<LevelBrowser>();

        SetActiveWindow(mainMenu);
    }

    private async void HandleLogOut()
    {
        if (SessionManager.LoggedIn && !await AskConfirmation("Are you sure you want to log out?")) return;

        Task<Scene> sceneLoadTask = AsyncUtils.LoadSceneAsync(0);
        Task logOutTask = SessionManager.LogOut();

        await Task.WhenAll(sceneLoadTask, logOutTask);
        SceneManager.SetActiveScene(sceneLoadTask.Result);
    }
    private async void HandleQuit()
    {
        if (!await AskConfirmation("Are you sure you want to quit?")) return;

        Application.Quit();
    }

    private void HandleOpenPlayMenu()
    {
        SetActiveWindow(levelBrowserWindow);
        levelBrowser.LevelCompendium = inbuiltLevels;
    }
    private void HandleOpenLevelBrowser()
    { 
        SetActiveWindow(levelBrowserWindow);
    }
    private void HandleOpenLevelEditor()
    {
        SetActiveWindow(levelBrowserWindow);
    }
}
