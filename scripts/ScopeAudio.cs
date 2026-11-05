using Godot;
using System;

namespace Primer;

public partial class ScopeAudio : Node
{
	public enum Mode
	{
		Original,
		Procedural,
	}

	public Mode Current { get; private set; } = Mode.Original;
	public bool Playing { get; private set; } = true;

	private AudioStreamPlayer _filePlayer = null!;
	private AudioStreamPlayer _genPlayer = null!;
	private AudioStreamGeneratorPlayback? _genPlayback;
	private AudioEffectCapture? _capture;
	private AudioStreamWav? _wav;
	private readonly DemoComposer _composer = new();
	private Vector2[] _genBuf = Array.Empty<Vector2>();
	private Vector2[] _wavWin = Array.Empty<Vector2>();

	public override void _Ready()
	{
		_filePlayer = new AudioStreamPlayer { Name = "Original", Bus = "Scope" };
		_genPlayer = new AudioStreamPlayer { Name = "Procedural", Bus = "Scope" };
		AddChild(_filePlayer);
		AddChild(_genPlayer);

		var gen = new AudioStreamGenerator
		{
			MixRate = 48000,
			BufferLength = 0.12f,
		};
		_genPlayer.Stream = gen;

		int bus = AudioServer.GetBusIndex("Scope");
		if (bus >= 0)
			_capture = AudioServer.GetBusEffect(bus, 0) as AudioEffectCapture;

		var original = GD.Load<AudioStream>("res://assets/audio/primer.wav");
		if (original != null)
		{
			_filePlayer.Stream = original;
			_wav = original as AudioStreamWav;
			StartOriginal();
		}
		else
			StartProcedural();
	}

	public void TogglePause()
	{
		Playing = !Playing;
		_filePlayer.StreamPaused = !Playing;
		_genPlayer.StreamPaused = !Playing;
	}

	public void Restart()
	{
		if (Current == Mode.Original)
			StartOriginal();
		else
			StartProcedural();
	}

	public void StartOriginal(float from = 0)
	{
		_genPlayer.Stop();
		_genPlayback = null;
		Current = Mode.Original;
		if (_filePlayer.Stream == null)
		{
			StartProcedural();
			return;
		}
		Playing = true;
		_filePlayer.StreamPaused = false;
		_filePlayer.Play(from);
	}

	public void StartProcedural()
	{
		_filePlayer.Stop();
		Current = Mode.Procedural;
		_composer.Reset();
		Playing = true;
		_genPlayer.StreamPaused = false;
		_genPlayer.Play();
		_genPlayback = _genPlayer.GetStreamPlayback() as AudioStreamGeneratorPlayback;
	}

	public int Feed(PhosphorScope scope)
	{
		if (Current == Mode.Original && Playing && FeedWav(scope))
			return _wavWin.Length;
		return DrainTo(scope);
	}

	private bool FeedWav(PhosphorScope scope)
	{
		if (_wav == null || _wav.Data.Length < 4 || _wav.Stereo == false)
			return false;
		int rate = _wav.MixRate;
		if (rate < 8000)
			return false;
		byte[] data = _wav.Data;
		int frames = data.Length / 4;
		int win = Math.Clamp((int)(rate * 0.11f), 2048, 24576);
		int end = (int)(_filePlayer.GetPlaybackPosition() * rate);
		if (end < 2)
			end = 2;
		if (end > frames)
			end = frames;
		int start = end - win;
		if (start < 0)
			start = 0;
		int n = end - start;
		if (n < 2)
			return false;
		if (_wavWin.Length != n)
			_wavWin = new Vector2[n];
		const float s = 1.0f / 32768.0f;
		for (int i = 0; i < n; i++)
		{
			int o = (start + i) * 4;
			short l = (short)(data[o] | (data[o + 1] << 8));
			short r = (short)(data[o + 2] | (data[o + 3] << 8));
			_wavWin[i] = new Vector2(l * s, r * s);
		}
		scope.ReplaceWindow(_wavWin);
		return true;
	}

	public int DrainTo(PhosphorScope scope)
	{
		if (_capture == null)
			return 0;
		int total = 0;
		while (true)
		{
			int avail = _capture.GetFramesAvailable();
			if (avail <= 0)
				break;
			int n = Math.Min(avail, 8192);
			if (!_capture.CanGetBuffer(n))
				break;
			var buf = _capture.GetBuffer(n);
			if (buf.Length == 0)
				break;
			scope.PushSamples(buf);
			total += buf.Length;
			if (total >= 32768)
				break;
		}
		return total;
	}

	public override void _Process(double delta)
	{
		if (!Playing || Current != Mode.Procedural || _genPlayback == null)
			return;
		int want = _genPlayback.GetFramesAvailable();
		if (want <= 0)
			return;
		want = Math.Min(want, 4096);
		if (_genBuf.Length != want)
			_genBuf = new Vector2[want];
		_composer.Fill(_genBuf, 48000.0);
		_genPlayback.PushBuffer(_genBuf);
	}
}
