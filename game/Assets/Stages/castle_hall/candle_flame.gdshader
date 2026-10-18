// A small flame that always faces the camera and flickers; bright enough for the glow to catch it.
shader_type spatial;
render_mode unshaded, cull_disabled;

uniform vec3 core_color : source_color = vec3(1.0, 0.9, 0.6);
uniform vec3 edge_color : source_color = vec3(1.0, 0.45, 0.1);
uniform float brightness = 2.5;
uniform float flicker_speed = 9.0;

varying float flicker;

void vertex() {
	MODELVIEW_MATRIX = VIEW_MATRIX * mat4(INV_VIEW_MATRIX[0], INV_VIEW_MATRIX[1], INV_VIEW_MATRIX[2], MODEL_MATRIX[3]);
	float seed = dot(MODEL_MATRIX[3].xyz, vec3(12.9, 78.2, 37.7));
	flicker = 0.85 + 0.15 * sin(TIME * flicker_speed + seed) * sin(TIME * flicker_speed * 1.7 + seed * 0.5);
}

void fragment() {
	vec2 p = (UV - vec2(0.5, 0.65)) * vec2(2.0, 1.6);
	p.y *= p.y < 0.0 ? 0.6 * flicker : 1.0;
	float shape = 1.0 - length(p);
	if (shape < 0.0) {
		discard;
	}
	ALBEDO = mix(edge_color, core_color, smoothstep(0.1, 0.6, shape)) * brightness * flicker;
}
