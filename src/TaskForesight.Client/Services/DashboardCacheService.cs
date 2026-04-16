using TaskForesight.Shared.Dto;

namespace TaskForesight.Client.Services;

public class DashboardCacheService
{
    public DashboardStats? Stats { get; set; }
    public IReadOnlyList<CategoryStats>? Categories { get; set; }
    public IReadOnlyList<ToxicChain>? ToxicChains { get; set; }
    public IReadOnlyList<string>? Developers { get; set; }
    public IReadOnlyList<string>? Components { get; set; }

    public bool HasData => Stats is not null && Categories is not null;

    public void Clear()
    {
        Stats = null;
        Categories = null;
        ToxicChains = null;
        Developers = null;
        Components = null;
    }
}
