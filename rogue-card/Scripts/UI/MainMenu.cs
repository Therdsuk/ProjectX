using Godot;
using Steamworks;

/// <summary>
/// A simple Main Menu scene that lets you Host a Steam game and see the Lobby list.
/// </summary>
public partial class MainMenu : Control
{
    [Export] public Button HostButton { get; set; }
    [Export] public Button LeaveButton { get; set; }
    [Export] public Button ReadyButton { get; set; }
    [Export] public Button StartGameButton { get; set; }
    [Export] public Label LobbyLabel { get; set; }
    [Export] public ItemList PlayerList { get; set; }
    
    private bool _isReady = false;

    public override void _Ready()
    {
        // 1. Connect UI Signals
        if (HostButton != null) HostButton.Pressed += OnHostPressed;
        if (LeaveButton != null) LeaveButton.Pressed += OnLeavePressed;
        if (ReadyButton != null) ReadyButton.Pressed += OnReadyPressed;
        if (StartGameButton != null) StartGameButton.Pressed += OnStartGamePressed;

        // 2. Subscribe to SteamManager Callbacks
        SteamManager.OnLobbyCreatedEvent += OnLobbyCreated;
        SteamManager.OnLobbyJoinedEvent += OnLobbyJoined;
        SteamManager.OnPlayerJoinedEvent += OnPlayerJoined;
        SteamManager.OnPlayerLeftEvent += OnPlayerLeft;
        SteamManager.OnLobbyDataUpdatedEvent += OnLobbyDataUpdated;
    }

    private void OnHostPressed()
    {
        HostButton.Disabled = true;
        LobbyLabel.Text = "Status: Creating Lobby...";
        SteamManager.Instance.HostLobby();
    }

    private void OnLeavePressed()
    {
        SteamManager.Instance.CurrentLobby?.Leave();
        HostButton.Disabled = false;
        LeaveButton.Disabled = true;
        ReadyButton.Disabled = true;
        StartGameButton.Visible = false;
        LobbyLabel.Text = "Status: Left Lobby.";
        PlayerList.Clear();
        _isReady = false;
        ReadyButton.Text = "Ready Up";
    }

    private void OnReadyPressed()
    {
        _isReady = !_isReady;
        ReadyButton.Text = _isReady ? "Unready" : "Ready Up";
        SteamManager.Instance.ToggleReady(_isReady);
    }

    private void OnStartGamePressed()
    {
        SteamManager.Instance.StartGame();
    }

    private void OnLobbyCreated(Steamworks.Data.Lobby lobby)
    {
        LobbyLabel.Text = $"Status: Hosting Lobby ({lobby.Id})";
        LeaveButton.Disabled = false;
        ReadyButton.Disabled = false;
        UpdatePlayerList();
    }

    private void OnLobbyJoined(Steamworks.Data.Lobby lobby)
    {
        HostButton.Disabled = true;
        LeaveButton.Disabled = false;
        ReadyButton.Disabled = false;
        LobbyLabel.Text = $"Status: Joined '{lobby.Owner.Name}'s Lobby";
        UpdatePlayerList();
    }

    private void OnPlayerJoined(Steamworks.Friend friend)
    {
        UpdatePlayerList();
    }

    private void OnPlayerLeft(Steamworks.Friend friend)
    {
        UpdatePlayerList();
    }

    private void UpdatePlayerList()
    {
        PlayerList.Clear();
        var lobby = SteamManager.Instance.CurrentLobby;
        
        if (lobby.HasValue)
        {
            bool allReady = true;
            foreach (var member in lobby.Value.Members)
            {
                // We show an asterisk if it's the lobby owner!
                string prefix = (member.Id == lobby.Value.Owner.Id) ? "[HOST] " : "";
                string readyTag = (lobby.Value.GetMemberData(member, "ready") == "true") ? "[READY] " : "";
                
                if (lobby.Value.GetMemberData(member, "ready") != "true") allReady = false;

                // Add the player's Steam username to the Godot List UI
                PlayerList.AddItem(readyTag + prefix + member.Name);
            }

            if (lobby.Value.Owner.Id == SteamClient.SteamId)
            {
                StartGameButton.Visible = true;
                StartGameButton.Disabled = !allReady;
            }
            else
            {
                StartGameButton.Visible = false;
            }
        }
    }

    private void OnLobbyDataUpdated()
    {
        UpdatePlayerList();
        
        var lobby = SteamManager.Instance.CurrentLobby;
        if (lobby.HasValue && lobby.Value.GetData("started") == "true")
        {
            // Transition to game!
            GD.Print("[MainMenu] Game started by host! Loading BattleScene...");
            GetTree().ChangeSceneToFile("res://Scenes/Battle/BattleScene.tscn");
        }
    }

    public override void _ExitTree()
    {
        // Always unsubscribe from static events to prevent memory/reference leaks!
        SteamManager.OnLobbyCreatedEvent -= OnLobbyCreated;
        SteamManager.OnLobbyJoinedEvent -= OnLobbyJoined;
        SteamManager.OnPlayerJoinedEvent -= OnPlayerJoined;
        SteamManager.OnPlayerLeftEvent -= OnPlayerLeft;
        SteamManager.OnLobbyDataUpdatedEvent -= OnLobbyDataUpdated;
    }
}
