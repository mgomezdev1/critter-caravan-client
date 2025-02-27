using Networking;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Extensions;
using System.Threading;

public class LevelCompendium : ILevelCompendium
{
    private readonly IPaginator<ILevel> pages;

    public int PageCount => pages.PageCount;

    public async Task<ILevelCompendiumPage> GetPage(int pageIndex, CancellationToken cancellationToken = default)
    {
        Sprite thumbnail = AssetManager.DefaultLevelPageBackground;
        List<ILevel> loadedPages = await pages.FetchPage(pageIndex).ToList(cancellationToken);
        return new LevelCompendiumPage(thumbnail, loadedPages);
    }

    public LevelCompendium(IPaginator<ILevel> pages)
    {
        this.pages = pages;
    }

    public override string ToString()
    {
        return this.pages.ToString();
    }
}