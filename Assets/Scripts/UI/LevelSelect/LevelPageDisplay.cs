using Networking;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Extensions;

#nullable enable
[UxmlElement]
public partial class LevelPageDisplay : VisualElement
{
    [UxmlAttribute]
    public int NumCols {
        get => numCols;
        set
        {
            numCols = value;
            RestructureColumns();
        }
    }
    private int numCols = 3;

    public ILevelCompendiumPage? CurrentPage => lastLoadedPage;
    private ILevelCompendiumPage? lastLoadedPage;

    readonly ScrollView columnHolder;
    readonly VisualElement noResultsIndicator;
    readonly List<LevelDisplay> displays = new();

    public LevelPageDisplay()
    {
        columnHolder = new();
        columnHolder.AddToClassList("level-columns");
        Add(columnHolder);

        noResultsIndicator = new Label() { text = "No Results"};
        noResultsIndicator.AddToClassList("cover-fadetext-large");
        Add(noResultsIndicator);

        RestructureColumns();
    }

    public async Task LoadPage(ILevelCompendiumPage page, CancellationToken cancellationToken = default)
    {
        lastLoadedPage = page;
        if (columnHolder.childCount != numCols)
        {
            RestructureColumns();
        }

        if (page == null)
        {
            // clear all existing levels and show "no results"
            ShowNoResults(true);
            return;
        }

        int index = 0;
        List<Task> levelLoadTasks = new();
        var cols = columnHolder.Children().ToArray();
        await foreach (ILevel level in page.GetLevels(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            LevelDisplay target;
            Debug.Log($"Loading level {level.Name} into level display {index}.");
            if (index < displays.Count)
            {
                target = displays[index];
            }
            else
            {
                target = new();
                cols[index % numCols].Add(target);
                displays.Add(target);
            }
            target.style.display = DisplayStyle.Flex;
            levelLoadTasks.Add(target.SetLevel(level, cancellationToken));
            index++;
        }

        // If no levels have been processed, show the "No Results" text.
        ShowNoResults(index == 0);

        // Hide all unused displays
        for (; index < displays.Count; index++)
        {
            displays[index].style.display = DisplayStyle.None;
        }

        await Task.WhenAll(levelLoadTasks);
    }

    public void RestructureColumns()
    {
        foreach (var display in displays)
        {
            display.RemoveFromHierarchy();
        }
        columnHolder.Clear();
        for (int i = 0; i < numCols; i++)
        {
            VisualElement col = new();
            col.AddToClassList("level-column");
            columnHolder.Add(col);
        }
        foreach (var (display, col) in displays.ZipAround(columnHolder.Children()))
        {
            col.Add(display);
        }
    }

    public void ShowNoResults(bool visible)
    {
        if (noResultsIndicator == null) return;
        noResultsIndicator.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}