// Paints the stage's key art onto blocked-out shapes from the point it was drawn for (ADR-0030 §3): the round-start
// camera. From there the shapes look exactly like the painting; the camera's sideways travel shows their depth. The
// painting already holds its light, so no lighting here; fog still fades it with distance.
shader_type spatial;
render_mode unshaded;

uniform sampler2D key_art : source_color, filter_linear_mipmap, repeat_disable;
// Developer settings (presentation), in metres: the round-start camera at 16:9 and where the painting's frame lies
// on the gameplay plane (z = 0). The defaults are the camera guide's frame for the current camera settings
// (StageProjectionTests keep them in step). The width follows the picture's shape, centred on art_centre_x, so a
// painting widened evenly on both sides only needs its new file.
uniform vec3 projector_position = vec3(0.0, 1.2377, 7.346);
uniform float art_centre_x = 0.0;
uniform float art_bottom = -0.3909;
uniform float art_height = 3.2571;
uniform float alpha_cutoff : hint_range(0.0, 1.0) = 0.5;

varying vec3 world_position;

void vertex() {
	world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	// Where the line from the projector through this point crosses the gameplay plane.
	vec3 ray = world_position - projector_position;
	vec2 on_plane = projector_position.xy + (ray.xy * projector_position.z / -ray.z);
	vec2 art_size = vec2(textureSize(key_art, 0));
	vec2 frame = vec2(art_height * art_size.x / art_size.y, art_height);
	vec2 uv = (on_plane - vec2(art_centre_x - (frame.x / 2.0), art_bottom)) / frame;
	vec4 texel = texture(key_art, vec2(uv.x, 1.0 - uv.y));
	ALBEDO = texel.rgb;
	ALPHA = texel.a;
	ALPHA_SCISSOR_THRESHOLD = alpha_cutoff;
}
