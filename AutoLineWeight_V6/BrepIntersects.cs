using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace AutoLineWeight_V6
{
    public class BrepIntersects : Command
    {
        Rhino.DocObjects.ObjRef[] objSel;
        ConcurrentBag<Curve> intersects = new ConcurrentBag<Curve>();
        public BrepIntersects(Rhino.DocObjects.ObjRef[] sourceObjSel)
        {
            Instance = this;
            this.objSel = sourceObjSel;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static BrepIntersects Instance { get; private set; }

        public override string EnglishName => "CalculateBrepIntersects";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            double tol = doc.ModelAbsoluteTolerance;
            int len = objSel.Length;
            ParallelLoopResult parallelLoopResult = Parallel.For (0,len,i =>
            {
                Rhino.DocObjects.ObjRef obj1 = objSel[i];
                Brep brep1 = obj1.Brep();
                if (brep1 == null) { return; }

                for (int j = i + 1; j < len; j++)
                {
                    Rhino.DocObjects.ObjRef obj2 = objSel[j];
                    Brep brep2 = obj2.Brep();
                    if (brep2 == null) { continue; }

                    BoundingBox bb1 = obj1.Object().Geometry.GetBoundingBox(false);
                    BoundingBox bb2 = obj2.Object().Geometry.GetBoundingBox(false);

                    if (!BoundingBoxOperations.BoundingBoxCoincides(bb1, bb2)) { continue; }

                    Point3d[] ptIntersect;
                    Curve[] crvIntersect;
                    
                    bool success = Intersection.BrepBrep(brep1, brep2, tol, out crvIntersect, out ptIntersect);
                    if (!success) { continue; }
                    foreach (Curve crv in crvIntersect)
                    {
                        if (crv == null) { continue; }
                        crv.UserDictionary.Set("parentObj1", obj1.ObjectId);
                        crv.UserDictionary.Set("parentObj2", obj2.ObjectId);
                        this.intersects.Add(crv);
                    }
                }
            });
            return Result.Success;
        }

        public Curve[] GetIntersects()
        {
            this.RunCommand(RhinoDoc.ActiveDoc, RunMode.Interactive);
            return this.intersects.ToArray();
        }
    }
}