// Procedural value noise for effect shaders, so effects need no noise textures.
// Sin-free hash (Dave Hoskins' hash12): fract(sin(x) * 43758) blew up rounding differences between a cell's corner
// and its neighbour's into different values, so the noise jumped at cell borders and showed as blocks.
float effect_hash(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float effect_noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	vec2 u = f * f * (3.0 - 2.0 * f);
	return mix(
		mix(effect_hash(i), effect_hash(i + vec2(1.0, 0.0)), u.x),
		mix(effect_hash(i + vec2(0.0, 1.0)), effect_hash(i + vec2(1.0, 1.0)), u.x),
		u.y);
}

float effect_fbm(vec2 p) {
	float sum = 0.0;
	float amplitude = 0.5;
	for (int octave = 0; octave < 4; octave++) {
		sum += amplitude * effect_noise(p);
		p *= 2.03;
		amplitude *= 0.5;
	}
	return sum;
}
