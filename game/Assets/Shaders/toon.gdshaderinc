// Toon look (P4-B), aiming at Guilty Gear Xrd: a light fixed to the character instead of the scene's lights, one hard
// border between a lit and a shade tone, and a deep, cool shade colour. VRoid's MToon values (shift moves the border,
// toony its softness) carry over per material; the look settings below apply on top.

uniform sampler2D lit_texture : source_color, filter_linear_mipmap, repeat_enable;
uniform vec4 lit_color : source_color = vec4(1.0);
uniform vec4 shade_color : source_color = vec4(0.8, 0.75, 0.85, 1.0);
uniform float shading_shift : hint_range(-1.0, 1.0) = 0.0;
uniform float shading_toony : hint_range(0.0, 1.0) = 0.95;
// Towards the light, in the model's own space (faces +Z, up +Y, the camera's side -X once in a match), so it turns
// and mirrors with the fighter: both sides are lit the same way. Above and in front, only a little from the camera's
// side, so a clear band of shade shows on the side the camera sees.
uniform vec3 light_direction = vec3(-0.15, 0.6, 0.8);
uniform float alpha_cutoff : hint_range(0.0, 1.0) = 0.5;

// Look settings (developer settings, ADR-0008): 1 = a single hard border, 0 = MToon's own softness.
uniform float shade_hardness : hint_range(0.0, 1.0) = 1.0;
// Multiplies the shade tone: deeper and cooler than VRoid's near-white shade, so the two tones read as flat colours.
uniform vec4 shade_tint : source_color = vec4(0.72, 0.64, 0.84, 1.0);
// Bends normals towards the camera, so a face's shading reads flat instead of round (Xrd edits face normals).
uniform float normal_flatten : hint_range(0.0, 1.0) = 0.0;

void fragment() {
	vec4 texel = texture(lit_texture, UV);
	// Culled parts only ever show their outside, but a mirrored fighter (facing left) reports it as a back face, so
	// don't ask; double-sided parts (hair strands, skirts) show both sides and FRONT_FACING tells them apart.
#ifdef SINGLE_SIDED
	bool outside = true;
#else
	bool outside = FRONT_FACING;
#endif
	vec3 view_normal = normalize(mix(outside ? NORMAL : -NORMAL, vec3(0.0, 0.0, 1.0), normal_flatten));
	vec3 world_normal = normalize((INV_VIEW_MATRIX * vec4(view_normal, 0.0)).xyz);
	vec3 world_light = normalize((MODEL_MATRIX * vec4(light_direction, 0.0)).xyz);
	float shading = dot(world_normal, world_light) + shading_shift;
	// fwidth keeps the hard border one pixel soft, so it doesn't stair-step.
	float softness = max(mix(1.0 - shading_toony, 0.0, shade_hardness), fwidth(shading));
	float lit = smoothstep(-softness, softness, shading);
	ALBEDO = texel.rgb * mix(shade_color.rgb * shade_tint.rgb, lit_color.rgb, lit);
	ALPHA = texel.a * lit_color.a;
	ALPHA_SCISSOR_THRESHOLD = alpha_cutoff;
}
