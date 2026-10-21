// The fire orb's model: its painted texture read as a heat map and recoloured through the fire ramp, so the
// blue-and-red Tripo paint becomes molten rock with white-hot seams. The surface boils (vertices pushed in and out
// by churning noise), lava crawls over it, the side facing the camera burns hottest and the rim cools to deep red
// for the corona card behind to take over. Its own light (no stage lighting, like the effect cards); opaque, so
// particles in front of and behind it sort by depth. Its clock comes per instance from the art node.
shader_type spatial;
render_mode unshaded, cull_back, fog_disabled;

#include "res://Assets/Effects/fire.gdshaderinc"

uniform sampler2D basecolor : source_color, filter_linear_mipmap;

group_uniforms core;
// How far the surface boils in and out, as a share of the model's size, and how fast.
uniform float boil : hint_range(0.0, 0.2) = 0.07;
uniform float boil_speed : hint_range(0.0, 4.0) = 1.6;
// How much the painted texture shapes the heat against the moving lava noise.
uniform float paint : hint_range(0.0, 1.0) = 0.55;
// How fast the heartbeat pulses, in beats per second, and how hard.
uniform float pulse_rate : hint_range(0.0, 6.0) = 2.3;
uniform float pulse : hint_range(0.0, 1.0) = 0.35;
group_uniforms;

// Seconds from the part's first frame, and the extra glow while it forms; set per instance.
instance uniform float t = 0.0;
instance uniform float flare = 0.0;

varying vec3 object_position;

float lava(vec3 p, float time) {
	vec2 a = p.xy * 3.0 + vec2(time * 0.7, -time * 0.4);
	vec2 b = p.zy * 3.0 + vec2(-time * 0.5, time * 0.6);
	return effect_fbm(a + effect_fbm(b) * 1.5);
}

void vertex() {
	object_position = VERTEX;
	float n = effect_fbm(VERTEX.xy * 4.0 + VERTEX.z * 3.0 + vec2(t * boil_speed, -t * boil_speed * 0.7));
	VERTEX += NORMAL * (n - 0.45) * boil;
}

void fragment() {
	vec3 texel = texture(basecolor, UV).rgb;
	float facing = clamp(dot(NORMAL, VIEW), 0.0, 1.0);

	// The paint's red runs hot and its blue cold; the lava noise crawls over both.
	float painted = clamp(texel.r * 1.1 - texel.b * 0.6 + 0.25, 0.0, 1.0);
	float flow = lava(object_position, t);
	float seams = 1.0 - smoothstep(0.0, 0.06, abs(flow - 0.5));
	float heat = mix(flow, painted, paint) * 0.75 + seams * 0.45;

	// The middle facing the camera is white-hot, the limb cools; the heartbeat swells it all.
	float beat = 0.5 + 0.5 * sin(t * pulse_rate * TAU);
	beat = beat * beat * beat;
	heat += pow(facing, 2.5) * 0.45 + beat * pulse * 0.25 - pow(1.0 - facing, 2.0) * 0.35;

	vec3 colour = fire_ramp(heat);
	// A thin hot rim where the surface turns away, so the silhouette never goes dark against the stage.
	colour += flame_color.rgb * pow(1.0 - facing, 4.0) * 1.5;
	ALBEDO = colour + core_color.rgb * flare * 2.0;
}
