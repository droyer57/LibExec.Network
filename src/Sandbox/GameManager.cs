using ImGuiNET;
using LibExec.MonoGame;
using LibExec.Network;

namespace Sandbox;

public sealed class GameManager : NetworkObject
{
    public void Draw()
    {
        ImGui.Begin("Game Manager");

        Gui.Button("Add object", AddObjectServer);

        foreach (var item in NetworkManager.Query<Obj>())
        {
            item.Draw();
            ImGui.SameLine();
        }

        ImGui.End();
    }

    [Server]
    private void AddObjectServer()
    {
        var obj = new Obj();
        obj.Spawn();
    }
}