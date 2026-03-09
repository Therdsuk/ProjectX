using Godot;
using Steamworks;

/// <summary>
/// A simple Main Menu scene that lets you Host a Steam game and see the Lobby list.
/// </summary>
public partial class MainMenu : Control
{
    private VBoxContainer _vbox;
    private Button _hostButton;
    private Button _leaveButton;
    private Label _lobbyLabel;
    private ItemList _playerList;

    public override void _Ready()
    {
        // 1. Programmatically Generate UI (Saves having to manually edit a layout in code)
        _vbox = new VBoxContainer();
        _vbox.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        AddChild(_vbox);

        var title = new Label();
        title.Text = "STEAM MULTIPLAYER TEST";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        _vbox.AddChild(title);

        _hostButton = new Button();
        _hostButton.Text = "Host Steam Game";
        _hostButton.Pressed += OnHostPressed;
        _vbox.AddChild(_hostButton);

        _leaveButton = new Button();
        _leaveButton.Text = "Leave Lobby";
        _leaveButton.Pressed += OnLeavePressed;
        _leaveButton.Disabled = true;
        _vbox.AddChild(_leaveButton);

        _lobbyLabel = new Label();
        _lobbyLabel.Text = "Status: Not in Lobby";
        _vbox.AddChild(_lobbyLabel);

        _playerList = new ItemList();
        _playerList.CustomMinimumSize = new Vector2(300, 200);
        _vbox.AddChild(_playerList);

        // 2. Subscribe to SteamManager Callbacks
        SteamManager.OnLobbyCreatedEvent += OnLobbyCreated;
        SteamManager.OnLobbyJoinedEvent += OnLobbyJoined;
        SteamManager.OnPlayerJoinedEvent += OnPlayerJoined;
        SteamManager.OnPlayerLeftEvent += OnPlayerLeft;
    }

    private void OnHostPressed()
    {
        _hostButton.Disabled = true;
        _lobbyLabel.Text = "Status: Creating Lobby...";
        SteamManager.Instance.HostLobby();
    }

    private void OnLeavePressed()
    {
        SteamManager.Instance.CurrentLobby?.Leave();
        _hostButton.Disabled = false;
        _leaveButton.Disabled = true;
        _lobbyLabel.Text = "Status: Left Lobby.";
        _playerList.Clear();
    }

    private void OnLobbyCreated(Steamworks.Data.Lobby lobby)
    {
        _lobbyLabel.Text = $"Status: Hosting Lobby ({lobby.Id})";
        _leaveButton.Disabled = false;
        UpdatePlayerList();
    }

    private void OnLobbyJoined(Steamworks.Data.Lobby lobby)
    {
        _hostButton.Disabled = true;
        _leaveButton.Disabled = false;
        _lobbyLabel.Text = $"Status: Joined '{lobby.Owner.Name}'s Lobby";
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
        _playerList.Clear();
        var lobby = SteamManager.Instance.CurrentLobby;
        
        if (lobby.HasValue)
        {
            foreach (var member in lobby.Value.Members)
            {
                // We show an asterisk if it's the lobby owner!
                string prefix = (member.Id == lobby.Value.Owner.Id) ? "[HOST] " : "";
                
                // Add the player's Steam username to the Godot List UI
                _playerList.AddItem(prefix + member.Name);
            }
        }
    }

    public override void _ExitTree()
    {
        // Always unsubscribe from static events to prevent memory/reference leaks!
        SteamManager.OnLobbyCreatedEvent -= OnLobbyCreated;
        SteamManager.OnLobbyJoinedEvent -= OnLobbyJoined;
        SteamManager.OnPlayerJoinedEvent -= OnPlayerJoined;
        SteamManager.OnPlayerLeftEvent -= OnPlayerLeft;
    }
}
