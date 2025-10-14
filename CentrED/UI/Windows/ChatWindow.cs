using System.Numerics;
using CentrED.Client;
using Hexa.NET.ImGui;

namespace CentrED.UI.Windows;

public class ChatWindow : Window
{
    private record struct ChatMessage(string User, string Message, DateTime Time);
    public ChatWindow()
    {
        Application.CEDClient.ChatMessage += (user, message) =>
        {
            ChatMessages.Add(new ChatMessage(user, message, DateTime.Now));
            _scrollToBottom = true;
            if (!Show)
            {
                _unreadMessages = true;
            }
        };
        Application.CEDClient.Disconnected += () => ChatMessages.Clear();
        Application.CEDClient.ClientConnected += user => ChatMessages.Add(new ChatMessage(user, LangManager.Get("CONNECTED"), DateTime.Now));
        Application.CEDClient.ClientDisconnected += user => ChatMessages.Add(new ChatMessage(user, LangManager.Get("DISCONNECTED"), DateTime.Now));
    }
    
    public override string Name => LangManager.Get("CHAT_WINDOW") + (_unreadMessages ? $"({LangManager.Get("NEW_MESSAGES")})" : "") + "###Chat";

    public override void OnShow()
    {
        _unreadMessages = false;
    }

    private bool _unreadMessages;
    private List<ChatMessage> ChatMessages = new();
    private bool _scrollToBottom = true;
    
    private string _chatInput = "";
    protected override void InternalDraw()
    {
        var clients = Application.CEDClient.Clients;
        
        var maxNameSize = clients.Count == 0 ? 0 : Application.CEDClient.Clients.Max(s => ImGui.CalcTextSize(s).X);
        if(ImGui.BeginChild("Client List", new Vector2(Math.Max(150, maxNameSize), 0), ImGuiChildFlags.Borders))
        {
            ImGui.Text(LangManager.Get("USERS"));
            ImGui.Separator();
            foreach (var client in clients)
            {
                ImGui.Selectable(client);
                if (client == Application.CEDClient.Username)
                {
                    ImGui.SameLine();
                    ImGui.TextDisabled("(" + LangManager.Get("YOU") + ")");
                    continue;
                }
                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    Application.CEDClient.Send(new GotoClientPosPacket(client));
                }
                if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                {
                    ImGui.OpenPopup($"{client}Popup");
                }
                if (ImGui.BeginPopup($"{client}Popup"))
                {
                    if (ImGui.Button($"{LangManager.Get("GO_TO")}##{client}"))
                    {
                        Application.CEDClient.Send(new GotoClientPosPacket(client));
                    }
                    ImGui.EndPopup();
                }
            }
        }
        ImGui.EndChild();
        ImGui.SameLine();
        
        ImGui.BeginGroup();
        ImGui.Text(LangManager.Get("CHAT"));
        ImGui.Separator();
        if (!Application.CEDClient.Running)
        {
            ImGui.TextDisabled(LangManager.Get("NOT_CONNECTED"));    
        }
        else
        {
            var sendButtonSize = ImGui.CalcTextSize(LangManager.Get("SEND") + "  ") + ImGui.GetStyle().FramePadding * 2;
            var inputPosY = ImGui.GetWindowSize().Y - ImGui.GetStyle().WindowPadding.Y - sendButtonSize.Y;

            var availSpace = ImGui.GetContentRegionAvail();
            var childSpace = availSpace with { Y = availSpace.Y - sendButtonSize.Y - ImGui.GetStyle().WindowPadding.Y};
            if(ImGui.BeginChild("Chat", childSpace))
            {
                foreach (var message in ChatMessages)
                {
                    ImGui.Text($"{message.Time:T}[{message.User}]: {message.Message}");
                }
                ImGui.Dummy(Vector2.One);
                if (_scrollToBottom)
                {
                    ImGui.SetScrollHereY(1.0f);
                    _scrollToBottom = false;
                }
            }
            ImGui.EndChild();

            ImGui.SetCursorPosY(inputPosY);
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - sendButtonSize.X - ImGui.GetStyle().ItemSpacing.X);
            ImGui.InputText("##ChatInput", ref _chatInput, 256);
            ImGui.SameLine();
            if (ImGui.Button(LangManager.Get("SEND"), sendButtonSize) || ImGui.IsKeyPressed(ImGuiKey.Enter))
            {
                Application.CEDClient.Send(new ChatMessagePacket(_chatInput));
                _chatInput = "";
            }
        }
        ImGui.EndGroup();
    }
}