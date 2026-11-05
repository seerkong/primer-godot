using Godot;
using System;
using System.Collections.Generic;

namespace Primer;

public static class PathUtil
{
	public static Vector2 Lissajous(double t, float a, float b, float dx, float dy)
	{
		return new Vector2(
			MathF.Sin((float)(t * Math.Tau * a) + dx),
			MathF.Sin((float)(t * Math.Tau * b) + dy));
	}

	public static Vector2 Circle(double t)
	{
		float a = (float)(t * Math.Tau);
		return new Vector2(MathF.Cos(a), MathF.Sin(a));
	}

	public static Vector2 Rose(double t, float k)
	{
		float a = (float)(t * Math.Tau);
		float r = MathF.Cos(k * a);
		return new Vector2(MathF.Cos(a) * r, MathF.Sin(a) * r);
	}

	public static Vector2 Spiral(double t, float turns)
	{
		float u = (float)t;
		float a = u * MathF.Tau * turns;
		float r = 0.08f + 0.9f * u;
		return new Vector2(MathF.Cos(a) * r, MathF.Sin(a) * r);
	}

	public static Vector2 Superellipse(double t, float n)
	{
		float a = (float)(t * Math.Tau);
		float ca = MathF.Cos(a);
		float sa = MathF.Sin(a);
		float rx = MathF.Pow(MathF.Abs(ca), 2f / n) * MathF.Sign(ca);
		float ry = MathF.Pow(MathF.Abs(sa), 2f / n) * MathF.Sign(sa);
		return new Vector2(rx, ry);
	}

	public static Vector2 Cube(double t, float yaw, float pitch)
	{
		ReadOnlySpan<(int A, int B)> edges =
		[
			(0, 1), (1, 2), (2, 3), (3, 0),
			(4, 5), (5, 6), (6, 7), (7, 4),
			(0, 4), (1, 5), (2, 6), (3, 7),
		];
		const int samplesPerEdge = 48;
		int total = edges.Length * samplesPerEdge;
		int idx = (int)(Frac(t) * total) % total;
		int e = idx / samplesPerEdge;
		float u = (idx % samplesPerEdge) / (float)(samplesPerEdge - 1);
		var a = CubeVert(edges[e].A);
		var b = CubeVert(edges[e].B);
		var p = a.Lerp(b, u);
		return Project(p, yaw, pitch);
	}

	private static Vector3 CubeVert(int i)
	{
		float x = (i & 1) != 0 ? 1f : -1f;
		float y = (i & 2) != 0 ? 1f : -1f;
		float z = (i & 4) != 0 ? 1f : -1f;
		return new Vector3(x, y, z) * 0.72f;
	}

	public static Vector2 CheckerSphere(double t, float yaw, float pitch)
	{
		const int latN = 28;
		const int lonN = 48;
		int total = latN * lonN;
		int idx = (int)(Frac(t) * total) % total;
		int iy = idx / lonN;
		int ix = idx % lonN;
		float v = iy / (float)(latN - 1);
		float u = ix / (float)(lonN - 1);
		bool dark = ((ix / 6) + (iy / 4)) % 2 == 0;
		if (dark)
		{
			ix = (ix + 6) % lonN;
			u = ix / (float)(lonN - 1);
		}
		float theta = u * MathF.Tau;
		float phi = v * MathF.PI;
		var p = new Vector3(
			MathF.Sin(phi) * MathF.Cos(theta),
			MathF.Cos(phi),
			MathF.Sin(phi) * MathF.Sin(theta));
		return Project(p * 0.92f, yaw, pitch);
	}

	public static Vector2 Project(Vector3 p, float yaw, float pitch)
	{
		float cy = MathF.Cos(yaw);
		float sy = MathF.Sin(yaw);
		float cp = MathF.Cos(pitch);
		float sp = MathF.Sin(pitch);
		float x1 = p.X * cy + p.Z * sy;
		float z1 = -p.X * sy + p.Z * cy;
		float y2 = p.Y * cp - z1 * sp;
		float z2 = p.Y * sp + z1 * cp;
		float persp = 1.35f / (1.85f + z2);
		return new Vector2(x1 * persp, y2 * persp);
	}

	public static Vector2 Along(IReadOnlyList<Vector2> path, double phase)
	{
		if (path.Count == 0)
			return Vector2.Zero;
		if (path.Count == 1)
			return path[0];
		float u = (float)Frac(phase) * (path.Count - 1);
		int i = Math.Clamp((int)u, 0, path.Count - 2);
		return path[i].Lerp(path[i + 1], u - i);
	}

	public static List<Vector2> Resample(List<Vector2> src, int n)
	{
		var dst = new List<Vector2>(n);
		if (src.Count == 0)
			return dst;
		float[] acc = new float[src.Count];
		for (int i = 1; i < src.Count; i++)
			acc[i] = acc[i - 1] + src[i].DistanceTo(src[i - 1]);
		float total = MathF.Max(acc[^1], 0.0001f);
		for (int k = 0; k < n; k++)
		{
			float d = total * k / Math.Max(1, n - 1);
			int i = 1;
			while (i < acc.Length && acc[i] < d)
				i++;
			float a = acc[i - 1];
			float b = acc[i];
			float t = b - a < 1e-6f ? 0f : (d - a) / (b - a);
			dst.Add(src[i - 1].Lerp(src[i], t));
		}
		return dst;
	}

	public static double Frac(double x)
	{
		x -= Math.Floor(x);
		return x < 0 ? x + 1 : x;
	}
}
