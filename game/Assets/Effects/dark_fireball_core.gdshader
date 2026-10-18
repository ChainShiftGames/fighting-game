// The dark fireball's solid core: black, with a ragged violet rim that boils. `t` is the projectile's age in
// seconds, from its logic frame, so the look follows rollback and hitstop.
shader_type spatial;
render_mode unshaded, cull_disabled;

#include "res://Assets/Effects/effect_noise.gdshaderinc"

uniform float t = 0.0;
uniform vec4 core_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform vec4 rim_color : source_color = vec4(0.62, 0.2, 1.0, 1.0);
uniform vec4 hot_color : source_color = vec4(0.95, 0.7, 1.0, 1.0);

void fragment() {
	float facing_away = 1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0);
	float boil = effect_fbm(UV * vec2(10.0, 5.0) + vec2(t * 2.5, -t * 1.2));
	float edge = facing_away + (boil - 0.5) * 0.55;
	// Eaten away at the silhouette, so the ball's outline flickers like flame instead of a clean circle.
	if (edge > 0.93) {
		discard;
	}
	float rim = smoothstep(0.5, 0.9, edge);
	float hot = smoothstep(0.78, 0.9, edge) * (1.0 - smoothstep(0.9, 0.93, edge));
	ALBEDO = mix(mix(core_color.rgb, rim_color.rgb, rim), hot_color.rgb, hot * 0.7);
}
