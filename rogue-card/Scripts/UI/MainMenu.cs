using Godot;
using Steamworks;

/// <summary>
/// Main Menu: shows a Class Select screen first, then the Lobby screen.
/// Stores the selected class in Steam member data so DebugBattleSpawner
/// can read it when the match starts.
/// </summary>
public partial class MainMenu : Control
{
    // -------------------------------------------------------------------------
    // Lobby Panel exports
    // -------------------------------------------------------------------------
    [Export] public Control  LobbyPanel      { get; set; }
    [Export] public Button   HostButton      { get; set; }
    [Export] public Button   LeaveButton     { get; set; }
    [Export] public Button   ReadyButton     { get; set; }
    [Export] public Button   StartGameButton { get; set; }
    [Export] public Label    LobbyLabel      { get; set; }
    [Export] public ItemList PlayerList      { get; set; }

    // -------------------------------------------------------------------------
    // Class Select Panel exports
    // -------------------------------------------------------------------------
    [Export] public Control ClassSelectPanel      { get; set; }
    [Export] public Label   ClassDescriptionLabel { get; set; }
    [Export] public Button  WarriorButton         { get; set; }
    [Export] public Button  ArcherButton          { get; set; }
    [Export] public Button  WizardButton          { get; set; }
    [Export] public Button  HealerButton          { get; set; }
    // ConfirmButton is connected via the tscn signal in _Ready
    
    private string _selectedClass = ClassRegistry.Warrior;
    private bool   _isReady       = false;

    public override void _Ready()
    {
        // --- Class Select signals ---
        if (WarriorButton != null) WarriorButton.Pressed += () => OnClassSelected(ClassRegistry.Warrior);
        if (ArcherButton  != null) ArcherButton.Pressed  += () => OnClassSelected(ClassRegistry.Archer);
        if (WizardButton  != null) WizardButton.Pressed  += () => OnClassSelected(ClassRegistry.Wizard);
        if (HealerButton  != null) HealerButton.Pressed  += () => OnClassSelected(ClassRegistry.Healer);

        // ConfirmButton lives inside ClassSelectPanel — find it by name
        var confirmButton = ClassSelectPanel?.GetNodeOrNull<Button>("VBoxContainer/ConfirmButton");
        if (confirmButton != null) confirmButton.Pressed += OnConfirmClassPressed;

        // Highlight the default class
        OnClassSelected(ClassRegistry.Warrior);

        // --- Lobby signals ---
        if (HostButton      != null) HostButton.Pressed      += OnHostPressed;
        if (LeaveButton     != null) LeaveButton.Pressed     += OnLeavePressed;
        if (ReadyButton     != null) ReadyButton.Pressed     += OnReadyPressed;
        if (StartGameButton != null) StartGameButton.Pressed += OnStartGamePressed;

        // --- Steam callbacks ---
        SteamManager.OnLobbyCreatedEvent    += OnLobbyCreated;
        SteamManager.OnLobbyJoinedEvent     += OnLobbyJoined;
        SteamManager.OnPlayerJoinedEvent    += OnPlayerJoined;
        SteamManager.OnPlayerLeftEvent      += OnPlayerLeft;
        SteamManager.OnLobbyDataUpdatedEvent += OnLobbyDataUpdated;
    }

    // -------------------------------------------------------------------------
    // Class Select handlers
    // -------------------------------------------------------------------------

    private void OnClassSelected(string classId)
    {
        _selectedClass = classId;
        var data = ClassRegistry.Get(classId);

        // Update description label
        if (ClassDescriptionLabel != null)
        {
            ClassDescriptionLabel.Text =
                $"[{data.ClassName}]\n" +
                $"{data.ClassDescription}\n\n" +
                $"HP: {data.BaseHp}  Mana: {data.BaseMana}  ATK: {data.BaseAttack}\n" +
                $"DEF: {data.BaseDefense}  SPD: {data.BaseSpeed}  Move: {data.MoveRange}";
        }

        // Highlight selected button (flat = unselected, normal = selected)
        foreach (var (id, btn) in new[] {
            (ClassRegistry.Warrior, WarriorButton),
            (ClassRegistry.Archer,  ArcherButton),
            (ClassRegistry.Wizard,  WizardButton),
            (ClassRegistry.Healer,  HealerButton),
        })
        {
            if (btn != null)
                btn.Flat = (id != classId);
        }
    }

