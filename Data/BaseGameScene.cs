using Godot;

public partial class BaseGameScene : Node2D
{
	private WorldEnvironment _worldEnvironment;
	private Sprite2D _worldBackground;
	public override void _Ready()
	{
		_worldEnvironment = GetNode<WorldEnvironment>("WorldEnvironment");
		_worldBackground  = GetNode<Sprite2D>("WorldBackground");
		SetEnvironmentProperties();
		InitializeLevel("level-1");
	}

	private void InitializeLevel(string levelName)
	{
		var scene    = GD.Load<PackedScene>($"res://Data/{levelName}.tscn");
		var instance = scene.Instantiate();
		AddChild(instance);
	}

	private void SetEnvironmentProperties()
	{
		_worldEnvironment.Environment = new Environment()
		{
			BackgroundMode = Environment.BGMode.Canvas,
			GlowEnabled         = true,
			GlowNormalized      = true,
			GlowIntensity       = 2.0f,
			GlowStrength        = 1.5f,
			GlowBloom           = 0.8f,
			GlowBlendMode       = Environment.GlowBlendModeEnum.Softlight,
			GlowHdrThreshold    = 1.0f,
			GlowHdrScale        = 2.0f,
			GlowHdrLuminanceCap = 12f,
			
		};
		
		_worldBackground.Modulate = new Color(0.3f, 0.3f, 0.3f);
	}
}
