// The fire orb's particles: camera-facing quads in a MultiMesh, each one's whole life worked out from the clock `t`
// and its instance number, so nothing is simulated and a rolled-back frame redraws exactly. `kind` picks the
// behaviour, one material per layer:
//   0 trail embers   - sparks shed off the surface, streaking back and floating up
//   1 trail flames   - soft fire puffs boiling off the core and stretching into the trail
//   2 trail smoke    - dark soot rising off the end of the trail
//   3 orbit sparks   - hot motes circling the core on tilted orbits, in front of it and behind it
//   4 burst embers   - sparks thrown out of an explosion, slowed by the air and falling
//   5 burst flames   - fireballs rolling out of an explosion, cooling as they swell
//   6 burst smoke    - soot left hanging after an explosion, rising
//   7 inward motes   - embers spiralling in to the centre (the charge)
//   8 arc flames     - flame tongues licking up off the floor fire's crescent, where it has caught
//   9 arc embers     - sparks spat off the burning crescent every way, many jumping toward the camera
//  10 arc smoke      - soot rising off the burning crescent
//  11 arc ignition   - a spray of sparks thrown up along the crescent as the sweep lights it (once)
// Positions are in the node's metres, flying +X; the node mirrors and scales them. The arc kinds (8 to 11) start on
// the crescent as the floor fire lays it down (arc_point), from the floor at the node's origin, +Z toward the camera.
shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_draw_never, cull_disabled, fog_disabled, skip_vertex_transform;

#include "res://Assets/Effects/fire.gdshaderinc"

uniform int kind = 0;
// The instances in the MultiMesh; `amount` (0 to 1) is the share of them shown.
uniform int count = 64;
uniform float amount : hint_range(0.0, 1.0) = 1.0;
// Size and opacity multipliers for the layer.
uniform float size_scale : hint_range(0.0, 4.0) = 1.0;
uniform float strength : hint_range(0.0, 3.0) = 1.0;
// How fast bursts fly out, as a multiplier.
uniform float burst_speed : hint_range(0.0, 3.0) = 1.0;
// Seconds from the part's first frame; looping layers only show particles born after 0, so trails build up.
uniform float t = 0.0;
// The floor fire's crescent: metres across, its tilt up off the floor toward the camera (radians), and how long
// the sweep takes to light it end to end; particles only start where it has caught.
uniform float arc_size : hint_range(0.1, 4.0) = 1.8;
uniform float arc_tilt : hint_range(0.0, 1.5) = 0.75;
uniform float sweep_seconds : hint_range(0.01, 1.0) = 0.13;

const float RING = 0.3;

varying vec4 v_colour;
varying vec2 v_corner;
varying float v_seed;
varying float v_age;

vec3 sphere_dir(float a, float b) {
	float z = a * 2.0 - 1.0;
	float angle = b * TAU;
	float s = sqrt(max(1.0 - z * z, 0.0));
	return vec3(s * cos(angle), s * sin(angle), z);
}

// A point on the floor fire's crescent, `s` 0 to 1 from its far horn round its back to its near one and `across`
// -1 to 1 through its width (narrowing to the horns). Matches fire_crescent.gdshader's shape as the art node lays
// it down: turned to open back toward the fighter, then tipped from upright to arc_tilt off the floor.
vec3 arc_point(float s, float across) {
	float angle = mix(0.6, TAU - 0.6, s);
	float r = 0.39 + across * 0.07 * sin(s * 3.14159);
	vec2 q = vec2(0.03 + r * cos(angle), r * sin(angle));
	return arc_size * vec3(-q.x, q.y * sin(arc_tilt), -q.y * cos(arc_tilt));
}

float arc_swept(float seconds) {
	return clamp(seconds / sweep_seconds, 0.0, 1.0);
}

// For a looping particle: its age in seconds (negative if not born yet) and its life share u.
vec2 looping(float h, float life) {
	float u = fract(t / life + h);
	float age = u * life;
	return vec2(t - age < 0.0 ? -1.0 : age, u);
}

