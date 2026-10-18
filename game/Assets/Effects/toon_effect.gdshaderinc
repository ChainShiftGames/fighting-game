// Toon finish for effects, so they match the toon-shaded characters (P4-B, Guilty Gear Xrd): an effect draws its
// layers as usual, then `toon_finish` turns them into flat palette bands with a hard silhouette and an ink line, and
// `toon_held` steps its clock on 2s or 3s like the characters' poses. Look settings are developer settings
// (ADR-0008); the ranges and groups are the VFX Lab's sliders (ADR-0029). The defaults are the house style, picked from
// the fireball's toon round (Cel Vortex: bold), so a new effect starts on-model. Effect shaders also use
// fog_disabled: a stage's fog would tint the whole card, transparent parts too, and show it as a pale square.

group_uniforms toon;
// Logic frames each drawing is held for: 1 = smooth, 2-3 = on 2s or 3s.
uniform float toon_hold_frames : hint_range(1.0, 4.0, 1.0) = 3.0;
// Flat tones from dark to light, mapped onto the palette ramp below.
uniform float toon_bands : hint_range(2.0, 5.0, 1.0) = 2.0;
// The ramp's middle tone; the ends are the effect's own dark and light colours.
uniform vec4 toon_mid_color : source_color = vec4(0.42, 0.25, 0.6, 1.0);
// Coverage above this is the effect, below is not: one hard silhouette instead of soft fades.
uniform float toon_cut : hint_range(0.05, 0.95) = 0.35;
uniform vec4 ink_color : source_color = vec4(0.06, 0.02, 0.08, 1.0);
// Ink line along the silhouette, in pixels at 1080p; 0 = none.
uniform float ink_pixels : hint_range(0.0, 6.0) = 3.5;
// How far shapes jump between held drawings, as hand-drawn frames never line up exactly ("boil").
uniform float toon_boil : hint_range(0.0, 2.0) = 1.2;
group_uniforms;

const float TOON_FRAMES_PER_SECOND = 60.0;

// A clock in seconds, held on the hold frames.
float toon_held(float seconds) {
	float frames = floor(seconds * TOON_FRAMES_PER_SECOND / toon_hold_frames + 1e-4) * toon_hold_frames;
	return frames / TOON_FRAMES_PER_SECOND;
}

// A clock in logic frames, held the same way.
float toon_held_frames(float frames) {
	return floor(frames / toon_hold_frames + 1e-4) * toon_hold_frames;
}

// A value that grows with the clock (spin, progress), scaled down to the held clock.
float toon_held_with(float value, float seconds) {
	return seconds > 1e-4 ? value * toon_held(seconds) / seconds : value;
}

// The drawing's jitter: a small offset that changes once per held drawing.
vec2 toon_boil_offset(vec2 p, float held_seconds) {
	float drawing = floor(held_seconds * TOON_FRAMES_PER_SECOND / toon_hold_frames + 0.5);
	vec2 cell = floor(p * 6.0);
	float a = fract(sin(dot(cell + drawing * 1.37, vec2(12.9898, 78.233))) * 43758.5453) * 6.2832;
	return vec2(cos(a), sin(a)) * 0.006 * toon_boil;
}

// The palette tone nearest to `colour`: dark and light for 2 bands, plus the mid tone for 3, plus the tones halfway
// between for 4 and 5. Nearest, not by brightness, because colours here are linear and the mid tone is dark.
vec3 toon_tone(vec3 colour, vec3 dark, vec3 light) {
	vec3 mid = toon_mid_color.rgb;
	vec3 tones[5] = vec3[](dark, light, mid, mix(dark, mid, 0.5), mix(mid, light, 0.5));
	vec3 best = dark;
	float best_distance = 1e9;
	for (int i = 0; i < 5; i++) {
		float used = toon_bands < 2.5 ? 2.0 : toon_bands;
		if (float(i) >= used) {
			break;
		}
		vec3 d = colour - tones[i];
		float distance = dot(d, d);
		if (distance < best_distance) {
			best_distance = distance;
			best = tones[i];
		}
	}
	return best;
}

// Turns a premultiplied layer stack into the toon look: each colour onto its nearest palette tone, a hard silhouette
// at `toon_cut`, and ink along it. `viewport_height` scales the ink to the window.
vec4 toon_finish(vec4 acc, vec3 dark, vec3 light, float viewport_height) {
	float a = acc.a;
	vec3 ramp = toon_tone(acc.rgb / max(a, 1e-4), dark, light);
	float pixel = max(fwidth(a), 1e-4);
	float inside = smoothstep(toon_cut - pixel * 0.5, toon_cut + pixel * 0.5, a);
	float ink_width = ink_pixels * viewport_height / 1080.0;
	float ink = ink_width > 0.0 ? 1.0 - smoothstep(ink_width * 0.5, ink_width * 0.5 + 1.0, abs(a - toon_cut) / pixel) : 0.0;
	float cover = max(inside, ink);
	return vec4(mix(ramp, ink_color.rgb, ink / max(cover, 1e-4)) * cover, cover);
}
