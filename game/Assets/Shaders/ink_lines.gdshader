// Inner ink lines (InkLines): a full-screen pass that inks where depth or surface direction jumps, so folds, sleeves
// and overlapping limbs get the drawn lines an outline can't give. Only up to a little behind the gameplay plane:
// the fighters are inked and the stage stays painted.
shader_type spatial;
render_mode unshaded, cull_disabled, depth_test_disabled, fog_disabled;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;
uniform sampler2D depth_texture : hint_depth_texture, filter_nearest;
uniform sampler2D normal_texture : hint_normal_roughness_texture, filter_nearest;
// Multiplies what's under the line, so it takes a darker shade of the part.
uniform vec4 line_color : source_color = vec4(0.15, 0.12, 0.15, 1.0);
// Line width in pixels at 1080p.
uniform float line_width_pixels = 1.0;
// Relative depth jump and surface turn (1 - cosine, summed over two neighbours) where a line starts and is solid.
uniform vec2 depth_edge = vec2(0.04, 0.08);
uniform vec2 normal_edge = vec2(0.25, 0.5);
// Set every frame by InkLines: view depth (metres) where the lines start to fade out, and where they're gone.
uniform vec2 inked_depth = vec2(20.0, 26.0);

void vertex() {
	POSITION = vec4(VERTEX.xy * 2.0, 1.0, 1.0);
}

float view_depth(vec2 screen_uv, mat4 inverse_projection) {
	float depth = texture(depth_texture, screen_uv).r;
	vec4 view = inverse_projection * vec4((screen_uv * 2.0) - 1.0, depth, 1.0);
	return -view.z / view.w;
}

vec3 surface_normal(vec2 screen_uv) {
	return (texture(normal_texture, screen_uv).xyz * 2.0) - 1.0;
}

void fragment() {
	vec2 uv = SCREEN_UV;
	vec3 colour = textureLod(screen_texture, uv, 0.0).rgb;
	vec2 step_size = (line_width_pixels * VIEWPORT_SIZE.y / 1080.0) / VIEWPORT_SIZE;
	float depth = view_depth(uv, INV_PROJECTION_MATRIX);
	float depth_jump = abs(view_depth(uv - vec2(step_size.x, 0.0), INV_PROJECTION_MATRIX) - depth)
		+ abs(view_depth(uv + vec2(step_size.x, 0.0), INV_PROJECTION_MATRIX) - depth)
		+ abs(view_depth(uv - vec2(0.0, step_size.y), INV_PROJECTION_MATRIX) - depth)
		+ abs(view_depth(uv + vec2(0.0, step_size.y), INV_PROJECTION_MATRIX) - depth);
	vec3 normal = surface_normal(uv);
	float turn = (1.0 - dot(normal, surface_normal(uv - vec2(step_size.x, 0.0))))
		+ (1.0 - dot(normal, surface_normal(uv - vec2(0.0, step_size.y))));
	float line = max(
		smoothstep(depth_edge.x, depth_edge.y, depth_jump / max(depth, 0.001)),
		smoothstep(normal_edge.x, normal_edge.y, turn));
	line *= 1.0 - smoothstep(inked_depth.x, inked_depth.y, depth);
	ALBEDO = mix(colour, line_color.rgb * colour, line);
}
