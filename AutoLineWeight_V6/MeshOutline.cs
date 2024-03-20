/*
-----------------------------------------------------------------------------------------
created 01/03/2024

Chloe Xu
guangyu.xu0630@gmail.com
Last edited:03/20/2024
-----------------------------------------------------------------------------------------
*/

using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace AutoLineWeight_V6
{
    /// <summary>
    /// This is a reproduction of the Rhino MeshOutline command. Customized to use
    /// GeometryContainers.
    /// </summary>
    internal class MeshOutline
    {
        public static PolylineCurve[] GetOutline(GeometryContainer gc, RhinoViewport vp)
        {
            List<PolylineCurve> outlines = new List<PolylineCurve>();

            Mesh baseMesh;
            if (gc.meshes.Count == 0)
            {
                if (gc.renderMeshes.Count == 0) { return outlines.ToArray(); }
                baseMesh = gc.renderMeshes[0];
            } else { baseMesh = gc.meshes[0]; }
            if (baseMesh == null) { return outlines.ToArray(); }

            List<Mesh> addMeshesLst = new List<Mesh>(gc.meshes);
            addMeshesLst.AddRange(gc.renderMeshes);
            Mesh[] addMeshes = addMeshesLst.Where(mesh => mesh != null).ToArray();

            if (addMeshes != null && addMeshes.Length != 0)
            {
                baseMesh.Append(addMeshes);
            }

            Polyline[] basePLine = baseMesh.GetOutlines(vp);
            if (basePLine != null)
            {
                for (int j = 0; j < basePLine.Length; j++)
                {
                    PolylineCurve plineCrv = new PolylineCurve(basePLine[j]);
                    double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
                    plineCrv.RemoveShortSegments(tol);
                    if (plineCrv.IsValid)
                    {
                        outlines.Add(plineCrv);
                    }
                }
            }

            return outlines.ToArray();
        }
    }
}