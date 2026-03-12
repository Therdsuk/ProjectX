using Godot;
using System.Collections.Generic;
using Steamworks;

/// <summary>
/// Main Menu controller — manages four panels:
///   1. MainPanel         — landing screen (Create Character / Play)
///   2. CharacterCreatePanel — name + class selection + save
///   3. CharacterSelectPanel — pick a saved character, then host
///   4. LobbyPanel        — Steam lobby view (existing)
/// </summary>
public partial class MainMenu : Control
{
    // -------------------------------------------------------------------------
    // MainPanel
    // -------------------------------------------------------------------------
    [Export] public Control MainPanel { get; set; }

    // -------------------------------------------------------------------------
    // CharacterCreatePanel
    // -------------------------------------------------------------------------
    [Export] public Control  CharacterCreatePanel { get; set; }
    [Export] public LineEdit CreateNameField       { get; set; }
    [Export] public Button   CreateWarriorBtn      { get; set; }
    [Export] public Button   CreateArcherBtn       { get; set; }
    [Export] public Button   CreateWizardBtn       { get; set; }
    [Export] public Button   CreateHealerBtn       { get; set; }
    [Export] public Label    CreateClassDescLabel  { get; set; }
    [Export] public Button   CreateSaveBtn         { get; set; }

    // -------------------------------------------------------------------------
    // CharacterSelectPanel
    // -------------------------------------------------------------------------
    [Export] public Control  CharacterSelectPanel  { get; set; }
    [Export] public ItemList SelectCharacterList   { get; set; }
    [Export] public Label    SelectDescLabel       { get; set; }
    [Export] public Button   SelectHostBtn         { get; set; }
    [Export] public Label    SelectJoinNoticeLabel { get; set; }
    [Export] public Button   DeleteCharacterBtn    { get; set; }

    // -------------------------------------------------------------------------
    // LobbyPanel
    // -------------------------------------------------------------------------
    [Export] public Control  LobbyPanel      { get; set; }
    [Export] public Button   HostButton      { get; set; }
    [Export] public Button   LeaveButton     { get; set; }
    [Export] public Button   ReadyButton     { get; set; }
    [Export] public Button   StartGameButton { get; set; }
    [Export] public Label    LobbyLabel      { get; set; }
    [Export] public ItemList PlayerList      { get; set; }

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------
    private string _createSelectedClass = ClassRegistry.Warrior;
    private List<CharacterProfile> _savedCharacters = new();
    private CharacterProfile _selectedCharacter = null;
    private bool _isReady = false;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        // --- Main Panel buttons (looked up by name since not exported) ---
        GetNode<Button>("MainPanel/VBox/CreateCharBtn").Pressed += OnCreateCharPressed;
        GetNode<Button>("MainPanel/VBox/PlayBtn").Pressed       += OnPlayPressed;

        // --- Character Create Panel ---
        if (CreateWarriorBtn != null) CreateWarriorBtn.Pressed += () => OnCreateClassSelected(ClassRegistry.Warrior);
        if (CreateArcherBtn  != null) CreateArcherBtn.Pressed  += () => OnCreateClassSelected(ClassRegistry.Archer);
        if (CreateWizardBtn  != null) CreateWizardBtn.Pressed  += () => OnCreateClassSelected(ClassRegistry.Wizard);
        if (CreateHealerBtn  != null) CreateHealerBtn.Pressed  += () => OnCreateClassSelected(ClassRegistry.Healer);
        if (CreateSaveBtn    != null) CreateSaveBtn.Pressed    += OnSaveCharacterPressed;
        GetNode<Button>("CharacterCreatePanel/VBox/BackBtn").Pressed += () => ShowPanel(MainPanel);
        OnCreateClassSelected(ClassRegistry.Warrior); // Default highlight

        // --- Character Select Panel ---
        if (SelectCharacterList  != null) SelectCharacterList.ItemSelected   += OnCharacterSelected;
        if (SelectHostBtn        != null) SelectHostBtn.Pressed              += OnSelectAndHostPressed;
        if (DeleteCharacterBtn   != null) DeleteCharacterBtn.Pressed         += OnDeleteCharacterPressed;
        GetNode<Button>("CharacterSelectPanel/VBox/BackBtn2").Pressed += () => ShowPanel(MainPanel);

        // --- Lobby Panel ---
        if (LeaveButton     != null) LeaveButton.Pressed     += OnLeavePressed;
        if (ReadyButton     != null) ReadyButton.Pressed     += OnReadyPressed;
        if (StartGameButton != null) StartGameButton.Pressed += OnStartGamePressed;

