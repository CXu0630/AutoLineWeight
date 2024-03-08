using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoLineWeight_V6
{
    public class MeshOutline
    {
        ObjRef[] outlineObjs;
        RhinoViewport vp;

        List<PolylineCurve> outputCrvs = new List<PolylineCurve>();

        public MeshOutline(ObjRef[] outlineObjs, RhinoViewport vp)
        {
            this.outlineObjs = outlineObjs;
            this.vp = vp;
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static MeshOutline Instance { get; private set; }

        public string EnglishName => "MeshOutline";

        protected Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            int objCount = this.outlineObjs.Length;
            Mesh[] inMeshes = new Mesh[objCount];
            int meshIter = 0;
            RhinoObject[] inObjects = new RhinoObject[objCount];
            int objIter = 0;

            // tried to write this to be more optimized with an array instead of a list...
            for (int i = 0; i < objCount; i++)
            {
                ObjRef objRef = this.outlineObjs[i];
                Mesh mesh = objRef.Mesh();
                if (mesh != null)
                {
                    inMeshes[meshIter] = mesh;
                    meshIter++;
                }
                else
                {
                    inObjects[objIter] = objRef.Object();
                    objIter++;
                }
            }

            if (objIter > 0)
            {
                ObjRef[] meshRefs = RhinoObject.GetRenderMeshes(inObjects, true, false);
                if (meshRefs != null)
                {
                    for (int i = 0; i < meshRefs.Length; i++)
                    {
                        Mesh mesh = meshRefs[i].Mesh();
                        if (mesh != null)
                        {
                            inMeshes[meshIter] = mesh;
                            meshIter++;
                        }
                    }
                }
            }

            Mesh baseMesh = inMeshes[0];
            if (baseMesh == null) { return Result.Failure; }

            Mesh[] addMeshes = new Mesh[objCount - 1];
            Array.Copy(inMeshes, 1, addMeshes, 0, objCount - 1);
            addMeshes = addMeshes.Where(mesh => mesh != null).ToArray();

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
                        this.outputCrvs.Add(plineCrv);
                    }
                }
            }

            return Result.Success;
        }

        public PolylineCurve[] GetOutlines()
        {
            this.RunCommand(RhinoDoc.ActiveDoc, RunMode.Interactive);
            return this.outputCrvs.ToArray();
        }
    }
}