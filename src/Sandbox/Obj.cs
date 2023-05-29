using System;
using System.Numerics;
using ImGuiNET;
using LibExec.MonoGame;
using LibExec.Network;

namespace Sandbox;

public sealed class Obj : NetworkObject
{
    [Replicate] private int _value;

    public Obj()
    {
        _value = new Random().Next(10) + 1;
    }

    public void Draw()
    {
        ImGui.BeginGroup();

        ImGui.BeginChild(Id.ToString(), new Vector2(33, 65), true);
        Gui.Button(_value.ToString(), IncrementValueServer);
        Gui.Button("X", DestroyServer);
        ImGui.EndChild();

        ImGui.EndGroup();
    }

    [Server]
    private void IncrementValueServer()
    {
        _value++;
    }

    [Server]
    private void DestroyServer()
    {
        Destroy();
    }
}