# Crystal Caustics

This project approximate caustics of crystal in real-time using the physically-based caustics rendered offline.

![thumbnail](https://raw.githubusercontent.com/CJT-Jackton/Crystal-Caustics/master/screenshots/screenshot01.png "Crystal caustics in Unity")

![thumbnail](https://raw.githubusercontent.com/CJT-Jackton/Crystal-Caustics/master/screenshots/screenshot02.png "Crystal caustics in Unity")

Caustics is a common optical phenomenon that light ray reflected or refracted by an object and created a pattern of light. It usually required track the path of light ray go through the object. However ray tracing is too computationally expensive and can't be done in real-time. This project use pre-render caustics map and light cookie to achieve the approximated effects. See the demo [here](https://youtu.be/-mrXdcObRNk).

## Getting started

1. Install [High Definition Render Pipeline](https://blogs.unity3d.com/2018/09/24/the-high-definition-render-pipeline-getting-started-guide-for-artists/).

2. Change the setting in the HDRP asset: **Lighting** > **Cookies** > **Point light cookie size** > **Resolution 256**.

![setting](https://raw.githubusercontent.com/CJT-Jackton/Crystal-Caustics/master/screenshots/HDRPAssetSetting.PNG "Change the light cookie setting")

3. (*Optional*) Install Unity HDRI pack.

## Methodology

For each object that has caustics when illuminated, surround it with a mesh bounding box. In the demo I used a [icosahedron](https://en.wikipedia.org/wiki/Icosahedron), but it can be alter to any convex shape. Each vertices of the bounding mesh mapping to one cubemap storing the caustics light.

1. Render the caustics of target object using photon mapping. To reduce complexity, the light source is limited to one single directional light. Then for each light directions, store the caustics light into a cubemap. The direction is determined by the bounding mesh.

2. In the real-time rendering, for each light sources calculate the caustics light by interpolate between the caustics cubemap. The interpolatation weight is determined by the barycentric coordinate of the triangle on the bounding mesh hit by the light ray.

## Requirements

Unity 2019.1.0

- HD Render Pipeline
