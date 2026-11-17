using CentrED.IO;
using Hexa.NET.ImGui;
using static CentrED.CentrED;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace CentrED.UI;

public class WorkspaceMenu
{
    private static NamedConfigManager Workspaces => App.Workspace;

    private string _nameBuf = "";
    private bool _requestOpenNamePopup;
    private bool _namePopupCreatesNew;
    private bool _requestOpenDeleteConfirm;
    private bool _requestOpenResetConfirm;
    private string _pendingDeleteName = "";

    private Vector2 _menuAnchorMax;
    private bool _hasMenuAnchor;

    private sealed class Toast
    {
        public string Text = "";
        public bool Success;
        public double StartTime;
    }

    private const double TOAST_DURATION_SECONDS = 10.0;
    private readonly List<Toast> _toastQueue = new();

    private void EnqueueToast(bool success, string text)
    {
        _toastQueue.Add(new Toast { Text = text, Success = success, StartTime = ImGui.GetTime() });
    }

    private void TryRun(Action action, string success, string failure)
    {
        try
        {
            action();
            EnqueueToast(true, success);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            EnqueueToast(false, failure);
        }
    }

    public void DrawTopBarEntry()
    {
        var open = ImGui.BeginMenu("Workspace");

        _menuAnchorMax = ImGui.GetItemRectMax();
        _hasMenuAnchor = true;

        ImGui.SameLine(0, 0);
        ImGui.TextUnformatted(":");
        ImGui.SameLine(0, 6f);
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.0f, 1.0f, 0.0f, 1.0f));
        ImGui.TextUnformatted(Workspaces.ActiveName.Value);
        ImGui.PopStyleColor();

        if (open)
        {
            DrawMenu();
            ImGui.EndMenu();
        }
    }

    private void DrawMenu()
    {
        foreach (var name in Workspaces.Names)
        {
            if (ImGui.MenuItem(name, "", name == Workspaces.ActiveName.Value) && name != Workspaces.ActiveName.Value)
            {
                TryRun
                (
                    () => Workspaces.ChangeActive(name),
                    $"Workspace: {name} loaded.",
                    $"Workspace: {name} failed to load."
                );
            }
        }

        ImGui.Separator();

        if (ImGui.MenuItem("New..."))
        {
            _nameBuf = "";
            _namePopupCreatesNew = true;
            _requestOpenNamePopup = true;
        }

        if (ImGui.MenuItem("Duplicate..."))
        {
            _nameBuf = "";
            _namePopupCreatesNew = false;
            _requestOpenNamePopup = true;
        }

        if (ImGui.MenuItem("Reset"))
        {
            _requestOpenResetConfirm = true;
        }

        if (Workspaces.Names.Length > 1 && ImGui.BeginMenu("Delete"))
        {
            foreach (var name in Workspaces.Names)
            {
                if (ImGui.MenuItem(name))
                {
                    _pendingDeleteName = name;
                    _requestOpenDeleteConfirm = true;
                }
            }
            ImGui.EndMenu();
        }
    }

    /// <summary>
    /// Must be called once per frame, outside the menu scope, for modal popups to open reliably.
    /// </summary>
    public void DrawPopups()
    {
        if (_requestOpenNamePopup)
        {
            ImGui.OpenPopup("WorkspaceName");
            _requestOpenNamePopup = false;
        }

        if (_requestOpenDeleteConfirm)
        {
            ImGui.OpenPopup("ConfirmDeleteWorkspace");
            _requestOpenDeleteConfirm = false;
        }

        if (_requestOpenResetConfirm)
        {
            ImGui.OpenPopup("ConfirmResetWorkspace");
            _requestOpenResetConfirm = false;
        }

        if (ImGui.BeginPopupModal("WorkspaceName", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
        {
            ImGui.Text("Workspace name:");
            ImGui.SameLine();
            ImGui.InputText("##WorkspaceName", ref _nameBuf, 64);

            var name = NamedConfigManager.Sanitize(_nameBuf);
            var existed = Workspaces.Exists(name);
            if (existed)
            {
                ImGui.TextColored(ImGuiColor.Red, "Workspace already exists");
            }

            ImGui.BeginDisabled(string.IsNullOrWhiteSpace(name) || existed);
            if (ImGui.Button(_namePopupCreatesNew ? "Create" : "Save"))
            {
                TryRun
                (
                    _namePopupCreatesNew ? () => Workspaces.Create(name) : () => Workspaces.Duplicate(name),
                    $"Workspace: {name} has been created.",
                    $"Workspace: {name} could not be created, try again."
                );
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndDisabled();

            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        if (ImGui.BeginPopupModal("ConfirmDeleteWorkspace", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
        {
            var name = _pendingDeleteName;
            ImGui.Text($"Are you sure you wish to DELETE workspace '{name}'?");
            ImGui.Separator();

            if (ImGui.Button("Yes"))
            {
                TryRun
                (
                    () => Workspaces.Remove(name),
                    $"Workspace: {name} has been deleted.",
                    $"Workspace: {name} could not be deleted, try again."
                );
                _pendingDeleteName = "";
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button("No"))
            {
                _pendingDeleteName = "";
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        if (ImGui.BeginPopupModal("ConfirmResetWorkspace", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
        {
            var name = Workspaces.ActiveName.Value;
            ImGui.Text($"Are you sure you wish to RESET workspace '{name}' to default values?");
            ImGui.Separator();

            if (ImGui.Button("Yes"))
            {
                TryRun
                (
                    Workspaces.Reset,
                    $"Workspace: {name} has been reset.",
                    $"Workspace: {name} could not be reset, try again."
                );
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button("No"))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        DrawToasts();
    }

    private void DrawToasts()
    {
        var now = ImGui.GetTime();
        _toastQueue.RemoveAll(t => now - t.StartTime >= TOAST_DURATION_SECONDS);
        if (_toastQueue.Count == 0)
            return;

        var vp = ImGui.GetMainViewport();
        var padding = new Vector2(12, 12);
        var yOffset = 0f;

        for (var i = _toastQueue.Count - 1; i >= 0; i--)
        {
            var t = _toastQueue[i];
            var remaining = TOAST_DURATION_SECONDS - (now - t.StartTime);

            var alpha = 1f;
            const double fadeWindow = 2.0;
            if (remaining < fadeWindow)
                alpha = (float)(remaining / fadeWindow);

            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, alpha);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 6f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 8));

            var bg = t.Success ? new Vector4(0.12f, 0.35f, 0.16f, 0.92f) : new Vector4(0.42f, 0.12f, 0.12f, 0.92f);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, bg);
            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(1, 1, 1, 0.15f));

            var textSize = ImGui.CalcTextSize(t.Text);
            var width = textSize.X + 16;
            var x = _hasMenuAnchor ? _menuAnchorMax.X : vp.WorkPos.X + vp.WorkSize.X - textSize.X - padding.X;
            var y = (_hasMenuAnchor ? _menuAnchorMax.Y + 2f : vp.WorkPos.Y + padding.Y) + yOffset;
            x = Math.Min(x, vp.WorkPos.X + vp.WorkSize.X - width - padding.X);

            ImGui.SetNextWindowPos(new Vector2(x, y), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(width, 0), ImGuiCond.Always);
            ImGui.Begin
            (
                $"##WorkspaceToast{i}",
                ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoNav |
                ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoInputs
            );
            ImGui.TextUnformatted(t.Text);
            ImGui.End();

            yOffset += textSize.Y + 10;

            ImGui.PopStyleColor(2);
            ImGui.PopStyleVar(3);
        }
    }
}
