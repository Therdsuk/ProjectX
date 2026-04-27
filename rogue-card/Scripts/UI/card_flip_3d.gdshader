shader_type canvas_item;

// Rotation angle around the Y axis (0 = face-up, PI = face-down)
uniform float rotation_y : hint_range(0.0, 3.14159265) = 0.0;
// Card center in canvas/global space — set from C# before animating
uniform vec2 card_center_global = vec2(0.0, 0.0);
// Perspective strength (higher = more dramatic perspective)
uniform float perspective_strength : hint_range(0.0, 0.005) = 0.0015;

void vertex() {
    // 1. Transform local vertex to canvas (global 2D) space
    vec4 world_pos = MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0);

    // 2. Offset from card center
    float dx = world_pos.x - card_center_global.x;
    float dy = world_pos.y - card_center_global.y;

    // 3. Rotate around Y axis
    float cos_a = cos(rotation_y);
    float sin_a = sin(rotation_y);
    float x_rotated = dx * cos_a;
    float z = -dx * sin_a; // depth into screen

    // 4. Apply perspective projection
    float persp = 1.0 / (1.0 + z * perspective_strength);
    float new_x = x_rotated * persp + card_center_global.x;
    float new_y = dy * persp + card_center_global.y;

    // 5. Transform back to local space
    vec4 new_world = vec4(new_x, new_y, 0.0, 1.0);
    VERTEX = (inverse(MODEL_MATRIX) * new_world).xy;
}
