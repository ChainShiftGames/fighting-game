// Stone courses for the castle hall: offset rows of blocks, each a little lighter or darker, with mortar lines.

float stone_hash(vec2 cell) {
	cell = fract(cell * vec2(123.34, 456.21));
	cell += dot(cell, cell + 45.32);
	return fract(cell.x * cell.y);
}

// position: metres along and up the surface.
vec3 stone_courses(vec2 position, vec2 block_size, vec3 stone, vec3 mortar, float variation) {
	float row = floor(position.y / block_size.y);
	float along = position.x / block_size.x + mod(row, 2.0) * 0.5;
	vec2 cell = vec2(floor(along), row);
	vec2 inside = vec2(fract(along) * block_size.x, fract(position.y / block_size.y) * block_size.y);
	float edge = min(min(inside.x, block_size.x - inside.x), min(inside.y, block_size.y - inside.y));
	float shade = 1.0 + variation * (stone_hash(cell) * 2.0 - 1.0);
	return mix(stone * shade, mortar, 1.0 - smoothstep(0.012, 0.028, edge));
}
