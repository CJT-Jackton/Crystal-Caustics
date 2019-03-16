# Crystal Caustics

This project approximate caustics of crystal in real-time using the physically accurated caustics rendered offline.

![thumbnail](https://raw.githubusercontent.com/CJT-Jackton/Crystal-Caustics/master/screenshots/crystal_render_by_Cinema4D.jpg "Crystal render by Cinema4D")
> *Caustics of crystal (render by Cinema4D)*

Caustics is a common optical phenomenon that light ray reflected or refracted by an object and created a pattern of light. It usually required track the path of light ray go through the object. However ray tracing is too computationally expensive and can't be done in real-time.

## Methodology

For each object that has caustics when illuminated, surround it with a mesh bounding box. In the demo I used a [icosahedron](https://en.wikipedia.org/wiki/Icosahedron), but it can be alter to any convex shape. Each vertices of the bounding mesh mapping to one cubemap storing the caustics light.

1. Render the caustics of target object using [ray tracing](https://github.com/CJT-Jackton/RayTracing). To reduce complexity, I limited the light source to one single directional light. Then for each light direction, store the caustics light into a cubemap. The direction is determined by the bounding mesh.

2. In the real-time rendering, for each light sources calculate the caustics light by interpolate between the caustics cubemap. The interpolatation weight is determined by the barycentric coordinate of the triangle on the bounding mesh hit by the light ray.
