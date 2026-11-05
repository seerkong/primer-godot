using Godot;
using System;

namespace Primer;

public partial class PhosphorScope : SubViewportContainer
{
	public float Zoom { get; set; } = 1.15f;
	public float Gain { get; set; } = 1.0f;
	public float LineWidth { get; set; } = 0.7f;
	public float Persistence { get; set; } = 0.12f;
	public bool GraticuleVisible { get; set; }

	private SubViewport _vp = null!;
	private ColorRect _decay = null!;
	private MeshInstance2D _beam = null!;
	private ArrayMesh _mesh = null!;
	private ColorRect _graticule = null!;

	private Vector2[] _ring = Array.Empty<Vector2>();
	private int _cap;
	private int _head;
	private int _count;

	private Vector3[] _verts = Array.Empty<Vector3>();
	private Color[] _cols = Array.Empty<Color>();
	private Vector2[] _uvs = Array.Empty<Vector2>();
	private int[] _idx = Array.Empty<int>();

	public override void _Ready()
	{
		Stretch = true;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Ignore;

		float mix = AudioServer.GetMixRate();
		if (mix < 8000f)
			mix = 48000f;
		_cap = Math.Max(24576, (int)(mix * 0.12f));
		_ring = new Vector2[_cap];

		_vp = new SubViewport
		{
			Name = "ScopeVp",
			Disable3D = true,
			TransparentBg = false,
			Msaa2D = Viewport.Msaa.Msaa2X,
			HandleInputLocally = false,
			RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
			Size = new Vector2I(1280, 720),
		};
		_vp.UseHdr2D = true;
		AddChild(_vp);

		_decay = new ColorRect
		{
			Name = "Decay",
			Color = new Color(0, 0, 0, 0),
			Visible = false,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_decay.Position = Vector2.Zero;
		_decay.Size = new Vector2(1280, 720);
		_vp.AddChild(_decay);

		_mesh = new ArrayMesh();
		_beam = new MeshInstance2D
		{
			Name = "Beam",
			Mesh = _mesh,
			Material = new ShaderMaterial
			{
				Shader = GD.Load<Shader>("res://shaders/beam.gdshader"),
			},
			ZIndex = 1,
		};
		_vp.AddChild(_beam);

		_graticule = new ColorRect
		{
			Name = "Graticule",
			Color = Colors.White,
			Visible = false,
			MouseFilter = MouseFilterEnum.Ignore,
			ZIndex = 2,
		};
		_graticule.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_graticule.Material = new ShaderMaterial
		{
			Shader = GD.Load<Shader>("res://shaders/graticule.gdshader"),
		};
		AddChild(_graticule);

		Material = new ShaderMaterial
		{
			Shader = GD.Load<Shader>("res://shaders/crt_bloom.gdshader"),
		};

		RenderingServer.ViewportSetClearMode(_vp.GetViewportRid(), RenderingServer.ViewportClearMode.Always);
	}

	public override void _Notification(int what)
	{
		if (what == NotificationResized)
			ResizeViewport();
	}

	private void ResizeViewport()
	{
		if (_vp == null)
			return;
		var sz = Size;
		if (sz.X < 8 || sz.Y < 8)
			return;
		_vp.Size = new Vector2I(Math.Max(8, (int)sz.X), Math.Max(8, (int)sz.Y));
		_decay.Size = _vp.Size;
	}

	public void ReplaceWindow(Vector2[] frames)
	{
		if (_ring.Length == 0 || frames.Length == 0)
			return;
		int n = Math.Min(frames.Length, _cap);
		int src = frames.Length - n;
		for (int i = 0; i < n; i++)
			_ring[i] = frames[src + i];
		_count = n;
		_head = n % _cap;
	}

	public void PushSamples(Vector2[] frames)
	{
		if (_ring.Length == 0)
			return;
		for (int i = 0; i < frames.Length; i++)
		{
			_ring[_head] = frames[i];
			_head = (_head + 1) % _cap;
			if (_count < _cap)
				_count++;
		}
	}

	public override void _Process(double delta)
	{
		_graticule.Visible = GraticuleVisible;
		RebuildBeam();
	}

	private void RebuildBeam()
	{
		if (_count < 2)
			return;

		EnsureCap(_count);
		Vector2 vp = _vp.Size;
		float sc = MathF.Min(vp.X, vp.Y) * 0.48f * Zoom;
		Vector2 origin = vp * 0.5f;

		int vi = 0;
		int ii = 0;
		int segs = 0;
		Vector2 last = Map(Sample(0), origin, sc);

		for (int s = 1; s < _count; s++)
		{
			Vector2 cur = Map(Sample(s), origin, sc);
			Vector2 d = cur - last;
			float len = d.Length();
			Vector2 nrm;
			float intens;
			float w;
			if (len < 1e-4f)
			{
				nrm = new Vector2(0, 1);
				w = MathF.Max(LineWidth, 0.7f);
				intens = 1.8f;
			}
			else
			{
				nrm = new Vector2(-d.Y, d.X) / len;
				w = LineWidth * (0.7f + 0.5f / (len + 0.45f));
				intens = Mathf.Clamp(2.2f / (len + 0.55f), 0f, 1.8f);
			}

			var col = new Color(1f, 1f, 1f, intens);
			int b = vi;
			_verts[vi] = new Vector3(last.X + nrm.X * w, last.Y + nrm.Y * w, 0);
			_uvs[vi] = new Vector2(0, 0);
			_cols[vi++] = col;
			_verts[vi] = new Vector3(last.X - nrm.X * w, last.Y - nrm.Y * w, 0);
			_uvs[vi] = new Vector2(0, 1);
			_cols[vi++] = col;
			_verts[vi] = new Vector3(cur.X - nrm.X * w, cur.Y - nrm.Y * w, 0);
			_uvs[vi] = new Vector2(1, 1);
			_cols[vi++] = col;
			_verts[vi] = new Vector3(cur.X + nrm.X * w, cur.Y + nrm.Y * w, 0);
			_uvs[vi] = new Vector2(1, 0);
			_cols[vi++] = col;
			_idx[ii++] = b;
			_idx[ii++] = b + 1;
			_idx[ii++] = b + 2;
			_idx[ii++] = b;
			_idx[ii++] = b + 2;
			_idx[ii++] = b + 3;
			segs++;
			last = cur;
		}

		if (segs == 0)
			return;

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = SliceVec3(_verts, vi);
		arrays[(int)Mesh.ArrayType.Color] = SliceCol(_cols, vi);
		arrays[(int)Mesh.ArrayType.TexUV] = SliceVec2(_uvs, vi);
		arrays[(int)Mesh.ArrayType.Index] = SliceInt(_idx, ii);
		_mesh.ClearSurfaces();
		_mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		_beam.Mesh = _mesh;
	}

	private Vector2 Sample(int i)
	{
		int start = _count == _cap ? _head : 0;
		return _ring[(start + i) % _cap] * Gain;
	}

	private static Vector2 Map(Vector2 s, Vector2 origin, float sc)
	{
		return new Vector2(origin.X + s.X * sc, origin.Y - s.Y * sc);
	}

	private void EnsureCap(int segs)
	{
		int v = segs * 4;
		int i = segs * 6;
		if (_verts.Length < v)
		{
			_verts = new Vector3[v];
			_cols = new Color[v];
			_uvs = new Vector2[v];
		}
		if (_idx.Length < i)
			_idx = new int[i];
	}

	private static Vector3[] SliceVec3(Vector3[] src, int n)
	{
		var d = new Vector3[n];
		Array.Copy(src, d, n);
		return d;
	}

	private static Vector2[] SliceVec2(Vector2[] src, int n)
	{
		var d = new Vector2[n];
		Array.Copy(src, d, n);
		return d;
	}

	private static Color[] SliceCol(Color[] src, int n)
	{
		var d = new Color[n];
		Array.Copy(src, d, n);
		return d;
	}

	private static int[] SliceInt(int[] src, int n)
	{
		var d = new int[n];
		Array.Copy(src, d, n);
		return d;
	}
}
