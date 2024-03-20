/*
-----------------------------------------------------------------------------------------
created 01/03/2024

Chloe Xu
guangyu.xu0630@gmail.com
Last edited:03/20/2024
-----------------------------------------------------------------------------------------
*/

using Rhino.Geometry;

namespace AutoLineWeight_V6
{
    /// <summary>
    /// Helper class for static bounding box operations
    /// </summary>
    public class BoundingBoxOperations
    {
        /// <summary>
        /// Static method that returns whether two bounding boxes have intersections.
        /// </summary>
        /// <param name="bb1"></param>
        /// <param name="bb2"></param>
        /// <returns></returns>
        public static bool BoundingBoxIntersects(BoundingBox bb1, BoundingBox bb2)
        {
            return bb1.Min.X <= bb2.Max.X &&
                bb1.Max.X >= bb2.Min.X &&
                bb1.Min.Y <= bb2.Max.Y &&
                bb1.Max.Y >= bb2.Min.Y &&
                bb1.Min.Z <= bb2.Max.Z &&
                bb1.Max.Z >= bb2.Min.Z;
        }

        /// <summary>
        /// Static method that returns whether the bounding boxes of two GeometryBases
        /// have intersections.
        /// </summary>
        /// <param name="gb1"></param>
        /// <param name="gb2"></param>
        /// <returns></returns>
        public static bool BoundingBoxIntersects(GeometryBase gb1, GeometryBase gb2)
        {
            BoundingBox bb1 = gb1.GetBoundingBox(false);
            BoundingBox bb2 = gb2.GetBoundingBox(false);

            return BoundingBoxIntersects(bb1, bb2);
        }

        /// <summary>
        /// Returns the point of the bounding box where x, y, and z values are minimum.
        /// </summary>
        /// <param name="bb"></param>
        /// <returns></returns>
        public static Point3d PointMin(BoundingBox bb)
        {
            return new Point3d(bb.Min.X, bb.Min.Y, bb.Min.Z);
        }

        /// <summary>
        /// Returns a vector representing the transformation from the center of the
        /// bounding box to the origin of the model.
        /// </summary>
        /// <param name="bb"></param>
        /// <returns></returns>
        public static Vector3d VectorCenterOrigin(BoundingBox bb)
        {
            Point3d center = bb.Center;
            Vector3d centerToO = new Vector3d(center);
            centerToO.Reverse();
            return centerToO;
        }

        /// <summary>
        /// Returns a vector representing the transformation form the point where x, y, z
        /// values are minimum for the bounding box to the origin of the model.
        /// </summary>
        /// <param name="bb"></param>
        /// <returns></returns>
        public static Vector3d VectorPointMinOrigin(BoundingBox bb)
        {
            Vector3d vec = new Vector3d(PointMin(bb));
            return -vec;
        }
    }
}