// The particle's position at `age` seconds (for the streak's tail it's asked again a moment earlier).
vec3 place(int k, vec4 h, float age, float life) {
	float a = age;
	vec3 dir = sphere_dir(h.x, h.y);
	if (k == 0) {
		vec3 start = dir * RING * 0.95;
		vec3 v = vec3(-(1.6 + 2.2 * h.z) * trail, (h.w - 0.35) * 1.2, (h.z - 0.5) * 1.0);
		return start + v * a + vec3(0.0, 1.3 * a * a, 0.0) + vec3(0.0, sin(a * 22.0 + h.x * 9.0) * 0.03, 0.0);
	} else if (k == 1) {
		vec3 start = dir * RING * 0.55 * h.w;
		vec3 v = vec3(-(1.1 + 1.0 * h.z) * trail, (h.y - 0.4) * 0.5, (h.x - 0.5) * 0.4);
		return start + v * a + vec3(0.0, 0.9 * a * a, 0.0);
	} else if (k == 2) {
		vec3 start = vec3(-RING * (1.0 + 1.5 * h.z) * trail, (h.y - 0.5) * RING, (h.x - 0.5) * RING);
		vec3 v = vec3(-0.9 * trail, 0.45 + 0.4 * h.w, 0.0);
		return start + v * a;
	} else if (k == 3) {
		float radius = RING * (1.15 + 0.55 * h.z);
		float angle = h.y * TAU + t * (5.0 + 4.0 * h.w) * (h.x < 0.5 ? 1.0 : -1.0);
		vec3 orbit = vec3(cos(angle), sin(angle), 0.0) * radius;
		// Each orbit tilted about X then Y, so some pass in front of the core and some behind.
		float tilt = (h.x - 0.5) * 2.6;
		orbit = vec3(orbit.x, orbit.y * cos(tilt), orbit.y * sin(tilt));
		float yaw = (h.w - 0.5) * 1.4;
		return vec3(orbit.x * cos(yaw) + orbit.z * sin(yaw), orbit.y, -orbit.x * sin(yaw) + orbit.z * cos(yaw));
	} else if (k == 4) {
		float speed = (2.5 + 5.5 * h.z * h.z) * burst_speed;
		// Air drag: the distance flown approaches speed / 3.5.
		float flown = speed * (1.0 - exp(-3.5 * a)) / 3.5;
		return dir * (RING * 0.3 + flown) - vec3(0.0, 2.2 * a * a, 0.0);
	} else if (k == 5) {
		float speed = (1.2 + 2.0 * h.z) * burst_speed;
		float flown = speed * (1.0 - exp(-6.0 * a)) / 6.0;
		return dir * (RING * 0.2 + flown) + vec3(0.0, 0.5 * a * a, 0.0);
	} else if (k == 6) {
		float flown = (0.6 + 0.6 * h.z) * burst_speed * (1.0 - exp(-4.0 * a)) / 4.0;
		return dir * (RING * 0.5 + flown) * vec3(1.0, 0.7, 1.0) + vec3(0.0, 0.55 * a, 0.0);
	} else if (k == 8) {
		vec3 start = arc_point(h.x, h.z * 2.0 - 1.0);
		vec3 v = vec3((h.w - 0.5) * 0.35, 0.9 + 1.1 * h.y, (h.z - 0.5) * 0.25);
		return start + v * a + vec3(sin(a * 9.0 + h.y * 20.0) * 0.04, 0.6 * a * a, 0.0);
	} else if (k == 9) {
		// Every way, more toward the camera than away: some jump well out of the floor's plane.
		vec3 start = arc_point(h.x, h.z * 2.0 - 1.0);
		vec3 v = vec3((h.y - 0.5) * 3.0, 1.6 + 3.4 * h.z * h.z, mix(-1.2, 4.2, h.w * h.w));
		float drag = (1.0 - exp(-1.6 * a)) / 1.6;
		return start + v * drag - vec3(0.0, 3.2 * a * a, 0.0);
	} else if (k == 10) {
		vec3 start = arc_point(h.x, h.z * 2.0 - 1.0) + vec3(0.0, 0.15, 0.0);
		vec3 v = vec3((h.w - 0.5) * 0.3, 0.55 + 0.45 * h.y, (h.z - 0.5) * 0.2);
		return start + v * a;
	} else if (k == 11) {
		vec3 start = arc_point(h.x, h.z * 2.0 - 1.0);
		vec3 v = vec3((h.y - 0.5) * 5.0, 2.5 + 5.0 * h.z, mix(-2.0, 6.5, h.w)) * burst_speed;
		float drag = (1.0 - exp(-3.0 * a)) / 3.0;
		return start + v * drag - vec3(0.0, 2.6 * a * a, 0.0);
	}
	// 7: spiral in from far away, faster as it closes.
	float u = clamp(a / life, 0.0, 1.0);
	float r = mix(RING * (2.6 + 2.2 * h.z), RING * 0.15, u * u);
	float angle = h.y * TAU + u * (2.5 + 2.0 * h.w);
	return vec3(cos(angle) * r, sin(angle) * r, (h.x - 0.5) * r * 0.8);
}

