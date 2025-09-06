using System.Collections.Immutable;
using System.Diagnostics;

namespace SecilStoreCodeCase;

public sealed class ConfigurationReader : IDisposable
{
    private readonly string _applicationName;
    private readonly IConfigStore _store;
    private readonly TimeSpan _interval;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _refreshLoop;
    public DateTime LastRefreshUtc => _lastSuccessUtc;

    // Bellekte tutulan snapshot 
    private volatile ImmutableDictionary<string, (string Raw, ConfigItemType Type)> _cache
        = ImmutableDictionary.Create<string, (string, ConfigItemType)>(StringComparer.OrdinalIgnoreCase);

    // Artımlı sorgu için en son başarılı refresh anı
    private DateTime _lastSuccessUtc = DateTime.MinValue;

    public ConfigurationReader(string applicationName, string connectionString, int refreshTimerIntervalInMs)
        : this(applicationName,
               new SqlConfigStore(connectionString),
               TimeSpan.FromMilliseconds(Math.Max(refreshTimerIntervalInMs, 5000))) // alt sınır 5 sn
    { }

    
    public ConfigurationReader(string applicationName, IConfigStore store, TimeSpan interval)
    {
        _applicationName = applicationName ?? throw new ArgumentNullException(nameof(applicationName));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _interval = interval <= TimeSpan.Zero ? TimeSpan.FromSeconds(30) : interval;

        // İlk yükleme: başarısız olursa boş snapshot ile ayağa kalk
        try
        {
            var initial = _store.GetActiveAsync(_applicationName, _cts.Token).GetAwaiter().GetResult();
            _cache = BuildSnapshot(initial);
            _lastSuccessUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConfigurationReader] Initial load failed: {ex}");
        }

        _refreshLoop = Task.Run(RefreshLoopAsync);
    }

    /// <summary> Tip güvenli okuma. Key yoksa KeyNotFoundException atar. </summary>
    public T GetValue<T>(string key)
    {
        if (key is null) throw new ArgumentNullException(nameof(key));

        var snap = _cache;
        if (!snap.TryGetValue(key, out var entry))
            throw new KeyNotFoundException($"Key not found for application '{_applicationName}': {key}");

        return (T)ConvertTo(entry.Raw, entry.Type, typeof(T));
    }

    private static object ConvertTo(string raw, ConfigItemType storedType, Type targetType)
    {
        raw = raw?.Trim() ?? string.Empty;

        // string isteniyorsa direkt dön
        if (targetType == typeof(string)) return raw;

        // bool
        if (storedType == ConfigItemType.Bool)
        {
            if (raw == "1") return Change(true, targetType);
            if (raw == "0") return Change(false, targetType);
            if (bool.TryParse(raw, out var b)) return Change(b, targetType);
        }

        // int
        if (storedType == ConfigItemType.Int &&
            int.TryParse(raw, System.Globalization.NumberStyles.Integer,
                         System.Globalization.CultureInfo.InvariantCulture, out var i))
            return Change(i, targetType);

        // double
        if (storedType == ConfigItemType.Double &&
            double.TryParse(raw, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var d))
            return Change(d, targetType);

        // geriye kalan — genel amaçlı dönüşüm (ör. string->int gibi)
        return System.Convert.ChangeType(raw, targetType, System.Globalization.CultureInfo.InvariantCulture)!;

        static object Change<TFrom>(TFrom val, Type to) =>
            to == typeof(TFrom) ? val! :
            System.Convert.ChangeType(val, to, System.Globalization.CultureInfo.InvariantCulture)!;
    }

    private static ImmutableDictionary<string, (string Raw, ConfigItemType Type)>
        BuildSnapshot(IEnumerable<ConfigurationItem> items)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, (string, ConfigItemType)>(StringComparer.OrdinalIgnoreCase);
        foreach (var it in items)
        {
            if (!it.IsActive) continue;
            builder[it.Name] = (it.Value, it.Type);
        }
        return builder.ToImmutable();
    }

    private async Task RefreshLoopAsync()
    {
        var timer = new PeriodicTimer(_interval);
        try
        {
            while (await timer.WaitForNextTickAsync(_cts.Token).ConfigureAwait(false))
            {
                try
                {
                    IReadOnlyList<ConfigurationItem> delta;
                    if (_lastSuccessUtc == DateTime.MinValue)
                        delta = await _store.GetActiveAsync(_applicationName, _cts.Token).ConfigureAwait(false);
                    else
                        delta = await _store.GetActiveChangedSinceAsync(_applicationName, _lastSuccessUtc, _cts.Token).ConfigureAwait(false);

                    if (delta.Count > 0 || _lastSuccessUtc == DateTime.MinValue)
                    {
                        // Mevcut snapshot'u kopyalayıp sadece değişenleri uygula (read-copy-update)
                        var builder = _cache.ToBuilder();
                        foreach (var ch in delta)
                        {
                            if (ch.IsActive)
                                builder[ch.Name] = (ch.Value, ch.Type);
                            else
                                builder.Remove(ch.Name);
                        }
                        _cache = builder.ToImmutable();
                        _lastSuccessUtc = DateTime.UtcNow;
                    }
                }
                catch (OperationCanceledException) { /* kapatılıyor */ }
                catch (Exception ex)
                {
                    // Storage down: mevcut snapshot ile çalışmaya devam (fail-open)
                    Debug.WriteLine($"[ConfigurationReader] Refresh failed: {ex}");
                }
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            timer.Dispose();
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _refreshLoop.Wait(TimeSpan.FromSeconds(2)); } catch { /* ignore */ }
        _cts.Dispose();
    }
}
