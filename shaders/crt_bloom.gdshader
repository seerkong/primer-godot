shader_type canvas_item;
render_mode unshaded;

uniform float bloom_spread : hint_range(0.5, 8.0) = 1.35;
uniform float bloom_gain : hint_range(0.0, 4.0) = 0.55;
uniform float vignette : hint_range(0.0, 1.5) = 0.28;

void fragment() {
	vec2 uv = UV;
	vec2 px = TEXTURE_PIXEL_SIZE;
	vec3 c = texture(TEXTURE, uv).rgb;
	vec3 b = vec3(0.0);
	b += texture(TEXTURE, uv + vec2(px.x, 0.0) * bloom_spread).rgb;
	b += texture(TEXTURE, uv - vec2(px.x, 0.0) * bloom_spread).rgb;
	b += texture(TEXTURE, uv + vec2(0.0, px.y) * bloom_spread).rgb;
	b += texture(TEXTURE, uv - vec2(0.0, px.y) * bloom_spread).rgb;
	b += texture(TEXTURE, uv + px * bloom_spread * 2.2).rgb * 0.55;
	b += texture(TEXTURE, uv - px * bloom_spread * 2.2).rgb * 0.55;
	b += texture(TEXTURE, uv + vec2(px.x, -px.y) * bloom_spread * 2.2).rgb * 0.55;
	b += texture(TEXTURE, uv + vec2(-px.x, px.y) * bloom_spread * 2.2).rgb * 0.55;
	b *= 0.125;
	float lum = dot(b, vec3(0.22, 0.72, 0.06));
	vec3 glow = b * bloom_gain * (0.5 + lum * 1.6);
	vec3 rgb = c + glow * vec3(0.42, 1.0, 0.38);
	float r = length(uv - 0.5);
	float vig = 1.0 - smoothstep(0.46, 0.82, r * (0.75 + vignette));
	COLOR = vec4(rgb * vig, 1.0);
}