void vertex() {
	vec4 h = vec4(
		effect_hash(vec2(float(INSTANCE_ID), 1.7)),
		effect_hash(vec2(float(INSTANCE_ID), 8.3)),
		effect_hash(vec2(float(INSTANCE_ID), 4.9)),
		effect_hash(vec2(float(INSTANCE_ID), 6.1)));
	v_seed = h.x;
	v_corner = VERTEX.xy;

	float life;
	float age;
	float u;
	bool streak = false;
	float size;
	float heat;
	float alpha;
	bool looping_kind = kind <= 3 || (kind >= 7 && kind <= 10);
	bool ember = kind == 0 || kind == 4 || kind == 9 || kind == 11;
	bool puff = kind == 1 || kind == 5 || kind == 8;
	bool smoke = kind == 2 || kind == 6 || kind == 10;
	if (kind == 0) { life = 0.35 + 0.45 * h.w; }
	else if (kind == 1) { life = 0.28 + 0.3 * h.w; }
	else if (kind == 2) { life = 0.7 + 0.6 * h.w; }
	else if (kind == 3) { life = 0.5 + 0.5 * h.z; }
	else if (kind == 4) { life = 0.3 + 0.55 * h.w; }
	else if (kind == 5) { life = 0.25 + 0.3 * h.w; }
	else if (kind == 6) { life = 0.6 + 0.5 * h.w; }
	else if (kind == 8) { life = 0.3 + 0.35 * h.w; }
	else if (kind == 9) { life = 0.6 + 0.7 * h.y; }
	else if (kind == 10) { life = 0.8 + 0.7 * h.w; }
	else if (kind == 11) { life = 0.5 + 0.6 * h.y; }
	else { life = 0.35 + 0.35 * h.w; }

	if (looping_kind) {
		vec2 l = looping(effect_hash(vec2(float(INSTANCE_ID), 3.3)), life);
		age = l.x;
		u = l.y;
	} else if (kind == 11) {
		// Each spark leaves its spot on the arc as the sweep reaches it.
		age = t - h.x * sweep_seconds;
		u = age / life;
	} else {
		age = t;
		u = t / life;
	}
	bool shown = float(INSTANCE_ID) < amount * float(count) && age >= 0.0 && u <= 1.0;
	if (kind >= 8 && kind <= 10) {
		// Only off the part of the arc that had caught when it was born.
		shown = shown && h.x <= arc_swept(t - age);
	}

	if (ember) {
		streak = true;
		size = (0.014 + 0.022 * h.z) * (1.0 - 0.6 * u);
		heat = mix(1.0, 0.35, u * u);
		alpha = (1.0 - u) * smoothstep(0.0, 0.05, u);
	} else if (puff) {
		size = RING * mix(0.55, 1.35, sqrt(u)) * (0.7 + 0.6 * h.z);
		heat = mix(1.05, 0.1, u);
		alpha = smoothstep(0.0, 0.12, u) * (1.0 - smoothstep(0.4, 0.9, u)) * 0.75;
	} else if (smoke) {
		size = RING * mix(0.6, 1.8, sqrt(u)) * (0.7 + 0.6 * h.z);
		heat = 0.12 * (1.0 - u);
		alpha = smoothstep(0.0, 0.25, u) * (1.0 - smoothstep(0.4, 1.0, u)) * 0.55;
	} else if (kind == 3) {
		size = 0.02 + 0.02 * h.w;
		heat = 0.85 + 0.15 * sin(t * 30.0 + h.x * 20.0);
		alpha = smoothstep(0.0, 0.1, u) * (1.0 - smoothstep(0.85, 1.0, u));
		streak = true;
	} else {
		streak = true;
		size = 0.015 + 0.02 * h.z;
		heat = mix(0.6, 1.0, u);
		alpha = smoothstep(0.0, 0.2, u);
	}
	v_age = u;

	vec3 here = place(kind, h, age, life);
	vec3 centre = (MODELVIEW_MATRIX * vec4(here, 1.0)).xyz;
	float world_scale = length(MODEL_MATRIX[1].xyz);
	float s = size * size_scale * world_scale * (shown ? 1.0 : 0.0);
	vec2 offset;
	if (streak) {
		// Stretched along the way it's moving on screen: from where it was a moment ago to where it is.
		vec3 before = (MODELVIEW_MATRIX * vec4(place(kind, h, max(age - 0.03, 0.0), life), 1.0)).xyz;
		vec2 d = centre.xy - before.xy;
		// The tail at most a few sizes long, so fast motes read as sparks, not lines.
		float len = min(length(d), s * 6.0);
		d = length(d) > 1e-5 ? normalize(d) * len : d;
		vec2 along = len > 1e-5 ? d / len : vec2(1.0, 0.0);
		vec2 side = vec2(-along.y, along.x);
		offset = along * VERTEX.x * (len * 1.4 + s) + side * VERTEX.y * s - d * 0.5;
		offset *= shown ? 1.0 : 0.0;
	} else {
		offset = fire_rotate(VERTEX.xy, h.y * TAU + t * (h.z - 0.5) * 3.0) * s;
		if (kind == 8) {
			// Tongues, not puffs: small, upright and tall, so the crescent's shape still reads through them.
			offset = vec2(VERTEX.x * 0.45, VERTEX.y * 1.1) * s;
		}
	}
	VERTEX = centre + vec3(offset, 0.0);

	vec3 colour = fire_ramp(heat);
	if (smoke) {
		// Soot: dark grey and covering, lit faintly red by the fire only while young.
		colour = mix(vec3(0.16, 0.14, 0.14), colour, (1.0 - u) * 0.5);
		v_colour = vec4(colour, 1.0) * alpha * strength;
	} else if (puff) {
		// Fire: mostly light added, a little cover so it reads on a bright stage.
		v_colour = vec4(colour * (1.0 + heat), 0.35) * alpha * strength;
	} else {
		// Sparks: pure light, bright enough to bloom.
		v_colour = vec4(colour * (1.5 + 2.0 * heat), 0.0) * alpha * strength;
	}
}

void fragment() {
	float r = length(v_corner) * 2.0;
	float shape;
	if (kind == 1 || kind == 5 || kind == 8 || kind == 2 || kind == 6 || kind == 10) {
		// A ragged puff: a soft disc eaten into by noise that churns as it ages.
		float n = effect_fbm(v_corner * 3.5 + vec2(v_seed * 17.0, v_seed * 5.0 - v_age * 2.0));
		shape = smoothstep(0.25, 0.75, (1.0 - r) * (0.5 + n * 1.1));
		// Fire puffs burn hottest in the middle.
		if (kind == 1 || kind == 5 || kind == 8) {
			shape *= 0.6 + 0.6 * n;
		}
	} else {
		shape = exp(-r * r * 6.0) + exp(-r * r * 40.0);
	}
	vec4 c = v_colour * clamp(shape, 0.0, 2.0);
	ALBEDO = c.rgb;
	ALPHA = clamp(c.a, 0.0, 1.0);
}
