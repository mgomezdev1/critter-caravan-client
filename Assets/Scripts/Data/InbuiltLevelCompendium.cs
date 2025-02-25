using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#nullable enable
[CreateAssetMenu(fileName = "Level Compendium", menuName = "Scriptable Objects/Level Compendium")]
public class InbuiltLevelCompendium : ScriptableObject, ILevelCompendium
{
    public IEnumerable<ILevelCompendiumPage> Pages => pages;
    [SerializeField] private List<InbuiltLevelCompendiumPage> pages = new();

    public int PageCount => pages.Count;

    public Task<ILevelCompendiumPage> GetPage(int pageIndex, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ILevelCompendiumPage>(pages[pageIndex]);
    }
}