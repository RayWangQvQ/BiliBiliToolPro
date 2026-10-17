using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Application.Contracts.Cookies;

namespace Ray.BiliBiliTool.Infrastructure.Cookies;

public sealed class FileCookieCheckStateStore(IConfiguration configuration) : ICookieCheckStateStore
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _path = Path.GetFullPath(
        configuration["CookieCheck:StateFile"] ?? "config/cookie-check-state.json"
    );

    public async Task<CookieCheckState?> ReadAsync(
        string userId,
        CancellationToken cancellationToken
    )
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var states = await ReadAllAsync(cancellationToken);
            return states.GetValueOrDefault(userId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task WriteAsync(
        string userId,
        CookieCheckState state,
        CancellationToken cancellationToken
    )
    {
        await _gate.WaitAsync(cancellationToken);
        var temporaryPath = _path + ".tmp";
        try
        {
            var states = await ReadAllAsync(cancellationToken);
            states[userId] = state;
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            await File.WriteAllTextAsync(
                temporaryPath,
                JsonSerializer.Serialize(states),
                cancellationToken
            );
            File.Move(temporaryPath, _path, overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            finally
            {
                _gate.Release();
            }
        }
    }

    private async Task<Dictionary<string, CookieCheckState>> ReadAllAsync(
        CancellationToken cancellationToken
    )
    {
        if (!File.Exists(_path))
            return [];
        var json = await File.ReadAllTextAsync(_path, cancellationToken);
        return JsonSerializer.Deserialize<Dictionary<string, CookieCheckState>>(json) ?? [];
    }
}
