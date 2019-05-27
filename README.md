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

## Idea

For each object that has caustics when illuminated, surround it with a mesh bounding box. In the demo I used a [icosahedron](https://en.wikipedia.org/wiki/Icosahedron), but it can be alter to any convex shape. Each vertices of the bounding mesh mapping to one cubemap storing the caustics light.

1. Render the caustics of target object using photon mapping. To reduce complexity, the light source is limited to one single directional light. Then for each light directions, store the caustics light into a cubemap. The direction is determined by the bounding mesh.

2. In the real-time rendering, for each light sources calculate the caustics light by interpolate between the caustics cubemap. The interpolatation weight is determined by the barycentric coordinate of the triangle on the bounding mesh hit by the light ray.

## Create your own caustics

### 1. Render caustics map

Use 3ds Max with VRay to render the caustics map to a sphere. You can use spherify modifier on a cube to generate your baking sphere, or simply download [my max file](https://drive.google.com/file/d/1TP5A27SL667VW0N3JahGPcVbOruw4txi/view?usp=sharing) (for 3ds Max 2020). [The light cookie guide by Pierre](https://docs.unity3d.com/uploads/ExpertGuides/Create_High-Quality_Light_Fixtures_in_Unity.pdf) desribed the detail about generating Unity ready cubemap in 3ds Max.

Apply VRay material on your object. I suggest using the physical attribute in the real-world for your material. Lookup the index of refraction and abbe number of a material in [here](https://refractiveindex.info/?shelf=3d&book=crystals&page=sapphire).

![mlt](https://raw.githubusercontent.com/CJT-Jackton/Crystal-Caustics/master/screenshots/Sapphirel_Material.PNG "The material of Sapphire")
> An example VRay material setting of Sapphire.

Adjust the VRay rendering setting, including the caustics subdivisions of your light source, max photon number in the global caustics render setting until you satify with the outcome. Add VRayCaustics to render output, use render to texture on each face of the baking sphere to get the caustics.

### 2. Import into Unity

Assemble each side of the cubemap into one single texture in order. You can use the smart object in Photoshop linking to the rendering outcome to make your life easier.

Import the caustics map into Unity. Change the import setting of **Texture Shape** to **Cube**, **Format** to **RGBA 32 bit**.

![texture import setting](https://raw.githubusercontent.com/CJT-Jackton/Crystal-Caustics/master/screenshots/TextureImportSetting.PNG "The import setting")

### 3. Setup caustics

Create a child gameObject for your object. Adding a point light, a mesh collider and the Caustics script to the child gameObject, or your can simply copy the one in the example scene. Apply the caustics map to the cookie cubemap, and then select the light source that affect the caustics.

## Requirements

Unity 2019.1.0

- HD Render Pipeline
