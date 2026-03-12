using Godot;

/// <summary>
/// A programmatic 3D Health Bar that floats above a unit.
/// It uses a SubViewport to render a standard UI ProgressBar as a texture on a Sprite3D.
/// </summary>
public partial class HealthBar3D : Node3D
{
    private ProgressBar _progressBar;
    private SubViewport _viewport;
    private Sprite3D _sprite;

    public override void _Ready()
    {
        // 1. Create a SubViewport
        _viewport = new SubViewport();
        _viewport.Size = new Vector2I(150, 20); // Resolution of the health bar
        _viewport.TransparentBg = true;
        _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;

        // 2. Create the UI ProgressBar
        _progressBar = new ProgressBar();
        _progressBar.CustomMinimumSize = new Vector2(150, 20);
        _progressBar.ShowPercentage = false;
        
        // 3. Style it (Dark Gray background, Red/Green fill based on what it is)
        var styleBoxBg = new StyleBoxFlat { BgColor = new Color(0.2f, 0.2f, 0.2f, 0.8f) };
        var styleBoxFg = new StyleBoxFlat { BgColor = new Color(0.2f, 0.8f, 0.2f, 1.0f) }; // Default green
        _progressBar.AddThemeStyleboxOverride("background", styleBoxBg);
        _progressBar.AddThemeStyleboxOverride("fill", styleBoxFg);
        
        // Add progress bar to viewport
        _viewport.AddChild(_progressBar);
        AddChild(_viewport);

        // 4. Create the Sprite3D to display the viewport texture hovering in 3D space
        _sprite = new Sprite3D();
        _sprite.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        _sprite.Texture = _viewport.GetTexture();
        _sprite.PixelSize = 0.01f;   // 150px * 0.01 = 1.5 world units wide
        
        // Position it above the unit — call SetYOffset() after adding to scene to customise
        _sprite.Position = new Vector3(0, 2.0f, 0); 
        AddChild(_sprite);
    }

    /// <summary>Move the health bar to a specific Y world position above the character origin.</summary>
    public void SetYOffset(float y)
    {
        if (_sprite != null)
            _sprite.Position = new Vector3(0, y, 1.0f);
    }

    /// <summary>
    /// Update the health bar values. Optionally change color based on percentage or unit type.
    /// </summary>
    public void UpdateHealth(int current, int max, bool isEnemy = false)
    {
        if (_progressBar == null) return;
        _progressBar.MaxValue = max;
        _progressBar.Value = current;

        // Optional: Make enemies have red bars, players have green bars
        var styleBoxFg = new StyleBoxFlat { 
            BgColor = isEnemy ? new Color(0.8f, 0.2f, 0.2f, 1.0f) : new Color(0.2f, 0.8f, 0.2f, 1.0f) 
        };
        _progressBar.AddThemeStyleboxOverride("fill", styleBoxFg);
    }
}
