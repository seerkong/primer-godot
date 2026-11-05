shader_type canvas_item;
render_mode unshaded, blend_mix;

uniform vec4 line_color : source_color = vec4(0.12, 0.55, 0.18, 0.35);
uniform vec4 axis_color : source_color = vec4(0.18, 0.75, 0.28, 0.55);
uniform int divisions : hint_range(2, 16) = 8;

void fragment() {
	vec2 uv = UV;
	vec2 dist = abs(uv - 0.5);
	float px = max(fwidth(uv.x), 0.0008);
	float major = 1.0 / float(divisions);
	vec2 grid_uv = abs(mod(uv + major * 0.5, major) - major * 0.5);
	float gx = 1.0 - smoothstep(px, px * 2.4, grid_uv.x);
	float gy = 1.0 - smoothstep(px, px * 2.4, grid_uv.y);
	float ax = 1.0 - smoothstep(px, px * 2.8, dist.x);
	float ay = 1.0 - smoothstep(px, px * 2.8, dist.y);
	float g = max(gx, gy);
	float a = max(ax, ay);
	vec4 col = line_color * g + axis_color * a;
	COLOR = vec4(col.rgb, clamp(col.a, 0.0, 1.0));
}