    private void OnConfirmClassPressed()
    {
        // Switch to lobby panel and start hosting
        if (ClassSelectPanel != null) ClassSelectPanel.Visible = false;
        if (LobbyPanel != null)       LobbyPanel.Visible = true;

        HostButton.Disabled = true;
        LobbyLabel.Text = "Status: Creating Lobby...";
        SteamManager.Instance.HostLobby();
    }

    // -------------------------------------------------------------------------
    // Lobby handlers
    // -------------------------------------------------------------------------

    private void OnHostPressed()
    {
        // Show class selection first before actually creating the lobby
        if (ClassSelectPanel != null) ClassSelectPanel.Visible = true;
        if (LobbyPanel != null)       LobbyPanel.Visible = false;
    }

    private void OnLeavePressed()
    {
        SteamManager.Instance.CurrentLobby?.Leave();
        HostButton.Disabled    = false;
        LeaveButton.Disabled   = true;
        ReadyButton.Disabled   = true;
        StartGameButton.Visible = false;
        LobbyLabel.Text        = "Status: Left Lobby.";
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

    // -------------------------------------------------------------------------
    // Steam Lobby callbacks
    // -------------------------------------------------------------------------

    private void OnLobbyCreated(Steamworks.Data.Lobby lobby)
    {
        // Store the selected class in our Steam member data so spawner can read it
        lobby.SetMemberData("class", _selectedClass);

        LobbyLabel.Text      = $"Status: Hosting Lobby — Class: {ClassRegistry.Get(_selectedClass).ClassName}";
        LeaveButton.Disabled = false;
        ReadyButton.Disabled = false;
        UpdatePlayerList();
    }

    private void OnLobbyJoined(Steamworks.Data.Lobby lobby)
    {
        // Store our class in member data so host can read it
        lobby.SetMemberData("class", _selectedClass);

        HostButton.Disabled  = true;
        LeaveButton.Disabled = false;
        ReadyButton.Disabled = false;
        LobbyLabel.Text      = $"Status: Joined '{lobby.Owner.Name}' — Class: {ClassRegistry.Get(_selectedClass).ClassName}";
        UpdatePlayerList();
    }

    private void OnPlayerJoined(Steamworks.Friend friend)  => UpdatePlayerList();
    private void OnPlayerLeft(Steamworks.Friend friend)    => UpdatePlayerList();

    private void UpdatePlayerList()
    {
        PlayerList.Clear();
        var lobby = SteamManager.Instance.CurrentLobby;

        if (!lobby.HasValue) return;

        bool allReady = true;
        foreach (var member in lobby.Value.Members)
        {
            string prefix    = (member.Id == lobby.Value.Owner.Id) ? "[HOST] " : "";
            string readyTag  = (lobby.Value.GetMemberData(member, "ready") == "true") ? "[READY] " : "";
            string classTag  = lobby.Value.GetMemberData(member, "class");
            string className = string.IsNullOrEmpty(classTag) ? "?" : ClassRegistry.Get(classTag).ClassName;

            if (lobby.Value.GetMemberData(member, "ready") != "true") allReady = false;

            PlayerList.AddItem($"{readyTag}{prefix}{member.Name} ({className})");
        }

        if (lobby.Value.Owner.Id == SteamClient.SteamId)
        {
            StartGameButton.Visible  = true;
            StartGameButton.Disabled = !allReady;
        }
        else
        {
            StartGameButton.Visible = false;
        }
    }

    private void OnLobbyDataUpdated()
    {
        UpdatePlayerList();

        var lobby = SteamManager.Instance.CurrentLobby;
        if (lobby.HasValue && lobby.Value.GetData("started") == "true")
        {
            GD.Print("[MainMenu] Game started by host! Loading BattleScene...");
            GetTree().ChangeSceneToFile("res://Scenes/Battle/BattleScene.tscn");
        }
    }

    public override void _ExitTree()
    {
        SteamManager.OnLobbyCreatedEvent     -= OnLobbyCreated;
        SteamManager.OnLobbyJoinedEvent      -= OnLobbyJoined;
        SteamManager.OnPlayerJoinedEvent     -= OnPlayerJoined;
        SteamManager.OnPlayerLeftEvent       -= OnPlayerLeft;
        SteamManager.OnLobbyDataUpdatedEvent -= OnLobbyDataUpdated;
    }
}
