using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System.Collections.Concurrent;
using Rhino.DocObjects;

namespace AutoLineWeight_V6
{
    internal class GeometryIntersects
    {
        public static Curve[] GetIntersects(GeometryContainer gc)
        {
            ConcurrentBag<Curve> intersects = new ConcurrentBag<Curve>();
            return intersects.ToArray();
        }

        public static Curve[] BrepBrep(Brep brep1, Brep brep2, double tol = 0)
        {
            Curve[] crvIntersect = { };

            if (!BoundingBoxOperations.BoundingBoxIntersects(brep1, brep2)) 
            { return crvIntersect; }

            // tries to avoid getting tolerance each time it is called
            if (tol == 0) { tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance; }

            bool success = Intersection.BrepBrep(brep1, brep2, tol, out crvIntersect, 
                out _);

            SetParents(brep1, brep2, crvIntersect);

            return crvIntersect;
        }

        public static Curve[] MeshMesh(Mesh mesh1, Mesh mesh2, double tol = 0)
        {
            Curve[] crvIntersect = { };

            if (!BoundingBoxOperations.BoundingBoxIntersects(mesh1, mesh2))
            { return crvIntersect; }

            // tries to avoid getting tolerance each time it is called
            if (tol == 0) { tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance; }

            Polyline[] plineIntersect = Intersection.MeshMeshAccurate(mesh1, mesh2, tol);
            crvIntersect = Array.ConvertAll(plineIntersect, pline =>
            {
                return pline.ToPolylineCurve();
            });

            SetParents(mesh1, mesh2, crvIntersect);

            return crvIntersect;
        }

        /// <summary>
        /// Helper method to set parent guids.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="crvs"></param>
        protected static void SetParents(GeometryBase p1, GeometryBase p2, Curve[] crvs)
        {
            // finds parent guids
            Guid parentGuid1;
            Guid parentGuid2;
            p1.UserDictionary.TryGetGuid("GUID", out parentGuid1);
            p2.UserDictionary.TryGetGuid("GUID", out parentGuid2);

            // sets parent guids to curve so as to trace back
            foreach (Curve crv in crvs)
            {
                if (crv == null) { continue; }
                crv.UserDictionary.Set("parentObj1", parentGuid1);
                crv.UserDictionary.Set("parentObj1", parentGuid2);
            }
        }
    }
}
