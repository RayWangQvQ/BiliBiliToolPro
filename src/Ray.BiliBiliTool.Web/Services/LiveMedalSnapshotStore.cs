using System.Text.Json;
using System.Text.RegularExpressions;

namespace Ray.BiliBiliTool.Web.Services;

public interface ILiveMedalSnapshotStore
{
    Task<LiveMedalSnapshot?> ReadAsync(string accountKey, CancellationToken token = default);
    Task WriteAsync(
        string accountKey,
        LiveMedalSnapshot snapshot,
        CancellationToken token = default
    );
}

public sealed class FileLiveMedalSnapshotStore(
    string directory,
    ILogger<FileLiveMedalSnapshotStore> logger
) : ILiveMedalSnapshotStore
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<LiveMedalSnapshot?> ReadAsync(
        string accountKey,
        CancellationToken token = default
    )
    {
        var path = CachePath(accountKey);
        await _gate.WaitAsync(token);
        try
        {
            if (!File.Exists(path))
                return null;
            await using var stream = File.OpenRead(path);
            var snapshot = await JsonSerializer.DeserializeAsync<LiveMedalSnapshot>(
                stream,
                cancellationToken: token
            );
            return
                snapshot?.Medals is not null
                && snapshot.Error is null
                && snapshot.UpdatedAt > DateTimeOffset.UtcNow.AddDays(-7)
                && snapshot.Medals.All(card => card is not null && card.Tasks is not null)
                ? snapshot
                : null;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            logger.LogWarning(
                "Medal snapshot cache could not be read: {ErrorType}",
                ex.GetType().Name
            );
            return null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task WriteAsync(
        string accountKey,
        LiveMedalSnapshot snapshot,
        CancellationToken token = default
    )
    {
        if (snapshot.Error is not null)
            return;
        var path = CachePath(accountKey);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        await _gate.WaitAsync(token);
        try
        {
            Directory.CreateDirectory(directory);
            await using (var stream = File.Create(temporary))
                await JsonSerializer.SerializeAsync(stream, snapshot, cancellationToken: token);
            File.Move(temporary, path, overwrite: true);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(
                "Medal snapshot cache could not be saved: {ErrorType}",
                ex.GetType().Name
            );
        }
        finally
        {
            try
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogDebug(
                    "Medal snapshot temporary file could not be removed: {ErrorType}",
                    ex.GetType().Name
                );
            }
            _gate.Release();
        }
    }

    private string CachePath(string accountKey)
    {
        if (!Regex.IsMatch(accountKey, "^[A-F0-9]{64}$", RegexOptions.CultureInvariant))
            throw new ArgumentException("Invalid medal cache key", nameof(accountKey));
        return Path.Combine(directory, accountKey + ".json");
    }
}
