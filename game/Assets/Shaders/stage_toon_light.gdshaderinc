// Toon lighting for stages (ADR-0030): light falls into a few hard bands instead of a smooth ramp, so the stage reads
// as drawn. Point lights band with distance too, giving pools of light round torches and lamps.
uniform float toon_bands : hint_range(1.0, 6.0, 1.0) = 3.0;
uniform float toon_softness : hint_range(0.0, 0.5) = 0.06;

vec3 toon_diffuse(vec3 normal, vec3 light, float attenuation, vec3 light_color) {
	float lit = clamp(dot(normal, light), 0.0, 1.0) * attenuation;
	float scaled = lit * toon_bands;
	float band = floor(scaled) + smoothstep(1.0 - toon_softness, 1.0, fract(scaled));
	return light_color * band / toon_bands / PI;
}
