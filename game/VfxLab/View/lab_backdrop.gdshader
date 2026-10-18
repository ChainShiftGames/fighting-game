// The VFX Lab's backdrops (dev only): a busy daylight stage, night, flat grey, black, white, a checkerboard that shows
// how far an effect bends what is behind it, or the stage art file's picture. Opaque, so effect cards that read the
// screen see it. World pixels: 70 per metre, floor at y = 0.
shader_type spatial;
render_mode unshaded, cull_disabled;

#include "res://Assets/Effects/effect_noise.gdshaderinc"

uniform int mode = 0;
// The super flash: the stage drops into shadow.
uniform float dim = 0.0;
uniform sampler2D picture : source_color, filter_linear_mipmap;
// Where the picture stands, in metres: left, bottom, width, height.
uniform vec4 picture_rect = vec4(-8.0, -1.0, 16.0, 9.0);

varying vec2 world;

void vertex() {
	world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xy * 70.0;
}

vec3 day(vec2 w) {
	if (w.y < 0.0) {
		return vec3(0.31, 0.3, 0.36);
	}
	vec3 sky = mix(vec3(0.62, 0.78, 0.76), vec3(0.86, 0.93, 0.92), clamp(w.y / 240.0, 0.0, 1.0));
	float fall = smoothstep(0.55, 0.8, effect_noise(vec2(w.x * 0.08, w.y * 0.004)));
	sky = mix(sky, vec3(0.95, 0.98, 0.98), fall * 0.6);
	float rock = smoothstep(0.52, 0.56, effect_fbm(w * 0.012 + vec2(3.0, 1.0)) + (60.0 - w.y) * 0.002);
	vec3 stone = mix(vec3(0.16, 0.22, 0.21), vec3(0.28, 0.36, 0.34), effect_fbm(w * 0.06));
	return mix(sky, stone, rock);
}

vec3 night(vec2 w) {
	if (w.y < 0.0) {
		return vec3(0.07, 0.06, 0.09);
	}
	vec3 sky = mix(vec3(0.05, 0.05, 0.1), vec3(0.12, 0.1, 0.2), clamp(w.y / 260.0, 0.0, 1.0));
	float hills = smoothstep(0.02, 0.0, w.y / 260.0 - 0.25 - 0.12 * effect_fbm(vec2(w.x * 0.01, 1.0)));
	return mix(sky, vec3(0.04, 0.04, 0.06), hills);
}

vec3 checker(vec2 w) {
	vec2 cell = floor(w / 20.0);
	float on = mod(cell.x + cell.y, 2.0);
	return w.y < 0.0 ? vec3(0.3) : mix(vec3(0.08), vec3(0.92), on);
}

vec3 stage_picture(vec2 w) {
	vec2 uv = (w / 70.0 - picture_rect.xy) / picture_rect.zw;
	if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) {
		return night(w);
	}
	return texture(picture, vec2(uv.x, 1.0 - uv.y)).rgb;
}

void fragment() {
	vec3 c;
	if (mode == 1) {
		c = night(world);
	} else if (mode == 2) {
		c = world.y < 0.0 ? vec3(0.28) : vec3(0.5);
	} else if (mode == 3) {
		c = world.y < 0.0 ? vec3(0.04) : vec3(0.0);
	} else if (mode == 4) {
		c = world.y < 0.0 ? vec3(0.8) : vec3(1.0);
	} else if (mode == 5) {
		c = checker(world);
	} else if (mode == 6) {
		c = stage_picture(world);
	} else {
		c = day(world);
	}
	ALBEDO = mix(c, c * 0.18, dim);
}
