using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface ILevelCompendium
{
    public Task<ILevelCompendiumPage> GetPage(int pageIndex, CancellationToken cancellationToken = default);
    public int PageCount { get; }
}