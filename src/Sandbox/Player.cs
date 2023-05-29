using System.Timers;
using ImGuiNET;
using LibExec.MonoGame;
using LibExec.Network;

namespace Sandbox;

[NetworkPlayer]
public sealed class Player : NetworkObject
{
    private readonly Timer _timer = new(100);

    private bool _isPing;
    [Replicate(SkipOwner)] private string _pseudo = string.Empty;
    [Replicate(SkipOwner)] private string _text = string.Empty;
    private int _timerCount;

    public Player()
    {
        _timer.Elapsed += TimerOnElapsed;
    }

    private void TimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        _timerCount++;
        _isPing = !_isPing;
        if (_timerCount >= 10)
        {
            _isPing = false;
            _timer.Stop();
        }
    }

    public override void OnSpawn()
    {
        if (IsOwner)
        {
            _pseudo = Sandbox.Pseudo.Trim();
            SetPseudoServer(_pseudo);
        }
    }

    public void Draw()
    {
        if (string.IsNullOrEmpty(_pseudo)) return;

        ImGuiBegin();

        ImGui.BeginDisabled(!IsOwner);
        ImGui.PushItemWidth(-1);
        if (ImGui.InputTextWithHint("##Text", "Text", ref _text, 100))
        {
            SetTextServer(_text);
        }

        ImGui.PopItemWidth();
        ImGui.EndDisabled();
        Gui.Button("Ping", PingServer, enabled: !IsOwner);
        Gui.Button("Disconnect", () => Owner!.Disconnect(), enabled: NetworkManager.IsServer || IsOwner);

        ImGui.End();
    }

    [Server]
    private void SetTextServer(string text)
    {
        _text = text;
    }

    [Server]
    private void SetPseudoServer(string pseudo)
    {
        _pseudo = pseudo;
    }

    [Server]
    private void PingServer()
    {
        PingClient();
    }

    [Client]
    private void PingClient()
    {
        _isPing = true;
        _timerCount = 0;
        _timer.Start();
    }

    private void ImGuiBegin()
    {
        if (IsOwner && !_isPing)
        {
            ImGui.PushStyleColor(ImGuiCol.Border, 0xff0000ff);
        }

        if (_isPing)
        {
            ImGui.PushStyleColor(ImGuiCol.Border, 0xffffff00);
        }

        ImGui.Begin(_pseudo);
        if (IsOwner || _isPing)
        {
            ImGui.PopStyleColor();
        }
    }
}