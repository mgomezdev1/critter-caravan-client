using Networking;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UIElements;

#nullable enable
[UxmlElement]
public partial class LevelDisplay : Button
{
    public VisualElement ThumbnailHolder { get; set; }
    public Label LevelNameLabel { get; set; }

    public ILevel? CurrentLevel => lastLoadedLevel;
    public ILevel? lastLoadedLevel = null;

    public LevelDisplay()
    {
        this.AddToClassList("level-diplay");
        ThumbnailHolder = new VisualElement();
        ThumbnailHolder.AddToClassList("level-thumbnail");
        LevelNameLabel = new Label();
        LevelNameLabel.AddToClassList("level-name");

        this.Add(ThumbnailHolder);
        this.Add(LevelNameLabel);
    }

    public async Task SetLevel(ILevel level, CancellationToken cancellationToken)
    {
        lastLoadedLevel = level;
        LevelNameLabel.text = level.Name;
        var levelGraphic = await level.GetThumbnail(cancellationToken);
        ThumbnailHolder.style.backgroundImage = new StyleBackground(levelGraphic);
    }
    public async Task SetLevelThreadSafe(ILevel level, CancellationToken cancellationToken)
    {
        lastLoadedLevel = level;
        LevelNameLabel.text = level.Name;
        var levelGraphic = await level.GetThumbnail(cancellationToken);
        ThumbnailHolder.style.backgroundImage = new StyleBackground(levelGraphic);
    }

    public async Task<WorldSaveData?> Load()
    {
        if (lastLoadedLevel == null) return null;

        var levelData = await lastLoadedLevel.FetchWorldData();
        // Tell the scene manager to load this level
        throw new NotImplementedException();
        return levelData;
    }
}