using ImGuiNET;
using LibExec.MonoGame;
using LibExec.Network;

namespace Sandbox;

public sealed class GameManager : NetworkObject
{
    [Replicate] private Weapon? _weapon;

    public void Draw()
    {
        ImGui.Begin("Game Manager");

        Gui.Button("Add object", AddObjectServer);

        foreach (var item in NetworkManager.Query<Obj>())
        {
            item.Draw();
            ImGui.SameLine();
        }

        ImGui.NewLine();
        Gui.Button("Spawn weapon", SpawnWeaponServer);

        if (_weapon?.IsValid == true)
        {
            _weapon.Draw();
        }

        ImGui.End();
    }

    [Server]
    private void SpawnWeaponServer()
    {
        _weapon = new Weapon();
        _weapon.Spawn();
    }

    [Server]
    private void AddObjectServer()
    {
        var obj = new Obj();
        obj.Spawn();
    }
}