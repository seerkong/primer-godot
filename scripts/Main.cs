using Godot;
using System;

namespace Primer;

public partial class Main : Control
{
	private PhosphorScope _scope = null!;
	private ScopeAudio _audio = null!;
	private Label _hud = null!;
	private float _hudAge;
	private bool _hudPinned;

	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;

		var bg = new ColorRect
		{
			Color = Colors.Black,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(bg);

		_scope = new PhosphorScope { Name = "Scope" };
		_scope.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(_scope);

		_audio = new ScopeAudio { Name = "Audio" };
		AddChild(_audio);

		_hud = new Label
		{
			Name = "Hud",
			Text = HudText(),
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_hud.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
		_hud.OffsetTop = -96;
		_hud.OffsetLeft = 24;
		_hud.OffsetRight = -24;
		_hud.OffsetBottom = -16;
		_hud.AddThemeColorOverride("font_color", new Color(0.55f, 1f, 0.6f, 0.85f));
		_hud.AddThemeFontSizeOverride("font_size", 14);
		AddChild(_hud);

		bool shot = false;
		float shotAt = 0;
		foreach (string arg in OS.GetCmdlineUserArgs())
		{
			if (arg == "--shot")
				shot = true;
			else if (arg.StartsWith("--shot-at="))
				float.TryParse(arg.AsSpan(10), System.Globalization.NumberStyles.Float,
					System.Globalization.CultureInfo.InvariantCulture, out shotAt);
		}
		if (shot)
		{
			_hud.Visible = false;
			if (shotAt > 0)
				_audio.StartOriginal(shotAt);
			GetTree().CreateTimer(1.8).Timeout += SaveShotAndQuit;
		}
	}

	private void SaveShotAndQuit()
	{
		SaveShot();
		GetTree().Quit();
	}

	private void SaveShot()
	{
		var tex = GetViewport().GetTexture();
		if (tex == null)
			return;
		var img = tex.GetImage();
		if (img == null)
			return;
		string path = ProjectSettings.GlobalizePath("res://primer_preview.png");
		img.SavePng(path);
		GD.Print("Wrote ", path);
	}

	public override void _Process(double delta)
	{
		_audio.Feed(_scope);
		_hudAge += (float)delta;
		if (!_hudPinned)
		{
			float a = Mathf.Clamp(1.4f - _hudAge * 0.22f, 0f, 1f);
			_hud.Modulate = new Color(1, 1, 1, a);
		}
		if (_audio.Current == ScopeAudio.Mode.Original && _audio.Playing)
			_hud.Text = HudText();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true, Echo: false } key)
		{
			switch (key.PhysicalKeycode)
			{
				case Key.F:
					DisplayServer.WindowSetMode(
						DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen
							? DisplayServer.WindowMode.Windowed
							: DisplayServer.WindowMode.Fullscreen);
					break;
				case Key.Space:
					_audio.TogglePause();
					break;
				case Key.G:
					_scope.GraticuleVisible = !_scope.GraticuleVisible;
					break;
				case Key.Key1:
					_audio.StartOriginal();
					break;
				case Key.Key2:
					_audio.StartProcedural();
					break;
				case Key.R:
					_audio.Restart();
					break;
				case Key.Equal:
				case Key.KpAdd:
					_scope.Gain = Mathf.Clamp(_scope.Gain * 1.08f, 0.2f, 4f);
					break;
				case Key.Minus:
				case Key.KpSubtract:
					_scope.Gain = Mathf.Clamp(_scope.Gain / 1.08f, 0.2f, 4f);
					break;
				case Key.H:
					_hudPinned = !_hudPinned;
					_hud.Modulate = Colors.White;
					_hudAge = 0;
					break;
				case Key.F12:
					SaveShot();
					break;
				case Key.Escape:
					GetTree().Quit();
					break;
			}
			_hud.Text = HudText();
			_hud.Modulate = Colors.White;
			_hudAge = 0;
		}

		if (@event is InputEventMouseButton { Pressed: true } mb)
		{
			if (mb.ButtonIndex == MouseButton.WheelUp)
				_scope.Zoom = Mathf.Clamp(_scope.Zoom * 1.06f, 0.4f, 3.5f);
			else if (mb.ButtonIndex == MouseButton.WheelDown)
				_scope.Zoom = Mathf.Clamp(_scope.Zoom / 1.06f, 0.4f, 3.5f);
		}
	}

	private string HudText()
	{
		string mode = _audio.Current == ScopeAudio.Mode.Original ? "ORIGINAL XY" : "PROCEDURAL";
		string run = _audio.Playing ? "PLAY" : "PAUSE";
		string grid = _scope.GraticuleVisible ? "GRID ON" : "GRID OFF";
		return $"PRIMER  ·  BUS ERROR Collective  ·  CC BY-SA 4.0\n" +
			   $"{mode}  {run}  {grid}  gain {_scope.Gain:0.00}  zoom {_scope.Zoom:0.00}\n" +
			   $"1 original   2 procedural   space pause   G grid   F fullscreen   wheel zoom   H hud   Esc";
	}
}
