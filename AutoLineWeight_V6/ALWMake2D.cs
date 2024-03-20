/*
-----------------------------------------------------------------------------------------
created 11/29/2023

Chloe Xu
guangyu.xu0630@gmail.com
Last edited:03/20/2024
-----------------------------------------------------------------------------------------
*/

using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;

namespace AutoLineWeight_V6
{
    /// <summary>
    /// Used to create an unflattened HiddenLineDrawing of input 3D geometry. Is
    /// customized for line weight sorting.
    /// </summary>
    internal class ALWMake2D
    {
        private RhinoViewport vp;
        private Curve[] crvs = { };
        private GeometryContainer gc = new GeometryContainer();
        private bool includeClipping;
        private bool includeHidden;

        public ALWMake2D(Curve[] intersects, RhinoViewport viewport, 
            bool includeClipping, bool includeHidden)
        {
            this.crvs = intersects;
            this.vp = viewport;
            this.includeClipping = includeClipping;
            this.includeHidden = includeHidden;
        }

        public ALWMake2D(GeometryContainer gc, Curve[] intersects, RhinoViewport viewport, 
            bool includeClipping, bool includeHidden)
        {
            this.gc = gc;
            this.crvs = intersects;
            this.vp= viewport;
            this.includeClipping = includeClipping;
            this.includeHidden = includeHidden;
        }

        public HiddenLineDrawing GetMake2D(RhinoDoc doc)
        {
            HiddenLineDrawingParameters make2DParams = new HiddenLineDrawingParameters();
            make2DParams.SetViewport(this.vp);
            make2DParams.IncludeHiddenCurves = this.includeHidden;
            make2DParams.IncludeTangentEdges = true;
            make2DParams.Flatten = false;
            make2DParams.AbsoluteTolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            // null cases: no geometry or no viewport
            if (gc.GetGeoCount() + crvs.Length == 0) return null;
            if (this.vp == null) return null;

            int count = 0;

            GeometryBase[] allGeo = gc.GetAllGeometries();
            foreach(GeometryBase geo in allGeo)
            {
                if (geo == null) continue;
                Guid srcGuid;
                geo.UserDictionary.TryGetGuid("GUID", out srcGuid);
                if (make2DParams.AddGeometry(geo, Transform.Identity, srcGuid)) count++;
            }

            foreach (Curve crv in this.crvs)
            {
                Guid parentGuid;
                crv.UserDictionary.TryGetGuid("parentObj1", out parentGuid);
                if (crv != null) 
                { 
                    if (make2DParams.AddGeometry(crv, Transform.Identity, parentGuid)) count++;
                }
            }

            RhinoApp.WriteLine(count.ToString() + " number of objects processing for make2D;");

            if (!this.includeHidden) { make2DParams.IncludeHiddenCurves = false; }

            if (this.includeClipping)
            {
                ClippingPlaneObject[] activeClippingPlanes = 
                    doc.Objects.FindClippingPlanesForViewport(this.vp);
                if (activeClippingPlanes != null)
                {
                    foreach (ClippingPlaneObject clippingPlane in activeClippingPlanes)
                    {
                        Plane plane = clippingPlane.ClippingPlaneGeometry.Plane;
                        plane.Flip();
                        make2DParams.AddClippingPlane(plane);
                    }
                }
            }

            HiddenLineDrawing make2D = HiddenLineDrawing.Compute(make2DParams, true);
            return make2D;
        }
    }
}