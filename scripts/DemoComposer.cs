using Godot;
using System;
using System.Collections.Generic;

namespace Primer;

public sealed class DemoComposer
{
	private double _t;
	private List<Vector2> _primer = TextPaths.Build("PRIMER", 0.55f, 0.85f);
	private List<Vector2> _bec = TextPaths.Build("BUS ERROR", 0.28f, 0.7f);
	private List<Vector2> _see = TextPaths.Build("WHAT YOU SEE", 0.22f, 0.62f);
	private List<Vector2> _hear = TextPaths.Build("IS WHAT YOU HEAR", 0.18f, 0.55f);

	public void Reset() => _t = 0;

	public void Fill(Vector2[] dest, double mixRate)
	{
		double dt = 1.0 / mixRate;
		for (int i = 0; i < dest.Length; i++)
		{
			dest[i] = Sample(_t);
			_t += dt;
		}
	}

	private Vector2 Sample(double t)
	{
		const double len = 92.0;
		double u = t % len;
		Vector2 p;
		if (u < 6)
			p = PathUtil.Circle(t * Lerp(40, 180, u / 6.0)) * Ease(u / 6.0);
		else if (u < 14)
			p = PathUtil.Lissajous(t, 2, 3, 0, (float)(u * 0.2)) * 0.9f;
		else if (u < 24)
			p = PathUtil.Along(_primer, t * 22.0) * Pulse(t, 2.0f, 0.08f);
		else if (u < 32)
			p = PathUtil.Along(_bec, t * 18.0);
		else if (u < 44)
			p = PathUtil.Cube(t * 14.0, (float)t * 0.7f, (float)t * 0.33f);
		else if (u < 58)
			p = PathUtil.CheckerSphere(t * 9.0, (float)t * 0.55f, 0.4f + 0.2f * MathF.Sin((float)t));
		else if (u < 68)
			p = PathUtil.Rose(t * 6.0, 4f + 2f * MathF.Sin((float)(t * 0.3))) * 0.95f;
		else if (u < 76)
			p = PathUtil.Along(_see, t * 16.0);
		else if (u < 84)
			p = PathUtil.Along(_hear, t * 14.0);
		else
			p = PathUtil.Along(_primer, t * 20.0) * (1.0f - (float)((u - 84) / 8.0) * 0.7f);

		float wob = 0.012f * MathF.Sin((float)(t * 37.0));
		return p + new Vector2(wob, -wob * 0.6f);
	}

	private static float Ease(double x) => (float)(0.15 + 0.85 * Math.Clamp(x, 0, 1));

	private static float Pulse(double t, float hz, float amt)
	{
		return 1.0f + amt * MathF.Sin((float)(t * Math.Tau * hz));
	}

	private static float Lerp(float a, float b, double t) => a + (b - a) * (float)Math.Clamp(t, 0, 1);
}
