shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_disabled, diffuse_burley, specular_schlick_ggx;

uniform vec4 water_color : source_color = vec4(0.1, 0.4, 0.8, 0.7);
uniform float wave_speed = 1.0;
uniform float wave_height = 0.05;

void vertex() {
    float time = TIME * wave_speed;
    VERTEX.y += sin(VERTEX.x * 2.0 + time) * wave_height;
    VERTEX.y += cos(VERTEX.z * 1.5 + time * 0.8) * wave_height;
}

void fragment() {
    ALBEDO = water_color.rgb;
    ALPHA = water_color.a;
    ROUGHNESS = 0.1;
    METALLIC = 0.3;
}
