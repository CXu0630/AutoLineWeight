/*
-----------------------------------------------------------------------------------------
created 03/18/2024

Chloe Xu
guangyu.xu0630@gmail.com
Last edited:03/20/2024
-----------------------------------------------------------------------------------------
*/

using System;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System.Collections.Concurrent;

namespace AutoLineWeight_V6
{
    internal class GeometryIntersects
    {
        /// <summary>
        /// Calculates all geometry intersections within a GeometryContainer.
        /// </summary>
        /// <param name="gc"></param>
        /// <param name="meshBrep"> Whether or not to calculate intersections between
        /// meshes and breps stored within the GeometryContainer. </param>
        /// <returns></returns>
        public static Curve[] GetIntersects(GeometryContainer gc, bool meshBrep)
        {
            ConcurrentBag<Curve> intersects = new ConcurrentBag<Curve>();

            // get brep brep intersections
            int lenBrep = gc.breps.Count;

            ParallelLoopResult parallelLoopResult = Parallel.For (0, lenBrep, i =>
            {
                if (gc.breps[i] == null) { return; }
                for(int j = i + 1; j < lenBrep; j++)
                {
                    if (gc.breps[j] == null) { continue; }
                    Curve[] pairIntersects = BrepBrep(gc.breps[i], gc.breps[j]);
                    foreach (Curve curve in pairIntersects) { intersects.Add(curve); }
                }
            });

            // get mesh mesh intersections
            int lenMesh = gc.meshes.Count;
            // no need to calculate mesh x brep if there are no meshes
            if(lenMesh == 0) { return intersects.ToArray(); }

            ParallelLoopResult parallelLoopResult2 = Parallel.For(0, lenMesh, i =>
            {
                if (gc.meshes[i] == null) { return; }
                for (int j = i + 1; j < lenMesh; j++)
                {
                    if (gc.meshes[j] == null) { continue; }
                    Curve[] pairIntersects = MeshMesh(gc.meshes[i], gc.meshes[j]);
                    foreach (Curve curve in pairIntersects) { intersects.Add(curve); }
                }
            });

            if (!meshBrep || gc.includeRenderMesh == false) { return intersects.ToArray(); }
            int lenRMesh = gc.renderMeshes.Count;
            if (lenRMesh == 0) { return intersects.ToArray(); }

            ParallelLoopResult parallelLoopResult3 = Parallel.For(0, lenRMesh, i =>
            {
                if (gc.renderMeshes[i] == null) { return; }
                for (int j = 0; j < lenMesh; j++)
                {
                    if (gc.meshes[j] == null) { continue; }
                    Curve[] pairIntersects = MeshMesh(gc.renderMeshes[i], gc.meshes[j]);
                    foreach (Curve curve in pairIntersects) { intersects.Add(curve); }
                }
            });

            return intersects.ToArray();
        }

        /// <summary>
        /// Calculates intersections between two breps.
        /// </summary>
        /// <param name="brep1"></param>
        /// <param name="brep2"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Calculates intersection curves between two meshes.
        /// </summary>
        /// <param name="mesh1"></param>
        /// <param name="mesh2"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
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
