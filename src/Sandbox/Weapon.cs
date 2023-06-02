using System;
using ImGuiNET;
using LibExec.Network;

namespace Sandbox;

public sealed class Weapon : NetworkObject
{
    [Replicate] private int _damage;

    public Weapon()
    {
        _damage = new Random().Next(0, 100) + 1;
    }

    public void Draw()
    {
        ImGui.Text($"Weapon: {_damage}");
    }
}