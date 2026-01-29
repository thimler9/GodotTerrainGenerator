#define BORDER_EPISLON 0.001


vec3 ProjectPointOnTangentPlane(vec3 position, vec3 normal, vec3 width) {
    return position + dot(width, normal) * normal;
}

void AdjustPosition(vec3 positionOS, vec3 normalOS, uint chunkSize, float width, uint expandBorders, uint retractBorders, out vec3 adjustedPosition) {
    float dx = 0;
    float dy = 0;
    float dz = 0;

    // Remove borders that shouldn't be rendered ------------------------------

    bool x1 = positionOS.x >= 0;
    bool x4 = positionOS.x <= chunkSize;
    bool y1 = positionOS.y >= 0;
    bool y4 = positionOS.y <= chunkSize;
    bool z1 = positionOS.z >= 0;
    bool z4 = positionOS.z <= chunkSize;

    bool removeVertex = (positionOS.x > chunkSize && (y1 && y4 && z1 && z4) && ((expandBorders >> 5) & 1) == 0) ||
        (positionOS.x < 0 && (y1 && y4 && z1 && z4) && ((expandBorders >> 4) & 1) == 0) ||
        (positionOS.z > chunkSize && (x1 && x4 && y1 && y4) && ((expandBorders >> 3) & 1) == 0) ||
        (positionOS.z < 0 && (x1 && x4 && y1 && y4) && ((expandBorders >> 2) & 1) == 0) ||
        (positionOS.y > chunkSize && (x1 && x4 && z1 && z4) && ((expandBorders >> 1) & 1) == 0) ||
        (positionOS.y < 0 && (x1 && x4 && z1 && z4) && ((expandBorders >> 0) & 1) == 0);

    dx = removeVertex ? -positionOS.x : 0;
    dy = removeVertex ? -positionOS.y : 0;
    dz = removeVertex ? -positionOS.z : 0;

    // Transpose expand borders -----------------------------------------------
    x1 = positionOS.x > 0;
    x4 = positionOS.x < chunkSize;
    y1 = positionOS.y > 0;
    y4 = positionOS.y < chunkSize;
    z1 = positionOS.z > 0;
    z4 = positionOS.z < chunkSize;

    // BORDER_EPISLON is just to account for a bit of floating point error
    dx = (positionOS.x > chunkSize + BORDER_EPISLON + BORDER_EPISLON * 0.001 && !(y1 && y4 && z1 && z4)) ?
        -width : dx;
    dx = (positionOS.x < -BORDER_EPISLON - BORDER_EPISLON * 0.001 && !(y1 && y4 && z1 && z4)) ?
        width : dx;
    dy = (positionOS.y > chunkSize + BORDER_EPISLON + BORDER_EPISLON * 0.001 && !(x1 && x4 && z1 && z4)) ?
        -width : dy;
    dy = (positionOS.y < -BORDER_EPISLON - BORDER_EPISLON * 0.001 && !(x1 && x4 && z1 && z4)) ?
        width : dy;
    dz = (positionOS.z > chunkSize + BORDER_EPISLON + BORDER_EPISLON * 0.001 && !(x1 && x4 && y1 && y4)) ?
        -width : dz;
    dz = (positionOS.z < -BORDER_EPISLON - BORDER_EPISLON * 0.001 && !(x1 && x4 && y1 && y4)) ?
        width : dz;

    // Transpose main chunk vertices ------------------------------------------

    bool x2 = positionOS.x < chunkSize / 2;
    bool x3 = positionOS.x > chunkSize / 2;
    bool y2 = positionOS.y < chunkSize / 2;
    bool y3 = positionOS.y > chunkSize / 2;
    bool z2 = positionOS.z < chunkSize / 2;
    bool z3 = positionOS.z > chunkSize / 2;

    bool retractEast = ((retractBorders >> 5) & 1) == 1 && positionOS.x == chunkSize && ((y1 && y2) || (y3 && y4)) && ((z1 && z2) || (z3 && z4));
    bool retractWest = ((retractBorders >> 4) & 1) == 1 && positionOS.x == 0 && ((y1 && y2) || (y3 && y4)) && ((z1 && z2) || (z3 && z4));
    bool retractTop = ((retractBorders >> 1) & 1) == 1 && positionOS.y == chunkSize && ((x1 && x2) || (x3 && x4)) && ((z1 && z2) || (z3 && z4));
    bool retractBottom = (retractBorders & 1) == 1 && positionOS.y == 0 && ((x1 && x2) || (x3 && x4)) && ((z1 && z2) || (z3 && z4));
    bool retractNorth = ((retractBorders >> 3) & 1) == 1 && positionOS.z == chunkSize && ((x1 && x2) || (x3 && x4)) && ((y1 && y2) || (y3 && y4));
    bool retractSouth = ((retractBorders >> 2) & 1) == 1 && positionOS.z == 0 && ((x1 && x2) || (x3 && x4)) && ((y1 && y2) || (y3 && y4));

    dx = retractEast ?
        -width / 2.0 : dx;
    dx = retractWest ?
        width / 2.0 : dx;

    dy = retractTop ?
        -width / 2.0 : dy;
    dy = retractBottom ?
        width / 2.0 : dy;

    dz = retractNorth ?
        -width / 2.0 : dz;
    dz = retractSouth ?
        width / 2.0 : dz;


    // Transpose vertices back to where they should be
    dx = positionOS.x > chunkSize ? dx - BORDER_EPISLON : dx;
    dx = positionOS.x < 0 ? dx + BORDER_EPISLON : dx;
    dy = positionOS.y > chunkSize ? dy - BORDER_EPISLON : dy;
    dy = positionOS.y < 0 ? dy + BORDER_EPISLON : dy;
    dz = positionOS.z > chunkSize ? dz - BORDER_EPISLON : dz;
    dz = positionOS.z < 0 ? dz + BORDER_EPISLON : dz;

    vec3 transpositions = vec3(dx, dy, dz);
    transpositions = vec3(0, 0, 0);
    vec3 newPosition = positionOS + transpositions;

    bool expandEast = positionOS.x > chunkSize + BORDER_EPISLON + BORDER_EPISLON * 0.001 && (y1 && y4 && z1 && z4) && ((expandBorders >> 5) & 1) == 1;
    bool expandWest = positionOS.x < -BORDER_EPISLON - BORDER_EPISLON * 0.001 && (y1 && y4 && z1 && z4) && ((expandBorders >> 4) & 1) == 1;
    bool expandNorth = positionOS.z > chunkSize + BORDER_EPISLON + BORDER_EPISLON * 0.001 && (x1 && x4 && y1 && y4) && ((expandBorders >> 3) & 1) == 1;
    bool expandSouth = positionOS.z < -BORDER_EPISLON - BORDER_EPISLON * 0.001 && (x1 && x4 && y1 && y4) && ((expandBorders >> 2) & 1) == 1;
    bool expandTop = positionOS.y > chunkSize + BORDER_EPISLON + BORDER_EPISLON * 0.001 && (x1 && x4 && z1 && z4) && ((expandBorders >> 1) & 1) == 1;
    bool expandBottom = positionOS.y < -BORDER_EPISLON - BORDER_EPISLON * 0.001 && (x1 && x4 && z1 && z4) && ((expandBorders >> 0) & 1) == 1;

    // newPosition = expandEast ? ProjectPointOnTangentPlane(newPosition, normalOS, vec3(width, 0, 0)) : newPosition;
    // newPosition = expandWest ? ProjectPointOnTangentPlane(newPosition, normalOS, vec3(-width, 0, 0)) : newPosition;
    // newPosition = expandNorth ? ProjectPointOnTangentPlane(newPosition, normalOS, vec3(0, 0, width)) : newPosition;
    // newPosition = expandSouth ? ProjectPointOnTangentPlane(newPosition, normalOS, vec3(0, 0, -width)) : newPosition;
    // newPosition = expandTop ? ProjectPointOnTangentPlane(newPosition, normalOS, vec3(0, width, 0)) : newPosition;
    // newPosition = expandBottom ? ProjectPointOnTangentPlane(newPosition, normalOS, vec3(0, -width, 0)) : newPosition;

    // newPosition = retractEast ? ProjectPointOnTangentPlane(newPosition, normalOS, vec3(-width / 2.0f, 0, 0)) : newPosition;
    // newPosition = retractWest ? ProjectPointOnTangentPlane(newPosition, normalOS, vec3(width / 2.0f, 0, 0)) : newPosition;
    // newPosition = retractNorth ? ProjectPointOnTangentPlane(newPosition, normalOS, vec3(0, 0, -width / 2.0f)) : newPosition;
    // newPosition = retractSouth ? ProjectPointOnTangentPlane(newPosition, normalOS, vec3(0, 0, width / 2.0f)) : newPosition;
    // newPosition = retractTop ? ProjectPointOnTangentPlane(newPosition, normalOS, vec3(0, -width / 2.0f, 0)) : newPosition;
    // newPosition = retractBottom ? ProjectPointOnTangentPlane(newPosition, normalOS, vec3(0, width / 2.0f, 0)) : newPosition;

    adjustedPosition = newPosition;
}