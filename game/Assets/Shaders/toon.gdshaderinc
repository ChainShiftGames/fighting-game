// Toon look (P4-B), aiming at an E's Laf++-style illustration: a light fixed to the character instead of the scene's
// lights, one border between a lit and a shade tone that is roughened into dry-brush strokes, a shade tone painted
// from the texture's own colour, and flat colour with little texture detail. VRoid's MToon values (shift moves the
// border, toony its softness) carry over per material; the look settings below apply on top.

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
// How much of the shade tone is the texture's own colour, darkened and saturated (painted shading), instead of the
// texture times the shade colours alone, which greys it.
uniform float shade_from_texture : hint_range(0.0, 1.0) = 0.7;
// Mipmap bias on the texture: washes fine painted detail out, so parts read as flat fills.
uniform float texture_detail_bias : hint_range(0.0, 4.0) = 2.5;
// Dry-brush border: how far stroke noise moves the shade border, and the strokes' size (per model unit, so tune it
// per model). The noise sticks to the model, so the strokes don't swim as it moves.
uniform float brush_strength : hint_range(0.0, 1.0) = 0.55;
uniform float brush_scale = 18.0;
// A rough patch of light tone where the surface faces the light most (lit dot product above this).
uniform float highlight_start : hint_range(0.0, 1.0) = 0.9;
uniform vec4 highlight_color : source_color = vec4(1.0, 0.97, 0.92, 1.0);

// Set by the stage (FighterStageLight), neutral by default: its light's colour on the lit and shade tones, and a rim
// of its sky colour round the silhouette, so the fighters sit in the stage while keeping their own fixed light.
global uniform vec4 stage_light_tint;
global uniform vec4 stage_shade_tint;
global uniform vec4 stage_rim_color;
// Set by a glowing effect near the fighters (EffectLighting), off by default: where it is (xyz) and how far it
// reaches (w, 0 = no light), and its colour. Its side of the fighter catches it in one hard band, plus a rim.
global uniform vec4 effect_light_position;
global uniform vec4 effect_light_color;
// Rim width: where the surface turns this far from the camera (0 = facing it, 1 = edge-on) the rim starts.
uniform float rim_start : hint_range(0.0, 1.0) = 0.55;
// Set by LookToggles (BrushShading): 1 = dry-brush strokes and highlight patches, 0 = a clean border.
global uniform float fighter_brush;

varying vec3 model_position;

void vertex() {
	model_position = VERTEX;
}

#include "res://Assets/Shaders/brush_noise.gdshaderinc"

// Strokes stretched along a diagonal, as a brush drags, two sizes for a ragged edge.
float brush_strokes(vec3 position) {
	vec3 stretched = vec3(position.x + position.y * 0.6, position.y * 0.25, position.z + position.y * 0.6) * brush_scale;
	return value_noise(stretched) * 0.65 + value_noise(stretched * 3.1 + 7.0) * 0.35;
}

void fragment() {
	vec4 texel = texture(lit_texture, UV, texture_detail_bias);
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
	float facing_light = dot(world_normal, world_light) + shading_shift;
	float stroke = (brush_strokes(model_position) - 0.5) * brush_strength * fighter_brush;
	float shading = facing_light + stroke;
	// fwidth keeps the hard border one pixel soft, so it doesn't stair-step.
	float softness = max(mix(1.0 - shading_toony, 0.0, shade_hardness), fwidth(shading));
	float lit = smoothstep(-softness, softness, shading);
	vec3 shade_colours = shade_tint.rgb * stage_shade_tint.rgb;
	// The texture is multiplied in again below, so this squares it: darker and more saturated, as a painter shades.
	vec3 painted_shade = clamp(texel.rgb * 1.35, 0.0, 1.0) * shade_colours;
	vec3 shade_tone = mix(shade_color.rgb * shade_colours, painted_shade, shade_from_texture);
	vec3 tone = mix(shade_tone, lit_color.rgb * stage_light_tint.rgb, lit);
	float over_highlight = facing_light + (stroke * 1.4) - highlight_start;
	float highlight_width = fwidth(over_highlight);
	float highlight = smoothstep(-highlight_width, highlight_width, over_highlight) * lit * fighter_brush;
	float rim = smoothstep(rim_start, 1.0, 1.0 - abs(view_normal.z));
	vec3 world_position = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	vec3 to_effect = effect_light_position.xyz - world_position;
	float reach = effect_light_position.w;
	float near = reach > 0.0 ? pow(1.0 - clamp(length(to_effect) / reach, 0.0, 1.0), 2.0) : 0.0;
	float towards = smoothstep(0.1, 0.2, dot(world_normal, normalize(to_effect + vec3(0.0, 0.0, 1e-4))));
	vec3 effect = effect_light_color.rgb * near * ((0.8 * towards) + rim);
	vec3 colour = texel.rgb * (tone + (stage_rim_color.rgb * rim) + effect);
	ALBEDO = mix(colour, mix(colour, highlight_color.rgb, 0.22) * 1.06, highlight);
	ALPHA = texel.a * lit_color.a;
	ALPHA_SCISSOR_THRESHOLD = alpha_cutoff;
}
