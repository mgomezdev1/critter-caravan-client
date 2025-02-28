using Networking;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

using static Networking.ServerAPI.Levels;

#nullable enable
public class LevelSelectUIManager : BaseUIManager
{
    Window levelBrowserWindow;
    Window mainMenu;
    VisualElement searchSettingsPanel;
    LevelPageDisplay browserPageDisplay;
    Button nextPageBtn;
    Button prevPageBtn;
    Button firstPageBtn;
    Button lastPageBtn;

    ILevelCompendium activeCompendium;

    [SerializeField] int currentPageIdx;
    Label pageLabel;

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

        searchSettingsPanel = Q("SearchOptions");
        browserPageDisplay = Q<LevelPageDisplay>();

        pageLabel = Q<Label>("PageLabel");
        nextPageBtn = Q<Button>("NextPageButton");
        prevPageBtn = Q<Button>("PrevPageButton");
        firstPageBtn = Q<Button>("FirstPageButton");
        lastPageBtn = Q<Button>("LastPageButton");

        nextPageBtn.clicked += HandleNextPage;
        prevPageBtn.clicked += HandlePrevPage;
        firstPageBtn.clicked += HandleFirstPage;
        lastPageBtn.clicked += HandleLastPage;

        activeCompendium = inbuiltLevels;

        Button searchButton = Q<Button>("SearchButton");
        searchButton.clicked += HandleSearch;

        SetActiveWindow(mainMenu);
    }

    private void OnDestroy()
    {
        cancellationTokenSource?.Dispose();
    }

    private async void HandleLogOut()
    {
        if (SessionManager.LoggedIn && !await AskConfirmation("Are you sure you want to log out?")) return;

        Task<Scene> sceneLoadTask = AsyncUtils.LoadSceneAsync(0);
        Task logOutTask = SessionManager.LogOut();

        Scene loadedScene = await AsyncUtils.WhenAll(sceneLoadTask, logOutTask);
        SceneManager.SetActiveScene(loadedScene);
    }
    private async void HandleQuit()
    {
        if (!await AskConfirmation("Are you sure you want to quit?")) return;

        Application.Quit();
    }

    private async void HandleOpenPlayMenu()
    {
        SetActiveWindow(levelBrowserWindow);
        activeCompendium = inbuiltLevels;
        HandleShowSearchOptions(false);
        await LoadPage(0);
    }
    private async void HandleOpenLevelBrowser()
    {
        HandleShowSearchOptions(true);
        activeCompendium = new LevelCompendium(await ServerAPI.Levels.FetchLevels());
        SetActiveWindow(levelBrowserWindow);
        await LoadPage(0);
    }
    private void HandleOpenLevelEditor()
    {
        WorldSaveData data = new(new Vector2Int(16, 9), new List<ObstacleSaveData>());
        SceneHelper.LoadLevel(data, GameMode.LevelEdit);
    }

    private async void HandleSearch()
    {
        LevelFetchQParams queryParameters = new()
        {
            Author = Q<StringField>("AuthorNameFilter").Value,
            LevelName = Q<StringField>("LevelNameFilter").Value,
            Category = Q<StringField>("CategoryFilter").Value,
            MinVerificationLevel = Q<VerificationLevelDropdown>("MinVerificationLevelSelector").Value,
            MaxVerificationLevel = Q<VerificationLevelDropdown>("MaxVerificationLevelSelector").Value,
            SortCriterion = Q<SortCriterionDropdown>().Value,
            SortAscending = Q<ToggleField>("SortAscendingToggle").Value
        };

        activeCompendium = new LevelCompendium(await FetchLevels(queryParameters));
        await LoadPage(0);
    }

    CancellationTokenSource? cancellationTokenSource = null;
    public async Task LoadPage(int index)
    {
        if (activeCompendium == null) return;
        int pageCount = activeCompendium.PageCount;
        if (index < 0 || index >= pageCount)
        {
            return;
        }
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();

        cancellationTokenSource = new();

        ILevelCompendiumPage page = await activeCompendium.GetPage(index);
        try
        {
            currentPageIdx = index;
            pageLabel.text = page.GetPageName(index, pageCount);
            UpdatePageButtonStates(currentPageIdx, pageCount);
            browserPageDisplay.style.backgroundImage = new StyleBackground(page.Background);
            await browserPageDisplay.LoadPage(page, cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Browser page load operation was cancelled");
        }
    }

    public async void HandleNextPage()
    {
        await LoadPage(currentPageIdx + 1);
    }
    public async void HandlePrevPage()
    {
        await LoadPage(currentPageIdx - 1);
    }
    public async void HandleLastPage()
    {
        await LoadPage((activeCompendium?.PageCount ?? 0) - 1);
    }
    public async void HandleFirstPage()
    {
        await LoadPage(0);
    }
    public void UpdatePageButtonStates(int pageIdx, int pageCount)
    {
        firstPageBtn.SetEnabled(pageIdx > 0);
        prevPageBtn.SetEnabled(pageIdx > 0);
        lastPageBtn.SetEnabled(pageIdx < pageCount - 1);
        nextPageBtn.SetEnabled(pageIdx < pageCount - 1);
    }

    public void HandleShowSearchOptions(bool showSearchOptions)
    {
        searchSettingsPanel.style.visibility = showSearchOptions ? Visibility.Visible : Visibility.Hidden;
    }
}
