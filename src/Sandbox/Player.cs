using ImGuiNET;
using LibExec.Network;

namespace Sandbox;

[NetworkPlayer]
public sealed class Player : NetworkObject
{
    [Replicate(SkipOwner)] private string _text = string.Empty;

    public void Draw()
    {
        ImGui.Begin(Id.ToString());

        ImGui.BeginDisabled(!IsOwner);
        if (ImGui.InputTextWithHint(string.Empty, "Text", ref _text, 100))
        {
            SetTextServer(_text);
        }

        ImGui.EndDisabled();

        ImGui.End();
    }

    [Server]
    private void SetTextServer(string text)
    {
        _text = text;
    }
}