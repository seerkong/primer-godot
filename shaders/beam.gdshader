shader_type canvas_item;
render_mode unshaded, blend_add;

void fragment() {
	float d = abs(UV.y - 0.5) * 2.0;
	float core = exp(-d * d * 32.0);
	float halo = exp(-d * d * 5.0) * 0.4;
	float spike = exp(-d * d * 90.0) * 0.8;
	vec3 phosphor = vec3(0.38, 1.0, 0.36);
	vec3 hot = vec3(0.92, 1.0, 0.86);
	vec3 col = mix(phosphor, hot, spike) * (core + halo);
	COLOR = vec4(col * COLOR.rgb, 1.0) * COLOR.a;
}
