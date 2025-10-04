#version 440 core

centroid out vec2 v_texCoord;

// we only draw 1 triangle. this is the shortest vtx shader i could come up with lol

void main(void) {
    gl_Position = vec4(-1.0, -1.0, 0.0, 1.0);
    v_texCoord = vec2(0.0, 0.0);
    
    // THIS IS SHORTER IN SOURCE but dynamically indexing variables generates horrible code so don't do it
    /*if (gl_VertexID != 0) {
        gl_Position[gl_VertexID - 1] = 3;
        v_texCoord[gl_VertexID - 1] = 2;
    }*/
    if (gl_VertexID == 1) {
        gl_Position.x = 3;
        v_texCoord.x = 2;
    } else if (gl_VertexID == 2) {
        gl_Position.y = 3;
        v_texCoord.y = 2;
    }
}