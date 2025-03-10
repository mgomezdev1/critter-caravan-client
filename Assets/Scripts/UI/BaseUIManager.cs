using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UIElements;


# nullable enable
public class Window
{
    public const string HIDDEN_CLASS = "hidden";
    public readonly string containerId;
    public readonly VisualElement container;
    public bool IsVisible => !this.container.ClassListContains(HIDDEN_CLASS);

    public event Action<bool>? IsVisibleChanged;
    public event Action? OnOpen;
    public event Action? OnClose;

    public Window(VisualElement root, string containerId)
    {
        this.containerId = containerId;
        this.container = root.Q(containerId);
    }
    public Window(VisualElement container)
    {
        this.containerId = container.name;
        this.container = container;
    }

    public Window Hide()
    {
        SetVisible(false);
        return this;
    }
    public Window Show()
    {
        SetVisible(true);
        return this;
    }
    public Window SetVisible(bool shown)
    {
        container.EnableInClassList(HIDDEN_CLASS, !shown);

        // Handle event invocation
        IsVisibleChanged?.Invoke(shown);
        if (shown) { OnOpen?.Invoke(); } else { OnClose?.Invoke(); }

        return this;
    }
}

public enum WindowButtonBehaviour
{
    Set,
    Toggle,
    Open,
    Close
}

public readonly struct WindowContext
{
    private readonly BaseUIManager uiManager;
    private readonly Window window;

    public WindowContext(BaseUIManager baseUIManager, Window window)
    {
        this.uiManager = baseUIManager;
        this.window = window;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WindowContext AddWindowButton(Button button, WindowButtonBehaviour behaviour = WindowButtonBehaviour.Toggle)
    {
        return uiManager.AddWindowButton(button, window, behaviour);
    }
}

public class BaseUIManager : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] protected UIDocument _document;
#pragma warning restore CS8618
    public VisualElement Root => _document.rootVisualElement;
    protected readonly Stack<Window> openWindows = new();
    protected readonly List<Window> windows = new();

    public T Q<T>(string? name = null, string? className = null) where T : VisualElement
    {
        return Root.Q<T>(name, className);
    }
    public VisualElement Q(string? name = null, string? className = null)
    {
        return Root.Q(name, className);
    }
    public IEnumerable<T> Query<T>(string? name = null, string? className = null) where T : VisualElement
    {
        return Root.Query<T>(name, className).ToList();
    }

    public void CloseAllWindows()
    {
        while (openWindows.Count > 0)
        {
            CloseLastWindow();
        }
    }
    public void OpenWindow(Window window)
    {
        window.Show();
        openWindows.Push(window);
    }
    public void CloseLastWindow()
    {
        if (openWindows.TryPop(out Window lastWindow))
            lastWindow.Hide();
    }
    public void SetWindowShownState(Window? window, bool shown)
    {
        if (window == null) return;
        if (window.IsVisible == shown) return;

        if (shown)
        {
            OpenWindow(window);
        }
        else
        {
            CloseWindow(window);
        }
    }

    public Window? GetWindowByContainerId(string containerId)
    {
        foreach (var window in windows) {
            if (window.containerId == containerId) return window;
        }
        return null;
    }
    public void SetActiveWindow(Window? window)
    {
        foreach (var otherWindow in windows)
        {
            otherWindow.SetVisible(window == otherWindow);
        }
        openWindows.Clear();
        if (window != null) { openWindows.Push(window); }
    }
    public void SetActiveWindow(string containerId)
    {
        Window? window = GetWindowByContainerId(containerId);
        SetActiveWindow(window);
    }
    public void ToggleWindow(Window? window)
    {
        if (window == null) return;
        SetWindowShownState(window, !window.IsVisible);
    }
    public void CloseWindow(Window? window)
    {
        if (window == null) return;
        window.SetVisible(false);
        Stack<Window> temp = new();
        while (openWindows.Count > 0)
        {
            Window next = openWindows.Pop();
            if (next == window) break;
            temp.Push(next);
        }
        while (temp.Count > 0)
        {
            openWindows.Push(temp.Pop());
        }
    }

    public WindowContext RegisterWindow(Window window)
    {
        windows.Add(window);
        if (window.IsVisible)
        {
            openWindows.Push(window);
        }
        return new WindowContext(this, window);
    }
    public WindowContext AddWindowButton(Button button, Window window, WindowButtonBehaviour behaviour = WindowButtonBehaviour.Toggle)
    {
        button.clicked += behaviour switch
        {
            WindowButtonBehaviour.Set => () => SetActiveWindow(window),
            WindowButtonBehaviour.Toggle => () => ToggleWindow(window),
            WindowButtonBehaviour.Open => () => OpenWindow(window),
            WindowButtonBehaviour.Close => () => CloseWindow(window),
            _ => throw new NotImplementedException($"The WindowButtonBehaviour {behaviour} is not implemented for window buttons."),
        };
        return new WindowContext(this, window);
    }

    public static string FillInFormatText(string text)
    {
        return text.Replace("%BASE_URL%", Networking.ServerAPI.BASE_URL);
    }

    public Task<bool> AskConfirmation(string content = "Are you sure?", string cancelButtonText = "Cancel", string okButtonText = "Yes")
    {
        var taskCompletionSource = new TaskCompletionSource<bool>();

        VisualElement modalBackground = new();
        modalBackground.AddToClassList("full-cover");

        VisualElement panel = new();
        panel.AddToClassList("abs-center");
        panel.AddToClassList("panel");
        panel.AddToClassList("confirmation-panel");

        Label label = new() { text = content };
        panel.Add(label);

        VisualElement buttonHolder = new();
        buttonHolder.AddToClassList("row-center");
        Button cancelButton = new() { text = cancelButtonText };
        cancelButton.AddToClassList("cancel-button");
        Button okButton = new() { text = okButtonText };
        okButton.AddToClassList("confirm-button");

        void Resolve(bool result)
        {
            taskCompletionSource.SetResult(result);
            Root.Remove(panel);
        }

        cancelButton.clicked += () => Resolve(false);
        okButton.clicked     += () => Resolve(true);

        buttonHolder.Add(cancelButton);
        buttonHolder.Add(okButton);
        panel.Add(buttonHolder);

        modalBackground.Add(panel);

        Root.Add(modalBackground);

        return taskCompletionSource.Task;
    }
}