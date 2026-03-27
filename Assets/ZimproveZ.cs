using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZimproveZ : MonoBehaviour
{

    // Possible optimizations for meshes:

    // Occulusion culling - 
    // GPU instancing and batching - for objects that are repeated many times, use GPU instancing to reduce draw calls
    // LOD (Level of Detail) - for objects that are far away, use a lower polygon model


    // Already optimizations from unity:

    // Backface culling - nu deseneaza fete care nu se vad
    // Frustum culling - nu deseneaza obiect care nu sunt in fov
    // Depth testing (z buffer) - gpu z buffer pentru a nu desena obiecte care sunt acoperite de altele
}
