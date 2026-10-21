// Smooth 3D value noise for the toon look's brush strokes and ink wobble (toon.gdshaderinc, toon_outline.gdshader).
// Hash-based, so it needs no noise texture.

float brush_hash(vec3 position) {
	position = fract(position * 0.3183099 + 0.1);
	position *= 17.0;
	return fract(position.x * position.y * position.z * (position.x + position.y + position.z));
}

float value_noise(vec3 position) {
	vec3 cell = floor(position);
	vec3 blend = fract(position);
	blend = blend * blend * (3.0 - 2.0 * blend);
	float near_face = mix(
		mix(brush_hash(cell), brush_hash(cell + vec3(1.0, 0.0, 0.0)), blend.x),
		mix(brush_hash(cell + vec3(0.0, 1.0, 0.0)), brush_hash(cell + vec3(1.0, 1.0, 0.0)), blend.x),
		blend.y);
	float far_face = mix(
		mix(brush_hash(cell + vec3(0.0, 0.0, 1.0)), brush_hash(cell + vec3(1.0, 0.0, 1.0)), blend.x),
		mix(brush_hash(cell + vec3(0.0, 1.0, 1.0)), brush_hash(cell + vec3(1.0, 1.0, 1.0)), blend.x),
		blend.y);
	return mix(near_face, far_face, blend.z);
}
