using Extensions;
using Networking;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

#nullable enable
public class LoginUIManager : BaseUIManager
{
    StringField UsernameField { get; set; }
    StringField PasswordField { get; set; }
    Label GlobalErrorLabel { get; set; }

    private Button loginButton;
    private Button guestButton;

    [SerializeField] bool autoCheckInGuests = false;

    void Awake()
    {
        _document = GetComponent<UIDocument>();

        UsernameField = Q<StringField>("UsernameField");
        PasswordField = Q<StringField>("PasswordField");
        GlobalErrorLabel = Q<Label>("GlobalErrorLabel");

        UsernameField.OnSubmit += HandleLogin;
        PasswordField.OnSubmit += HandleLogin;

        foreach (var label in Query<Label>())
        {
            if (label == null) continue;
            label.text = FillInFormatText(label.text);
        }

        loginButton = Q<Button>("LoginButton");
        loginButton.clicked += HandleLogin;

        guestButton = Q<Button>("GuestButton");
        guestButton.clicked += HandleGuestSignIn;
        
        SetErrorMessage(null);
    }

    private async void Start()
    {
        var session = await SessionManager.GetSessionAsync();
        if (session.IsGuest && autoCheckInGuests) 
        { 
            HandleGuestSignIn(); 
        }
        else if (!session.IsGuest && session.UserToken != null)
        {
            if (!session.TokenExpiration.HasValue || session.TokenExpiration.Value < DateTime.Now + TimeSpan.FromSeconds(ServerAPI.TIMEOUT))
            {
                SetErrorMessage($"Your login information expired. Please log in again.");
            }
            else
            {
                try
                {
                    await SessionManager.LoginWithToken(session.UserToken);
                    await LoadLevelsScene();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }

    private bool loginInProgress = false;
    private async void HandleLogin()
    {
        if (loginInProgress) return;
        
        // gather credentials and hide global error.
        UserLogin login = new(UsernameField.Value ?? string.Empty, PasswordField.Value ?? string.Empty);
        SetErrorMessage(null);

        try
        {
            SetLoginState(true);
            await SessionManager.Login(login);

            loginButton.text = "Loading...";
            await LoadLevelsScene();
        }
        catch (ServerAPIException e)
        {
            ServerAggregateValidationResult? formValidation = null;
            if (e.ResponseBody != null)
            {
                formValidation = JsonConvert.DeserializeObject<ServerAggregateValidationResult>(e.ResponseBody);
            }

            if (formValidation != null)
            {
                Dictionary<string, DataField> fields = new() {
                    { "username", UsernameField },
                    { "password", PasswordField }
                };
                ServerAPI.ShowServerFormErrors(formValidation, fields, GlobalErrorLabel);
            }
            else
            {
                SetErrorMessage(e.Message);
            }
        }
        catch (Exception e) {
            SetErrorMessage($"Something went wrong internally when logging you in. If this error persists, contact support.\n Error: {e.Message}");
            Debug.LogException(e); 
        }
        finally
        {
            SetLoginState(false);
        }
    }

    private void SetLoginState(bool inProgress)
    {
        loginInProgress = inProgress;
        loginButton.SetEnabled(!inProgress);
        guestButton.SetEnabled(!inProgress);

        loginButton.text = inProgress ? "Signing in..." : "Sign in";
    }

    private async void HandleGuestSignIn()
    {
        if (loginInProgress)
        {
            SetErrorMessage("You can't sign in as guest while another sign in operation is in progress.");
            return;
        }
        await SessionManager.SetGuestSession(User.GuestUser);
        guestButton.text = "Loading...";
        await LoadLevelsScene();
    }

    public void SetErrorMessage(string? message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            GlobalErrorLabel.style.display = DisplayStyle.Flex;
            GlobalErrorLabel.text = message;
        } 
        else
        {
            GlobalErrorLabel.style.display = DisplayStyle.None;
        }
    }

    private async Task LoadLevelsScene()
    {
        Task<Scene> sceneLoadTask = AsyncUtils.LoadSceneAsync(1);
        Task levelLoadTask = SessionManager.GetSession().LoadLevelStats(SessionManager.CurrentUser?.Id);

        loginButton.SetEnabled(false);
        guestButton.SetEnabled(false);

        await Task.WhenAll(sceneLoadTask, levelLoadTask);

        Scene targetScene = sceneLoadTask.Result;
        SceneManager.SetActiveScene(targetScene);
    }
}
