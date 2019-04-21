#if SHADER_TARGET || UNITY_VERSION // We're being included in a shader
 // Convert an xyz vector to a uvw Texture2DArray sample as if it were a cubemap
 float3 xyz_to_uvw(float3 xyz)
 {
     // Find which dimension we're pointing at the most
     float3 absxyz = abs(xyz);
     int xMoreY = absxyz.x > absxyz.y;
     int yMoreZ = absxyz.y > absxyz.z;
     int zMoreX = absxyz.z > absxyz.x;
     int xMost = (xMoreY) && (!zMoreX);
     int yMost = (!xMoreY) && (yMoreZ);
     int zMost = (zMoreX) && (!yMoreZ);
 
     // Determine which index belongs to each +- dimension
     // 0: +X; 1: -X; 2: +Y; 3: -Y; 4: +Z; 5: -Z;
     float xSideIdx = 0 + (xyz.x < 0);
     float ySideIdx = 2 + (xyz.y < 0);
     float zSideIdx = 4 + (xyz.z < 0);
 
     // Composite it all together to get our side
     float side = xMost * xSideIdx + yMost * ySideIdx + zMost * zSideIdx;
 
     // Depending on side, we use different components for UV and project to square
     float3 useComponents = float3(0, 0, 0);
     if (xMost) useComponents = xyz.yzx;
     if (yMost) useComponents = xyz.xzy;
     if (zMost) useComponents = xyz.xyz;
     float2 uv = useComponents.xy / useComponents.z;
 
     // Transform uv from [-1,1] to [0,1]
     uv = uv * 0.5 + float2(0.5, 0.5);
 
     return float3(uv, side);
 }        
 
 // Convert an xyz vector to the side it would fall on for a cubemap
 // Can be used in conjuction with xyz_to_uvw_force_side
 float xyz_to_side(float3 xyz)
 {
     // Find which dimension we're pointing at the most
     float3 absxyz = abs(xyz);
     int xMoreY = absxyz.x > absxyz.y;
     int yMoreZ = absxyz.y > absxyz.z;
     int zMoreX = absxyz.z > absxyz.x;
     int xMost = (xMoreY) && (!zMoreX);
     int yMost = (!xMoreY) && (yMoreZ);
     int zMost = (zMoreX) && (!yMoreZ);
 
     // Determine which index belongs to each +- dimension
     // 0: +X; 1: -X; 2: +Y; 3: -Y; 4: +Z; 5: -Z;
     float xSideIdx = 0 + (xyz.x < 0);
     float ySideIdx = 2 + (xyz.y < 0);
     float zSideIdx = 4 + (xyz.z < 0);
 
     // Composite it all together to get our side
     return xMost * xSideIdx + yMost * ySideIdx + zMost * zSideIdx;
 }
 
 // Convert an xyz vector to a uvw Texture2DArray sample as if it were a cubemap
 // Will force it to be on a certain side
 float3 xyz_to_uvw_force_side(float3 xyz, float side)
 {
     // Depending on side, we use different components for UV and project to square
     float3 useComponents = float3(0, 0, 0);
     if (side < 2) useComponents = xyz.yzx;
     if (side >= 2 && side < 4) useComponents = xyz.xzy;
     if (side >= 4) useComponents = xyz.xyz;
     float2 uv = useComponents.xy / useComponents.z;
 
     // Transform uv from [-1,1] to [0,1]
     uv = uv * 0.5 + float2(0.5, 0.5);
 
     return float3(uv, side);
 }
 
 // Convert a uvw Texture2DArray coordinate to the vector that points to it on a cubemap
 float3 uvw_to_xyz(float3 uvw)
 {
     // Use side to decompose primary dimension and negativity
     int side = uvw.z;
     int xMost = side < 2;
     int yMost = side >= 2 && side < 4;
     int zMost = side >= 4;
     int wasNegative = side & 1;
 
     // Insert a constant plane value for the dominant dimension in here
     uvw.z = 1;
 
     // Depending on the side we swizzle components back (NOTE: uvw.z is 1)
     float3 useComponents = float3(0, 0, 0);
     if (xMost) useComponents = uvw.zxy;
     if (yMost) useComponents = uvw.xzy;
     if (zMost) useComponents = uvw.xyz;
 
     // Transform components from [0,1] to [-1,1]
     useComponents = useComponents * 2 - float3(1, 1, 1);
     useComponents *= 1 - 2 * wasNegative;
 
     return useComponents;
 }
#else // We're being included in a C# workspace
using UnityEngine;

namespace CubemapTransform
{
    public static class CubemapExtensions
    {
        // Convert an xyz vector to a uvw Texture2DArray sample as if it were a cubemap
        public static Vector3 XyzToUvw(this Vector3 xyz)
        {
            return xyz.XyzToUvwForceSide(xyz.XyzToSide());
        }

        // Convert an xyz vector to the side it would fall on for a cubemap
        // Can be used in conjuction with Vector3.XyzToUvwForceSide(int)
        public static int XyzToSide(this Vector3 xyz)
        {
            // Find which dimension we're pointing at the most
            Vector3 abs = new Vector3(Mathf.Abs(xyz.x), Mathf.Abs(xyz.y), Mathf.Abs(xyz.z));
            bool xMoreY = abs.x > abs.y;
            bool yMoreZ = abs.y > abs.z;
            bool zMoreX = abs.z > abs.x;
            bool xMost = (xMoreY) && (!zMoreX);
            bool yMost = (!xMoreY) && (yMoreZ);
            bool zMost = (zMoreX) && (!yMoreZ);

            // Determine which index belongs to each +- dimension
            // 0: +X; 1: -X; 2: +Y; 3: -Y; 4: +Z; 5: -Z;
            int xSideIdx = xyz.x < 0 ? 1 : 0;
            int ySideIdx = xyz.y < 0 ? 3 : 2;
            int zSideIdx = xyz.z < 0 ? 5 : 4;

            // Composite it all together to get our side
            return (xMost ? xSideIdx : 0) + (yMost ? ySideIdx : 0) + (zMost ? zSideIdx : 0);
        }

        // Convert an xyz vector to a uvw Texture2DArray sample as if it were a cubemap
        // Will force it to be on a certain side
        public static Vector3 XyzToUvwForceSide(this Vector3 xyz, int side)
        {
            // Depending on side, we use different components for UV and project to square
            Vector2 uv = new Vector2(side < 2 ? xyz.y : xyz.x, side >= 4 ? xyz.y : xyz.z);
            uv /= xyz[side / 2];

            // Transform uv from [-1,1] to [0,1]
            uv *= 0.5f;
            return new Vector3(uv.x + 0.5f, uv.y + 0.5f, side);
        }

        // Convert a uvw Texture2DArray coordinate to the vector that points to it on a cubemap
        public static Vector3 UvwToXyz(this Vector3 uvw)
        {
            // Use side to decompose primary dimension and negativity
            int side = (int)uvw.z;
            bool xMost = side < 2;
            bool yMost = side >= 2 && side < 4;
            bool zMost = side >= 4;
            int wasNegative = side & 1;

            // Restore components based on side
            Vector3 result = new Vector3(
                xMost ? 1 : uvw.x,
                yMost ? 1 : (xMost ? uvw.x : uvw.y),
                zMost ? 1 : uvw.y);

            // Transform components from [0,1] to [-1,1]
            result *= 2;
            result -= new Vector3(1, 1, 1);
            result *= 1 - 2 * wasNegative;

            return result;
        }
    }
}
#endif
