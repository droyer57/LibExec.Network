using ImGuiNET;
using LibExec.MonoGame;
using LibExec.Network;
using Microsoft.Xna.Framework.Input;
using Num = System.Numerics;

namespace Sandbox;

public sealed class Sandbox : Window
{
    private readonly NetworkManager _networkManager = new();
    private string _address = NetworkManager.LocalAddress;
    private string _port = NetworkManager.DefaultPort.ToString();

    private string _pseudo = string.Empty;

    public Sandbox()
    {
        ImGuiRenderer.AddFont("Sandbox.Fonts.Cousine-Regular.ttf", 16);
        ImGuiRenderer.AddFont("Sandbox.Fonts.DroidSans.ttf", 16);
        ImGuiRenderer.AddFont("Sandbox.Fonts.Karla-Regular.ttf", 16);
        ImGuiRenderer.AddFont("Sandbox.Fonts.ProggyTiny.ttf", 16);
        ImGuiRenderer.AddFont("Sandbox.Fonts.Roboto-Medium.ttf", 16);
    }

    private bool IsServerRunning => _networkManager.ServerManager.IsRunning;
    private bool IsClientRunning => _networkManager.ClientManager.IsRunning;
    private bool IsClientStarted => _networkManager.ClientManager.IsStarted;

    protected override void Start()
    {
    }

    protected override void Update()
    {
        _networkManager.Update();

        if (Input.IsKeyDown(Keys.Escape))
        {
            Exit();
        }
    }

    protected override void Draw()
    {
        ImGui.ShowDemoWindow();

        ImGui.Begin("Settings");
        ImGui.InputTextWithHint(string.Empty, "Pseudo", ref _pseudo, 25);

        ImGui.InputTextWithHint(string.Empty, "Address", ref _address, 25);
        ImGui.SameLine();
        ImGui.InputTextWithHint(string.Empty, "Port", ref _port, 5);

        ImGui.End();

        ImGui.Begin("Network");

        Gui.DrawButton("Start Server", StartServer, IsServerRunning);
        ImGui.SameLine();
        Gui.DrawButton("Start Client", StartClient, IsClientRunning);

        Gui.DrawButton("Disconnect", Disconnect, enabled: IsServerRunning || IsClientRunning);

        ImGui.End();

        foreach (var player in _networkManager.Query<Player>()) // todo: improve this in the lib
        {
            player.Draw();
        }
    }

    private void StartServer()
    {
        _networkManager.StartServer(int.Parse(_port));
    }

    private void StartClient()
    {
        _networkManager.StartClient(_address, int.Parse(_port));
    }

    private void Disconnect()
    {
        _networkManager.StopClient();
        _networkManager.StopServer();
    }
}