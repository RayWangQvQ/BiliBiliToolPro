using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace Ray.BiliBiliTool.Web.Components.Comps;

public enum UnsavedChangesDecision
{
    Save,
    Discard,
}

public partial class UnsavedChangesGuard : IDisposable
{
    [Inject]
    private IDialogService Dialogs { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    [Parameter]
    public bool HasChanges { get; set; }

    [Parameter]
    public bool Busy { get; set; }

    [Parameter]
    public bool CanSave { get; set; } = true;

    [Parameter]
    public Func<Task<bool>> SaveBeforeLeaving { get; set; } = null!;

    private Task<bool>? _pendingDecision;
    private IDialogReference? _dialog;
    private readonly CancellationTokenSource _lifetime = new();
    private bool _disposed;

    private async Task BeforeNavigationAsync(LocationChangingContext context)
    {
        if (_disposed || context.TargetLocation == Navigation.Uri || (!HasChanges && !Busy))
            return;
        if (Busy)
        {
            context.PreventNavigation();
            Snackbar.Add("正在保存，请稍候再切换页面", Severity.Info);
            return;
        }
        // Share one decision across repeated navigation attempts while the dialog is open.
        var decision = _pendingDecision ??= RequestDecisionAsync();
        try
        {
            var leave = await decision;
            if (!leave && !context.CancellationToken.IsCancellationRequested)
                context.PreventNavigation();
        }
        finally
        {
            if (ReferenceEquals(_pendingDecision, decision) && decision.IsCompleted)
                _pendingDecision = null;
        }
    }

    private async Task<bool> RequestDecisionAsync()
    {
        try
        {
            _dialog = await Dialogs.ShowAsync<UnsavedChangesDialog>(
                "是否保存修改？",
                new DialogParameters<UnsavedChangesDialog> { { x => x.CanSave, CanSave } },
                new DialogOptions
                {
                    MaxWidth = MaxWidth.Small,
                    FullWidth = true,
                    BackdropClick = false,
                    CloseOnEscapeKey = true,
                }
            );
            if (_disposed)
            {
                _dialog.Close(DialogResult.Cancel());
                return false;
            }
            var result = await _dialog.Result.WaitAsync(_lifetime.Token);
            if (_disposed || result is null || result.Canceled)
                return false;
            if (result.Data is UnsavedChangesDecision.Discard)
                return true;
            if (result.Data is not UnsavedChangesDecision.Save || !CanSave)
                return false;
            if (EditContext?.Validate() == false)
            {
                Snackbar.Add("请检查配置中的填写提示，修改后再保存", Severity.Warning);
                return false;
            }
            var saved = await SaveBeforeLeaving();
            if (!saved)
                Snackbar.Add("保存未完成，已留在当前页面，请检查保存提示", Severity.Warning);
            return saved;
        }
        catch
        {
            if (!_disposed)
                Snackbar.Add("保存未完成，修改已保留，请重试", Severity.Error);
            return false;
        }
        finally
        {
            _dialog = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        var dialog = _dialog;
        _lifetime.Cancel();
        dialog?.Close(DialogResult.Cancel());
        _lifetime.Dispose();
    }
}
