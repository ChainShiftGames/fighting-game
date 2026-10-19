// Painted cards for the stage's back and sky layers (ADR-0030): the picture as painted, no lighting, faded by fog
// with distance. Optional ambient motion, all off by default: a slow scroll (mist, clouds), sway with the top edge
// moving and the bottom fixed (foliage, banners), and a brightness flicker (crystals, fire). Motion runs on render
// time, not the logic frame: it is scenery that no gameplay event starts, so rollback never touches it.
shader_type spatial;
render_mode unshaded, cull_disabled;

uniform sampler2D card_texture : source_color, filter_linear_mipmap, repeat_enable;
uniform vec4 tint : source_color = vec4(1.0);
uniform float alpha_cutoff : hint_range(0.0, 1.0) = 0.5;
// Picture widths and heights per second.
uniform vec2 scroll_speed = vec2(0.0);
// Metres the top edge swings either way, and swings per second.
uniform float sway_amount = 0.0;
uniform float sway_speed = 0.5;
// Brightness swing as a share of the picture's own, and how fast it changes.
uniform float flicker_amount : hint_range(0.0, 1.0) = 0.0;
uniform float flicker_speed = 6.0;

void vertex() {
	// Cards placed side by side sway out of step.
	float phase = NODE_POSITION_WORLD.x * 0.7;
	VERTEX.x += sin((TIME * sway_speed * TAU) + phase) * sway_amount * (1.0 - UV.y);
}

void fragment() {
	vec4 texel = texture(card_texture, UV + (scroll_speed * TIME)) * tint;
	// Two sines at an uneven ratio never line up, so the flicker doesn't read as a loop.
	float flicker = 0.5 * (sin(TIME * flicker_speed) + sin((TIME * flicker_speed * 2.37) + 1.3));
	ALBEDO = texel.rgb * (1.0 + (flicker_amount * flicker));
	ALPHA = texel.a;
	ALPHA_SCISSOR_THRESHOLD = alpha_cutoff;
}
