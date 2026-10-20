// The ice orb's ring model: its painted texture as ice that holds its own light (no stage lighting, like the
// effect cards), with a frost rim where the surface turns away from the camera, the vortex at its heart glowing and
// pulsing, glassy refraction of what's behind it, and glints sweeping over the crystal. Drawn in the transparent
// pass so it can see the screen behind it; its clock comes per instance from the art node.
shader_type spatial;
render_mode unshaded, blend_mix, depth_draw_always, cull_back, fog_disabled;

uniform sampler2D basecolor : source_color, filter_linear_mipmap;
uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;

group_uniforms crystal;
uniform vec4 rim_color : source_color = vec4(0.85, 0.97, 1.0, 1.0);
uniform vec4 core_glow_color : source_color = vec4(0.45, 0.9, 1.0, 1.0);
uniform float rim_strength : hint_range(0.0, 3.0) = 1.2;
// Brightness of the vortex at the ring's heart, and how hard it pulses.
uniform float core_glow : hint_range(0.0, 4.0) = 1.6;
uniform float core_pulse : hint_range(0.0, 1.0) = 0.35;
// How much the stage behind shows through the crystal, and how far it bends.
uniform float glass : hint_range(0.0, 1.0) = 0.35;
uniform float refraction : hint_range(0.0, 0.1) = 0.03;
uniform float sweep : hint_range(0.0, 2.0) = 1.0;
group_uniforms;

// Seconds from the part's first frame, and the extra glow while it grows in; set per instance.
instance uniform float t = 0.0;
instance uniform float flare = 0.0;

void fragment() {
	vec4 texel = texture(basecolor, UV);
	float facing = clamp(dot(NORMAL, VIEW), 0.0, 1.0);
	float rim = pow(1.0 - facing, 3.0) * rim_strength;

	// The painted vortex is the bright, saturated middle of the texture: lift it into a glow.
	float lum = dot(texel.rgb, vec3(0.299, 0.587, 0.114));
	// Where this point is on the ring, in the model's own units (the model is 1 across), as the camera sees it.
	vec2 p = (VERTEX.xy - (VIEW_MATRIX * vec4(NODE_POSITION_WORLD, 1.0)).xy) / max(length(MODEL_MATRIX[0].xyz), 1e-4);
	float centre = 1.0 - smoothstep(0.08, 0.38, length(p));
	float beat = 1.0 + core_pulse * (sin(t * 7.0) * 0.6 + sin(t * 17.3 + 1.0) * 0.4);
	vec3 glow = core_glow_color.rgb * core_glow * smoothstep(0.35, 0.9, lum) * centre * beat;

	// Glass: the stage behind, bent along the surface's tilt and tinted by the ice.
	vec2 bent = SCREEN_UV + NORMAL.xy * refraction;
	vec3 behind = textureLod(screen_texture, bent, 0.0).rgb * mix(vec3(1.0), core_glow_color.rgb, 0.5);
	vec3 ice = mix(texel.rgb, behind, glass * facing);

	// A band of light sweeping across the crystal now and then.
	float band = fract(t * 0.55) * 3.0 - 1.0;
	float sweep_light = exp(-pow((p.x + p.y * 0.6) * 2.5 - band * 2.0, 2.0) * 6.0) * sweep * (1.0 - facing * 0.5);

	ALBEDO = ice + glow + (rim_color.rgb * (rim + sweep_light * 0.6)) + core_glow_color.rgb * flare;
	ALPHA = 1.0;
}
