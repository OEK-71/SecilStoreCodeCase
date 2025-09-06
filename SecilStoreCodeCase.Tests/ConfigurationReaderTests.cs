using SecilStoreCodeCase;
using Xunit;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class ConfigurationReaderTests
{
    private static ConfigurationItem CI(string app, string name, ConfigItemType t, string val, bool active = true, DateTime? ts = null)
        => new ConfigurationItem { ApplicationName = app, Name = name, Type = t, Value = val, IsActive = active, UpdatedAtUtc = ts ?? DateTime.UtcNow };

    [Fact]
    public void Bool_Parse_Variants()
    {
        var item = CI("A", "IsFeatureXOpen", ConfigItemType.Bool, "1");
        var store = new InMemoryStore(item);
        using var r = new ConfigurationReader("A", store, TimeSpan.FromSeconds(5));
        r.GetValue<bool>("IsFeatureXOpen").Should().BeTrue();
    }

    [Fact]
    public void Int_Parse_Ok()
    {
        var item = CI("A", "MaxItemCount", ConfigItemType.Int, "42");
        var store = new InMemoryStore(item);
        using var r = new ConfigurationReader("A", store, TimeSpan.FromSeconds(5));
        r.GetValue<int>("MaxItemCount").Should().Be(42);
    }

    [Fact]
    public void Double_Parse_Ok()
    {
        var item = CI("A", "Rate", ConfigItemType.Double, "2.50");
        var store = new InMemoryStore(item);
        using var r = new ConfigurationReader("A", store, TimeSpan.FromSeconds(5));
        r.GetValue<double>("Rate").Should().Be(2.5);
    }

    [Fact]
    public void Missing_Key_Should_Throw()
    {
        var store = new InMemoryStore(); // boþ snapshot
        using var r = new ConfigurationReader("A", store, TimeSpan.FromSeconds(5));
        Action act = () => r.GetValue<string>("UnknownKey");
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public async Task Refresh_Applies_Delta()
    {
        // baþlangýç: 50
        var t0 = DateTime.UtcNow.AddSeconds(-10);
        var store = new InMemoryStore(CI("A", "MaxItemCount", ConfigItemType.Int, "50", ts: t0));
        using var r = new ConfigurationReader("A", store, TimeSpan.FromMilliseconds(200));
        r.GetValue<int>("MaxItemCount").Should().Be(50);

        // delta: 99 (daha yeni timestamp ile)
        await Task.Delay(250); // ilk refresh dönemini bekle
        store.ReplaceForApp("A", new[]
        {
            CI("A", "MaxItemCount", ConfigItemType.Int, "99", ts: DateTime.UtcNow)
        });

        await Task.Delay(400); // refresh loop tekrar çalýþsýn
        r.GetValue<int>("MaxItemCount").Should().Be(99);
    }
}


public class InMemoryStore : IConfigStore
{
    private readonly object _gate = new();
    private readonly List<ConfigurationItem> _items = new();

    public InMemoryStore(params ConfigurationItem[] items) => _items.AddRange(items);

    public void ReplaceForApp(string app, IEnumerable<ConfigurationItem> newItems)
    {
        lock (_gate)
        {
            _items.RemoveAll(i => i.ApplicationName.Equals(app, StringComparison.OrdinalIgnoreCase));
            _items.AddRange(newItems);
        }
    }

    public Task<IReadOnlyList<ConfigurationItem>> GetActiveAsync(string app, CancellationToken ct = default)
    {
        lock (_gate)
        {
            var q = _items.Where(i => i.ApplicationName.Equals(app, StringComparison.OrdinalIgnoreCase) && i.IsActive).ToList();
            return Task.FromResult<IReadOnlyList<ConfigurationItem>>(q);
        }
    }

    public Task<IReadOnlyList<ConfigurationItem>> GetActiveChangedSinceAsync(string app, DateTime sinceUtc, CancellationToken ct = default)
    {
        lock (_gate)
        {
            var q = _items.Where(i =>
                i.ApplicationName.Equals(app, StringComparison.OrdinalIgnoreCase) &&
                i.IsActive &&
                i.UpdatedAtUtc > sinceUtc).ToList();
            return Task.FromResult<IReadOnlyList<ConfigurationItem>>(q);
        }
    }

    public Task<int> UpsertAsync(ConfigurationItem item, CancellationToken ct = default) => Task.FromResult(1);
    public Task<int> DeactivateAsync(string applicationName, string name, CancellationToken ct = default) => Task.FromResult(1);
    public Task<int> ActivateAsync(string applicationName, string name, CancellationToken cancellationToken = default) => Task.FromResult(1);
    public Task<IReadOnlyList<ConfigurationItem>> GetByApplicationAsync(string applicationName, CancellationToken ct = default)
    {
        lock (_gate)
            return Task.FromResult<IReadOnlyList<ConfigurationItem>>(_items.Where(i => i.ApplicationName == applicationName).ToList());
    }
    public Task<IReadOnlyList<ConfigurationItem>> GetAllAsync(CancellationToken ct = default)
    {
        lock (_gate) return Task.FromResult<IReadOnlyList<ConfigurationItem>>(_items.ToList());
    }
    public Task<IReadOnlyList<string>> GetApplicationsAsync(CancellationToken ct = default)
    {
        lock (_gate) return Task.FromResult<IReadOnlyList<string>>(_items.Select(i => i.ApplicationName).Distinct().ToList());
    }
}
