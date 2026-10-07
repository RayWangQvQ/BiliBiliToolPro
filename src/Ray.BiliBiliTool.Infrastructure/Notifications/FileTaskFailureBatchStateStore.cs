using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Ray.BiliBiliTool.Application.Contracts.Notifications;

namespace Ray.BiliBiliTool.Infrastructure.Notifications;

public sealed class FileTaskFailureBatchStateStore(IConfiguration configuration)
    : ITaskFailureBatchStateStore
{
    private readonly string _path = Path.GetFullPath(
        configuration["TaskFailureNotification:StateFile"] ?? "config/task-failure-batch.json"
    );

    public async Task<TaskFailureBatchState?> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
            return null;
        return JsonSerializer.Deserialize<TaskFailureBatchState>(
            await File.ReadAllTextAsync(_path, cancellationToken)
        );
    }

    public async Task WriteAsync(TaskFailureBatchState? state, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temporaryPath = _path + ".tmp";
        try
        {
            await File.WriteAllTextAsync(
                temporaryPath,
                JsonSerializer.Serialize(state),
                cancellationToken
            );
            File.Move(temporaryPath, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
