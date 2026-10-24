// The floor fire's crescent (the fire_crescent model): the arc a burning leg sweep leaves on the floor. It ignites
// along the arc behind a white-hot leading edge, as fast as the sweep, then burns: flame licks crawl along it, the
// top face heaves up in tongues, the spine of the arc burns hottest and its thin edges flicker away to deep red.
// As it dies it breaks up into guttering patches. Coloured through the fire orb's palette (fire.gdshaderinc), so
// it matches the fireball; its own light, premultiplied like the orb's card. Its clock comes per instance from the
// art node: `t` and `flare` (ProjectileModel), and `burn`, 1 while it burns fully and down to 0 as it dies.
//
// Object space (the model is 1 m across): the crescent stands in XY, opening to +X, thin along Z. Its spine is
// about a circle of radius SPINE_RADIUS round SPINE_CENTRE (fire_orb_particles.gdshader's arc_point follows the
// same circle); the arc runs from the top horn (s = 0) round the back to the bottom horn (s = 1).
shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_draw_never, cull_disabled, fog_disabled;

#include "res://Assets/Effects/fire.gdshaderinc"

group_uniforms crescent;
// How long the ignition takes to run the length of the arc, in seconds (match it to the sweep).
uniform float sweep_seconds : hint_range(0.01, 1.0) = 0.13;
// How high the top face heaves in flame tongues, as a share of the model's size.
uniform float lick_height : hint_range(0.0, 0.4) = 0.12;
// How fast the flame licks crawl along the arc and flicker.
uniform float crawl_speed : hint_range(0.0, 4.0) = 1.5;
// How much of the arc's thin edge burns see-through (0 = solid to the edge).
uniform float ragged : hint_range(0.0, 1.0) = 0.55;
// How much the burning crescent covers what is behind it (the rest is light added on top).
uniform float cover : hint_range(0.0, 1.0) = 0.8;
group_uniforms;

instance uniform float t = 0.0;
instance uniform float flare = 0.0;
instance uniform float burn = 1.0;

const vec2 SPINE_CENTRE = vec2(0.03, 0.0);
const float SPINE_RADIUS = 0.39;
// How far either side of the spine the arc's middle reaches, in model units; it narrows to the horns.
const float HALF_WIDTH = 0.13;
// The horns' angles round the origin, either side of the opening.
const float HORN = 0.6;

varying vec3 object_position;

// How far `p` is inside the crescent, 0 at its edge to 1 on the spine.
float depth_in(vec2 p, float s) {
	float width = HALF_WIDTH * mix(0.35, 1.0, sin(s * 3.14159));
	return clamp(1.0 - abs(length(p - SPINE_CENTRE) - SPINE_RADIUS) / width, 0.0, 1.0);
}

// Where `p` is along the arc: 0 at the top horn, 1 at the bottom one.
float along(vec2 p) {
	float a = atan(p.y, p.x);
	a = a < 0.0 ? a + TAU : a;
	return clamp((a - HORN) / (TAU - 2.0 * HORN), 0.0, 1.0);
}

float swept() {
	return clamp(t / sweep_seconds, 0.0, 1.0);
}

void vertex() {
	object_position = VERTEX;
	// The top face (+Z, up once the art node lays the crescent down) heaves up in tongues where it has caught.
	vec2 p = VERTEX.xy;
	float s = along(p);
	float lit = 1.0 - smoothstep(swept() - 0.04, swept() + 0.02, s);
	float tongue = fire_flicker(vec2(s * 7.0, 0.0), t * crawl_speed);
	float lift = step(0.0, VERTEX.z) * lick_height * (0.25 + tongue) * depth_in(p, s) * lit * burn;
	VERTEX.z += lift;
}

void fragment() {
	vec2 p = object_position.xy;
	float s = along(p);
	float inside = depth_in(p, s);
	float sweep = swept();

	// Caught behind the leading edge, which burns white-hot while it runs.
	float lit = 1.0 - smoothstep(sweep - 0.04, sweep + 0.02, s);
	float front = exp(-pow((s - sweep) / 0.05, 2.0)) * (1.0 - step(1.0, sweep));

	// Flame licks crawling along the arc, out from the spine to the edges.
	float licks = fire_flicker(vec2(s * 9.0, inside * 1.6), t * crawl_speed);
	float embers = effect_fbm(p * 14.0 + vec2(t * 0.6, -t * 0.4));
	float heat = inside * 0.7 + (licks - 0.45) * 0.8 + 0.2;
	heat += front * 0.9 + flare * 0.5 + embers * 0.15;
	// Dying: it cools toward ember red and soot.
	heat *= mix(0.45, 1.0, burn);

	// Thin edges burn see-through where the licks dip; dying, it breaks up into patches round the hottest licks.
	float edge = smoothstep(0.0, ragged + 0.01, inside + (licks - 0.5) * ragged);
	float patches = smoothstep(1.0 - burn - 0.12, 1.0 - burn + 0.12, licks * 0.8 + embers * 0.4);
	float a = clamp(edge * patches * lit + front * 0.5, 0.0, 1.0);

	vec3 colour = fire_ramp(heat);
	ALBEDO = colour * a * (1.4 + flare);
	ALPHA = a * cover * smoothstep(0.1, 0.4, heat);
}