        // --- Steam callbacks ---
        SteamManager.OnLobbyCreatedEvent     += OnLobbyCreated;
        SteamManager.OnLobbyJoinedEvent      += OnLobbyJoined;
        SteamManager.OnPlayerJoinedEvent     += OnPlayerJoined;
        SteamManager.OnPlayerLeftEvent       += OnPlayerLeft;
        SteamManager.OnLobbyDataUpdatedEvent += OnLobbyDataUpdated;

        // Start on main panel
        ShowPanel(MainPanel);
    }

    // -------------------------------------------------------------------------
    // Panel Switching
    // -------------------------------------------------------------------------

    private void ShowPanel(Control panel)
    {
        MainPanel?.Hide();
        CharacterCreatePanel?.Hide();
        CharacterSelectPanel?.Hide();
        LobbyPanel?.Hide();
        panel?.Show();
    }

    // -------------------------------------------------------------------------
    // Main Panel Handlers
    // -------------------------------------------------------------------------

    private void OnCreateCharPressed()
    {
        ShowPanel(CharacterCreatePanel);
        if (CreateNameField != null) CreateNameField.Text = "";
        OnCreateClassSelected(ClassRegistry.Warrior);
    }

    private void OnPlayPressed()
    {
        _savedCharacters = CharacterSaveSystem.Load();

        if (_savedCharacters.Count == 0)
        {
            // No characters — redirect to create screen with a hint
            OnCreateCharPressed();
            if (CreateClassDescLabel != null)
                CreateClassDescLabel.Text = "You have no characters yet. Create one first!";
            return;
        }

        RefreshCharacterSelectList();
        ShowPanel(CharacterSelectPanel);
        if (SelectJoinNoticeLabel != null)
            SelectJoinNoticeLabel.Text = "Host a game or wait for a Steam invite.";
    }

    // -------------------------------------------------------------------------
    // Character Create Handlers
    // -------------------------------------------------------------------------

    private void OnCreateClassSelected(string classId)
    {
        _createSelectedClass = classId;
        var data = ClassRegistry.Get(classId);
        if (CreateClassDescLabel != null)
        {
            CreateClassDescLabel.Text =
                $"[{data.ClassName}] {data.ClassDescription}\n" +
                $"HP:{data.BaseHp}  Mana:{data.BaseMana}  ATK:{data.BaseAttack}  " +
                $"DEF:{data.BaseDefense}  SPD:{data.BaseSpeed}  Move:{data.MoveRange}";
        }

        // Visual highlight — flat = not selected, raised = selected
        foreach (var (id, btn) in new[] {
            (ClassRegistry.Warrior, CreateWarriorBtn),
            (ClassRegistry.Archer,  CreateArcherBtn),
            (ClassRegistry.Wizard,  CreateWizardBtn),
            (ClassRegistry.Healer,  CreateHealerBtn),
        })
        {
            if (btn != null) btn.Flat = (id != classId);
        }
    }

    private void OnSaveCharacterPressed()
    {
        string charName = CreateNameField?.Text.Trim() ?? "";
        if (string.IsNullOrEmpty(charName))
        {
            if (CreateClassDescLabel != null)
                CreateClassDescLabel.Text = "Please enter a name for your character!";
            return;
        }

        var profile = new CharacterProfile
        {
            Name    = charName,
            ClassId = _createSelectedClass,
        };
        CharacterSaveSystem.AddCharacter(profile);
        GD.Print($"[MainMenu] Saved character: {profile.Name} ({profile.ClassId})");

        // Immediately go to select screen with the new character available
        _savedCharacters = CharacterSaveSystem.Load();
        RefreshCharacterSelectList();
        ShowPanel(CharacterSelectPanel);
        if (SelectJoinNoticeLabel != null)
            SelectJoinNoticeLabel.Text = $"'{profile.Name}' saved! Host a game to play.";
    }

    // -------------------------------------------------------------------------
    // Character Select Handlers
    // -------------------------------------------------------------------------

    private void RefreshCharacterSelectList()
    {
        if (SelectCharacterList == null) return;
        SelectCharacterList.Clear();
        _selectedCharacter = null;
        if (SelectHostBtn != null)    SelectHostBtn.Disabled = true;
        if (DeleteCharacterBtn != null) DeleteCharacterBtn.Disabled = true;
        if (SelectDescLabel != null)  SelectDescLabel.Text = "Pick a character to see their stats.";

        foreach (var profile in _savedCharacters)
        {
            var data = ClassRegistry.Get(profile.ClassId);
            SelectCharacterList.AddItem($"{profile.Name}  [{data.ClassName}]");
        }
    }

    private void OnCharacterSelected(long index)
    {
        if (index < 0 || index >= _savedCharacters.Count) return;
        _selectedCharacter = _savedCharacters[(int)index];
        var data = ClassRegistry.Get(_selectedCharacter.ClassId);

        if (SelectDescLabel != null)
        {
            SelectDescLabel.Text =
                $"{_selectedCharacter.Name} — {data.ClassName}\n" +
                $"HP:{data.BaseHp}  Mana:{data.BaseMana}  ATK:{data.BaseAttack}\n" +
                $"DEF:{data.BaseDefense}  SPD:{data.BaseSpeed}  Move:{data.MoveRange}";
        }

        if (SelectHostBtn      != null) SelectHostBtn.Disabled = false;
        if (DeleteCharacterBtn != null) DeleteCharacterBtn.Disabled = false;
    }

    private void OnSelectAndHostPressed()
    {
        if (_selectedCharacter == null) return;

        ShowPanel(LobbyPanel);
        LobbyLabel.Text = $"Status: Creating Lobby ({_selectedCharacter.Name})...";
        SteamManager.Instance.HostLobby();
    }

    private void OnDeleteCharacterPressed()
    {
        if (_selectedCharacter == null) return;
        CharacterSaveSystem.DeleteCharacter(_selectedCharacter.Id);
        _savedCharacters = CharacterSaveSystem.Load();
        RefreshCharacterSelectList();
        if (SelectJoinNoticeLabel != null)
            SelectJoinNoticeLabel.Text = "Character deleted.";
    }

    // -------------------------------------------------------------------------
    // Lobby Handlers
    // -------------------------------------------------------------------------

    private void OnLeavePressed()
    {
        SteamManager.Instance.CurrentLobby?.Leave();
        LeaveButton.Disabled    = true;
        ReadyButton.Disabled    = true;
        StartGameButton.Visible = false;
        LobbyLabel.Text         = "Status: Left Lobby.";
        PlayerList.Clear();
        _isReady = false;
        ReadyButton.Text = "Ready Up";
        ShowPanel(MainPanel);
    }

    private void OnReadyPressed()
    {
        _isReady = !_isReady;
        ReadyButton.Text = _isReady ? "Unready" : "Ready Up";
        SteamManager.Instance.ToggleReady(_isReady);
    }

    private void OnStartGamePressed() => SteamManager.Instance.StartGame();

    // -------------------------------------------------------------------------
    // Steam Callbacks
    // -------------------------------------------------------------------------

    private void OnLobbyCreated(Steamworks.Data.Lobby lobby)
    {
        // Store chosen character class in Steam member data
        if (_selectedCharacter != null)
            lobby.SetMemberData("class", _selectedCharacter.ClassId);

        LobbyLabel.Text      = $"Hosting — {_selectedCharacter?.Name ?? "?"} [{ClassRegistry.Get(_selectedCharacter?.ClassId ?? "").ClassName}]";
        LeaveButton.Disabled = false;
        ReadyButton.Disabled = false;
        UpdatePlayerList();
    }

    private void OnLobbyJoined(Steamworks.Data.Lobby lobby)
    {
        // If a character was selected before joining (via Steam invite flow),
        // write it to member data now. Otherwise pick first saved character as fallback.
        if (_selectedCharacter == null)
        {
            _savedCharacters = CharacterSaveSystem.Load();
            _selectedCharacter = _savedCharacters.Count > 0 ? _savedCharacters[0] : null;
        }

        if (_selectedCharacter != null)
            lobby.SetMemberData("class", _selectedCharacter.ClassId);

        ShowPanel(LobbyPanel);
        LeaveButton.Disabled = false;
        ReadyButton.Disabled = false;
        LobbyLabel.Text = $"Joined '{lobby.Owner.Name}' — Playing as {_selectedCharacter?.Name ?? "?"}";
        UpdatePlayerList();
    }

    private void OnPlayerJoined(Steamworks.Friend friend) => UpdatePlayerList();
    private void OnPlayerLeft(Steamworks.Friend friend)   => UpdatePlayerList();

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
            string classId   = lobby.Value.GetMemberData(member, "class");
            string className = string.IsNullOrEmpty(classId) ? "?" : ClassRegistry.Get(classId).ClassName;

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
            GD.Print("[MainMenu] Game started! Loading BattleScene...");
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
