using Networking;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public interface ILevelCompendiumPage
{
    public Sprite Background { get; }

    public IAsyncEnumerable<ILevel> GetLevels(CancellationToken cancellationToken = default);
    public string GetPageName(int index, int total);
}