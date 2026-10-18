// Render modes can't be uniforms, so double-sided parts (skirts, hair strands) get their own shader.
shader_type spatial;
render_mode unshaded, cull_disabled, fog_disabled;

#include "res://Assets/Shaders/toon.gdshaderinc"
