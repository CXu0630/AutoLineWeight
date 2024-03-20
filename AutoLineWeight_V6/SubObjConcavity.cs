/*
-----------------------------------------------------------------------------------------
created 02/05/2024

Chloe Xu
guangyu.xu0630@gmail.com
Last edited:03/20/2024
-----------------------------------------------------------------------------------------
*/


using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace AutoLineWeight_V6
{
    internal class SubObjConcavity
    {
        public static Concavity GetConcavity(ObjRef objRef, ComponentIndex ci, Point3d pt)
        {
            if (objRef.Brep() != null)
            {
                return GetBrepConcavity(objRef, ci, pt);
            }
            if (objRef.Mesh() != null)
            {
                return GetMeshConcavivty(objRef, ci, pt);
            }

            return Concavity.None;
        }

        public static Concavity GetBrepConcavity (ObjRef objRef, ComponentIndex ci, Point3d pt)
        {
            Brep brep = objRef.Brep();
            if (brep == null) return Concavity.None;

            if (ci.ComponentIndexType == ComponentIndexType.BrepEdge) 
            {
                BrepEdge edge = brep.Edges[ci.Index];
                double sourcePt;
                edge.ClosestPoint(pt, out sourcePt);
                return edge.ConcavityAt(sourcePt, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
            }

            if (ci.ComponentIndexType == ComponentIndexType.BrepFace)
            {
                return Concavity.None;
            }

            if (ci.ComponentIndexType == ComponentIndexType.BrepTrim)
            {
                return Concavity.None;
            }
            return Concavity.None;
        }

        public static Concavity GetMeshConcavivty (ObjRef objRef, ComponentIndex ci, Point3d pt)
        {
            return Concavity.None;
        }
    }
}
