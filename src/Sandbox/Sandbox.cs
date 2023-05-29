using System;
using ImGuiNET;
using LibExec.MonoGame;
using LibExec.Network;
using Microsoft.Xna.Framework.Input;
using Num = System.Numerics;

namespace Sandbox;

public sealed class Sandbox : Window
{
    private static Sandbox _instance = null!;

    private readonly NetworkManager _networkManager = new();
    private string _address = NetworkManager.LocalAddress;
    private GameManager? _gameManager;
    private string _port = NetworkManager.DefaultPort.ToString();

    private string _pseudo = string.Empty;

    public Sandbox()
    {
        _instance = this;

        ImGuiRenderer.AddFont("Sandbox.Fonts.Cousine-Regular.ttf", 16);
        ImGuiRenderer.AddFont("Sandbox.Fonts.DroidSans.ttf", 16);
        ImGuiRenderer.AddFont("Sandbox.Fonts.Karla-Regular.ttf", 16);
        ImGuiRenderer.AddFont("Sandbox.Fonts.ProggyTiny.ttf", 16);
        ImGuiRenderer.AddFont("Sandbox.Fonts.Roboto-Medium.ttf", 16);

        _networkManager.ServerManager.ConnectionStateChangedEvent += OnServerConnectionStateChanged;
        _networkManager.NetworkObjectEvent += OnNetworkObject;

        var args = Environment.GetCommandLineArgs();
        if (args.Length == 2)
        {
            _pseudo = args[1];
        }
    }

    public static string Pseudo => _instance._pseudo;

    private bool IsServerRunning => _networkManager.ServerManager.IsRunning;
    private bool IsClientRunning => _networkManager.ClientManager.IsRunning;

    private void OnServerConnectionStateChanged(ConnectionState state)
    {
        if (state == ConnectionState.Started)
        {
            _gameManager = new GameManager();
            _gameManager.Spawn();
        }
    }

    private void OnNetworkObject(NetworkObject networkObject, NetworkObjectEvent networkObjectEvent)
    {
        if (networkObjectEvent == NetworkObjectEvent.Spawned && networkObject is GameManager gameManager)
        {
            _gameManager = gameManager;
        }
    }

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

        DrawSettingsWindow();
        DrawNetworkWindow();

        if (_gameManager?.IsValid == true)
            _gameManager.Draw();

        foreach (var player in _networkManager.Query<Player>()) // todo: improve this in the lib
        {
            player.Draw();
        }
    }

    private void DrawSettingsWindow()
    {
        ImGui.BeginDisabled(IsServerRunning || IsClientRunning);

        ImGui.Begin("Settings");

        ImGui.PushItemWidth(-1);
        ImGui.InputTextWithHint("##Pseudo", "Pseudo", ref _pseudo, 25);
        ImGui.PopItemWidth();

        ImGui.InputTextWithHint("##Address", "Address", ref _address, 25);

        ImGui.SameLine();
        ImGui.PushItemWidth(-1);
        ImGui.InputTextWithHint("##Port", "Port", ref _port, 5);
        ImGui.PopItemWidth();

        ImGui.End();

        ImGui.EndDisabled();
    }

    private void DrawNetworkWindow()
    {
        ImGui.Begin("Network");

        Gui.Button("Start Server", StartServer, IsServerRunning);
        ImGui.SameLine();
        Gui.Button("Start Client", StartClient, IsClientRunning || string.IsNullOrWhiteSpace(Pseudo));

        Gui.Button("Disconnect", Disconnect, enabled: IsServerRunning || IsClientRunning);

        ImGui.End();
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