// Procedural value noise for effect shaders, so effects need no noise textures.
float effect_hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
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
