using Rhino.Geometry;

namespace AutoLineWeight_V6
{
    public class BoundingBoxOperations
    {
        public BoundingBoxOperations()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static BoundingBoxOperations Instance { get; private set; }

        public static bool BoundingBoxCoincides(BoundingBox bb1, BoundingBox bb2)
        {
            return bb1.Min.X <= bb2.Max.X &&
                bb1.Max.X >= bb2.Min.X &&
                bb1.Min.Y <= bb2.Max.Y &&
                bb1.Max.Y >= bb2.Min.Y &&
                bb1.Min.Z <= bb2.Max.Z &&
                bb1.Max.Z >= bb2.Min.Z;
        }

        public static Point3d PointLeftBot(BoundingBox bb)
        {
            return new Point3d(bb.Min.X, bb.Min.Y, bb.Min.Z);
        }

        public static Vector3d VectorCenterOrigin(BoundingBox bb)
        {
            Point3d center = bb.Center;
            Vector3d centerToO = new Vector3d(center);
            centerToO.Reverse();
            return centerToO;
        }

        public static Vector3d VectorLeftBottomOrigin(BoundingBox bb)
        {
            Vector3d vec = new Vector3d(PointLeftBot(bb));
            return -vec;
        }
    }
}