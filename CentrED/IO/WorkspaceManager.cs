using Hexa.NET.ImGui;
using static CentrED.CentrED;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace CentrED.IO;

public class WorkspaceManager
{
    private const string WorkspacesDir = "Workspaces";
    private const string DefaultName = "Default";

    public static string WorkspacePath(string name) => Path.Combine(WorkspacesDir, $"{name}.cfg");

    public readonly ConfigValue<string> ActiveName = App.Config.Init("workspace.active", DefaultName);
    public readonly Config Active;
    public event Action? Changed;

    private readonly List<string> _Names = [];
    public string[] Names { get; private set; }

    public WorkspaceManager()
    {
        if (!Directory.Exists(WorkspacesDir))
        {
            Directory.CreateDirectory(WorkspacesDir);
        }
        foreach (var workspacePath in Directory.EnumerateFiles(WorkspacesDir, "*.cfg", SearchOption.TopDirectoryOnly))
        {
            _Names.Add(Path.GetFileNameWithoutExtension(workspacePath));
        }
        if (!Exists(DefaultName))
        {
            _Names.Add(DefaultName);
        }
        if (!Exists(ActiveName.Value))
        {
            ActiveName.Value = DefaultName;
        }
        UpdateNames();
        Active = new Config(WorkspacePath(ActiveName.Value));
    }

    public bool Exists(string name) => _Names.Contains(name, StringComparer.OrdinalIgnoreCase);

    public void ChangeActive(string name)
    {
        Active.Save();
        ActiveName.Value = name;
        Active.Load(WorkspacePath(name));
        Changed?.Invoke();
    }

    public void SaveAs(string name)
    {
        name = _Names.FirstOrDefault(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)) ?? name;
        Active.Save();
        if (name == ActiveName.Value)
            return;

        File.Copy(Active.FilePath, WorkspacePath(name), true);
        if (!Exists(name))
        {
            _Names.Add(name);
            UpdateNames();
        }
        ActiveName.Value = name;
        Active.Load(WorkspacePath(name));
    }

    public void Remove(string name)
    {
        if (string.Equals(name, DefaultName, StringComparison.OrdinalIgnoreCase))
            return;

        File.Delete(WorkspacePath(name));
        _Names.Remove(name);
        UpdateNames();
        if (name == ActiveName.Value)
        {
            ActiveName.Value = DefaultName;
            Active.Load(WorkspacePath(DefaultName));
            Changed?.Invoke();
        }
    }

    public void Save()
    {
        Active.Save();
    }

    private void UpdateNames()
    {
        Names = _Names.Order(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim();
    }

    // ---- UI ----
    private string _saveNameBuf = "";
    private bool _requestOpenSavePopup;
    private bool _requestOpenDeleteConfirm;
    private string _pendingDeleteName = "";

    private Vector2 _menuAnchorMax;
    private bool _hasMenuAnchor;

    private sealed class Toast
    {
        public string Text = "";
        public bool Success;
        public double StartTime;
    }

    private const double ToastDurationSeconds = 10.0;
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
        ImGui.TextUnformatted(ActiveName.Value);
        ImGui.PopStyleColor();

        if (open)
        {
            DrawMenu();
            ImGui.EndMenu();
        }
    }

    private void DrawMenu()
    {
        foreach (var name in Names)
        {
            if (ImGui.MenuItem(name, "", name == ActiveName.Value) && name != ActiveName.Value)
            {
                TryRun
                (
                    () => ChangeActive(name),
                    $"Workspace: {name} loaded.",
                    $"Workspace: {name} failed to load."
                );
            }
        }

        ImGui.Separator();

        if (ImGui.MenuItem("Save As..."))
        {
            _saveNameBuf = ActiveName.Value;
            _requestOpenSavePopup = true;
        }

        if (ImGui.BeginMenu("Delete"))
        {
            foreach (var name in Names)
            {
                if (string.Equals(name, DefaultName, StringComparison.OrdinalIgnoreCase))
                    continue;

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
        if (_requestOpenSavePopup)
        {
            ImGui.OpenPopup("SaveWorkspace");
            _requestOpenSavePopup = false;
        }

        if (_requestOpenDeleteConfirm)
        {
            ImGui.OpenPopup("ConfirmDeleteWorkspace");
            _requestOpenDeleteConfirm = false;
        }

        if (ImGui.BeginPopupModal("SaveWorkspace", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
        {
            ImGui.Text("Workspace name:");
            ImGui.SameLine();
            ImGui.InputText("##WorkspaceName", ref _saveNameBuf, 64);

            var name = Sanitize(_saveNameBuf);
            ImGui.BeginDisabled(string.IsNullOrWhiteSpace(name));
            if (ImGui.Button("Save"))
            {
                var existed = Exists(name);
                TryRun
                (
                    () => SaveAs(name),
                    existed ? $"Workspace: {name} has been updated." : $"Workspace: {name} has been created.",
                    $"Workspace: {name} could not be saved, try again."
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
                    () => Remove(name),
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

        DrawToasts();
    }

    private void DrawToasts()
    {
        var now = ImGui.GetTime();
        _toastQueue.RemoveAll(t => now - t.StartTime >= ToastDurationSeconds);
        if (_toastQueue.Count == 0)
            return;

        var vp = ImGui.GetMainViewport();
        var padding = new Vector2(12, 12);
        var yOffset = 0f;

        for (var i = _toastQueue.Count - 1; i >= 0; i--)
        {
            var t = _toastQueue[i];
            var remaining = ToastDurationSeconds - (now - t.StartTime);

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
