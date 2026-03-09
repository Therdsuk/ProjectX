using Godot;
using Steamworks;

/// <summary>
/// A global AutoLoad (Singleton) responsible for initializing the Facepunch.Steamworks library
/// and pushing Steam callbacks every frame.
/// </summary>
public partial class SteamManager : Node
{
    public static SteamManager Instance { get; private set; }
    
    // The AppId for Spacewar (Steam's officially sanctioned testing app)
    public const uint AppId = 480;

    public bool IsSteamInitialized { get; private set; } = false;
    
    // The active lobby we are hosting or sitting in
    public Steamworks.Data.Lobby? CurrentLobby { get; private set; }

    // Events for UI to listen to
    public static event System.Action<Steamworks.Data.Lobby> OnLobbyCreatedEvent;
    public static event System.Action<Steamworks.Data.Lobby> OnLobbyJoinedEvent;
    public static event System.Action<Steamworks.Friend> OnPlayerJoinedEvent;
    public static event System.Action<Steamworks.Friend> OnPlayerLeftEvent;

    public override void _EnterTree()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        
        Instance = this;
        
        try
        {
            // Initialize the SteamClient. The "true" argument initializes Steamworks asynchronously.
            SteamClient.Init(AppId, true);
            
            if (SteamClient.IsValid)
            {
                IsSteamInitialized = true;
                GD.Print($"[SteamManager] Successfully initialized Steam! Logged in as: {SteamClient.Name} (ID: {SteamClient.SteamId})");
                
                // Hook up the matchmaking callbacks so we know when people invite us or join us
                SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
                SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
                SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
                SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeft;
                
                // This is the big one: Fires when you accept an invite from the Steam Overlay
                SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
            }
            else
            {
                GD.PrintErr("[SteamManager] SteamClient initialized but is invalid. Is Steam running?");
            }
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"[SteamManager] Failed to initialize Steam. Error: {e.Message}");
        }
    }

    // -------------------------------------------------------------------------
    // Lobby Hosting & Joining
    // -------------------------------------------------------------------------

    /// <summary>
    /// Call this to start hosting a new game!
    /// Steam requires you to be in a Lobby to enable the "Invite to Game" button in the overlay.
    /// </summary>
    public async void HostLobby()
    {
        if (!IsSteamInitialized) return;

        GD.Print("[SteamManager] Requesting to create a Steam Lobby...");
        // Ask Steam to create a lobby for 4 players max
        var lobbyResult = await SteamMatchmaking.CreateLobbyAsync(4);
        
        if (!lobbyResult.HasValue)
        {
            GD.PrintErr("[SteamManager] Failed to create lobby.");
            return;
        }

        // We successfully created it, but OnLobbyCreated callback will handle the rest!
    }

    // -------------------------------------------------------------------------
    // Steam Callbacks
    // -------------------------------------------------------------------------

    private void OnLobbyCreated(Steamworks.Result result, Steamworks.Data.Lobby lobby)
    {
        if (result != Steamworks.Result.OK)
        {
            GD.PrintErr($"[SteamManager] Lobby creation failed with result: {result}");
            return;
        }

        CurrentLobby = lobby;
        
        // This makes the lobby public and joinable via friends list
        lobby.SetPublic();
        lobby.SetJoinable(true);
        
        // We set metadata on the lobby so peers know what game they are connecting to
        lobby.SetData("name", $"{SteamClient.Name}'s Rogue Card Match");

        GD.Print($"[SteamManager] Lobby created successfully! ID: {lobby.Id}");
        GD.Print("[SteamManager] You can now Shift+Tab and invite friends!");
        
        OnLobbyCreatedEvent?.Invoke(lobby);
        
        // FUTURE: Here we will start the Godot MultiplayerServer
    }

    private void OnLobbyEntered(Steamworks.Data.Lobby lobby)
    {
        CurrentLobby = lobby;
        GD.Print($"[SteamManager] Successfully entered lobby: {lobby.Id}");
        
        // If we are not the owner, we just joined someone else's game!
        if (lobby.Owner.Id != SteamClient.SteamId)
        {
             // FUTURE: Here we will start the Godot MultiplayerClient and connect to the Host
             GD.Print($"[SteamManager] Connected to Host: {lobby.Owner.Name}");
        }

        OnLobbyJoinedEvent?.Invoke(lobby);
    }

    private async void OnGameLobbyJoinRequested(Steamworks.Data.Lobby lobby, Steamworks.SteamId friendId)
    {
        GD.Print($"[SteamManager] Accepted invite from {friendId}. Joining lobby {lobby.Id}...");
        
        // We received an invite and clicked "Join" in the overlay! Let's join it.
        var joinResult = await lobby.Join();
        
        if (joinResult != Steamworks.RoomEnter.Success)
        {
            GD.PrintErr($"[SteamManager] Failed to join lobby. Result: {joinResult}");
        }
    }

    private void OnLobbyMemberJoined(Steamworks.Data.Lobby lobby, Steamworks.Friend friend)
    {
        GD.Print($"[SteamManager] Player Joined the Lobby: {friend.Name}");
        OnPlayerJoinedEvent?.Invoke(friend);
    }

    private void OnLobbyMemberLeft(Steamworks.Data.Lobby lobby, Steamworks.Friend friend)
    {
        GD.Print($"[SteamManager] Player Left the Lobby: {friend.Name}");
        OnPlayerLeftEvent?.Invoke(friend);
    }

    // -------------------------------------------------------------------------
    // Godot Loops
    // -------------------------------------------------------------------------

    public override void _Process(double delta)
    {
        if (IsSteamInitialized)
        {
            // Must be called every frame to process Steam events/callbacks
            SteamClient.RunCallbacks();
        }
    }

    public override void _ExitTree()
    {
        if (IsSteamInitialized)
        {
            CurrentLobby?.Leave();
            SteamClient.Shutdown();
            GD.Print("[SteamManager] SteamClient gracefully shut down.");
        }
    }
}
