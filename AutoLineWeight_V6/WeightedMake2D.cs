/*
-----------------------------------------------------------------------------------------

This command creates a weighted 2D representation of user-selected 3D geometry.
It weighs the 2D lines based on formal relationships between the source geometry
edges and its adjacent faces. If an edge only has one adjacent face, or one of
its two adjacent faces is hidden, it is defined as an "WT_Outline". If both faces are
present and the line is on a convex corner, it is defined as "WT_Convex". All other
visible lines are defined as "WT_Concave". Hidden lines are also processed. Results
are baked onto layers according to their assigned weight.

Sorry for the spaghetti :(

-----------------------------------------------------------------------------------------
created 11/28/2023

Chloe Xu
guangyu.xu0630@gmail.com
Last edited:03/20/2024
-----------------------------------------------------------------------------------------
*/

using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace AutoLineWeight_V6
{
    /// <summary>
    /// This command creates a weighted 2D representation of user-selected 3D geometry.
    /// </summary>
    public class WeightedMake2D : Command
    {
        /// <summary>
        /// creates a weighted 2D representation of user-selected 3D geometry based
        /// on formal relationships between user-selected source geometry and its
        /// adjacent faces.
        /// </summary>

        RhinoViewport currentViewport;

        // initialize transformation (move and flatten)
        Transform flatten;

        // initialize user options:
        // color by source, include intersect, include clipping, include hidden,
        // include silhouette
        bool colorBySrc = true;
        bool addIntersect = true;
        bool meshBrep = false;
        bool addClip = false;
        bool addHid = false;
        bool addSil = false;

        // initialize layer management
        LayerManager LM;
        int clipLyrIdx;
        int silLyrIdx;
        int hidLyrIdx;
        int outLyrIdx;
        int convexLyrIdx;
        int concaveLyrIdx;


        public WeightedMake2D()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static WeightedMake2D Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "WeightedMake2D";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Aquires current viewport
            currentViewport = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;

            // aquires user selection
            ALWSelector selectObjs = new ALWSelector();
            selectObjs.SetDefaultValues(addClip, addHid, 
                addSil, addIntersect, colorBySrc, meshBrep);
            ObjRef[] objRefs = selectObjs.GetSelection();
            // aquires user options
            this.colorBySrc = selectObjs.colorBySource;
            this.addIntersect = selectObjs.includeIntersect;
            this.addClip = selectObjs.includeClipping;
            this.addHid = selectObjs.includeHidden;
            this.addSil = selectObjs.includeSceneSilhouette;
            this.meshBrep = selectObjs.meshBrepIntersect;
            // resolves conflict between silhouette and clipping
            if (this.addSil) { this.addClip = false; }
            if (!this.addIntersect) { this.meshBrep = false; }

            // error aquireing user selection
            if (objRefs == null) { return Result.Cancel; }


            // start stopwatch for entire process
            Stopwatch watch0 = Stopwatch.StartNew();


            // create geometry container and parse blocks

            GeometryContainer gc = new GeometryContainer(objRefs, addSil || meshBrep);
            RhinoApp.WriteLine(" ---------------- ");
            RhinoApp.WriteLine(gc.GetCount().ToString() 
                + " objects prior to exploding blocks");
            GeometryContainer expGC = gc.ExplodeBlocks();
            RhinoApp.WriteLine(expGC.GetCount().ToString() 
                + " objects after exploding blocks");
            RhinoApp.WriteLine(" ---------------- ");


            Curve[] intersects3D = { };
            Curve[] intersects2D = { };
            BoundingBox intersectsBB = new BoundingBox();

            if (this.addIntersect)
            {
                // calculate the intersection between selected breps
                Stopwatch watch1 = Stopwatch.StartNew();
                intersects3D = GeometryIntersects.GetIntersects(expGC, meshBrep);
                intersects2D = CurvesMake2D(doc, intersects3D, out intersectsBB);
                watch1.Stop();
                RhinoApp.WriteLine("Calculating geometry intersects {0} miliseconds.",
                    watch1.ElapsedMilliseconds.ToString());
            }


            // compute a make2D for selected geometry plus intersections
            Stopwatch watch2 = Stopwatch.StartNew();
            ALWMake2D awlMake2D = new ALWMake2D(expGC, intersects3D,
                currentViewport, addClip, addHid);
            HiddenLineDrawing hld = awlMake2D.GetMake2D(doc);
            if (hld == null) { return Result.Failure; }
            // recalculate flatten
            RecalcTransformation(hld);
            watch2.Stop();
            RhinoApp.WriteLine("Generating Make2D {0} miliseconds.",
                watch2.ElapsedMilliseconds.ToString());


            // generate layers
            Stopwatch watch3 = Stopwatch.StartNew();
            GenerateLayers(doc);
            watch3.Stop();
            RhinoApp.WriteLine("Generating Layers {0} miliseconds.",
                watch3.ElapsedMilliseconds.ToString());


            // sort make2D curves
            Stopwatch watch4 = Stopwatch.StartNew();
            foreach (HiddenLineDrawingSegment make2DCurve in hld.Segments)
            { SortMake2DSegment(doc, make2DCurve, intersects2D, intersectsBB); }
            doc.Views.Redraw();
            watch4.Stop();
            RhinoApp.WriteLine("Sorting Curves took {0} miliseconds.",
                watch4.ElapsedMilliseconds.ToString());


            // generate outlines if required
            if (this.addSil)
            {
                Stopwatch watch5 = Stopwatch.StartNew();
                MakeOutline(doc, expGC);
                watch5.Stop();
                RhinoApp.WriteLine("Generating Outline took {0} miliseconds.",
                    watch5.ElapsedMilliseconds.ToString());
            }

            RhinoApp.WriteLine("WeightedMake2D was Successful!");
            watch0.Stop();
            long elapsedMs = watch0.ElapsedMilliseconds;
            RhinoApp.WriteLine("WeightedMake2D took {0} milliseconds.", elapsedMs.ToString());

            return Result.Success;
        }


        /// <summary>
        /// Helper function to recalculate the transformation required to flatten and move
        /// a make2D drawing to the origin.
        /// </summary>
        /// <param name="make2D"></param>
        private void RecalcTransformation(HiddenLineDrawing make2D)
        {
            this.flatten = Transform.PlanarProjection(Plane.WorldXY);
            BoundingBox bb = make2D.BoundingBox(true);
            Vector3d moveVector = BoundingBoxOperations.VectorPointMinOrigin(bb);
            moveVector.Z = 0;
            Transform move2D = Transform.Translation(moveVector);
            this.flatten = move2D * flatten;
        }


        /// <summary>
        /// Method used to sort HiddenLineDrawing curves based on the formal relationships 
        /// between their source edges and their adjacent faces.
        /// </summary>
        private void SortMake2DSegment(RhinoDoc doc, HiddenLineDrawingSegment make2DCurve, 
            Curve[] intersects, BoundingBox intersectsBB)
        {
            // Check for parent curve. Discard if not found.
            if (make2DCurve?.ParentCurve == null ||
                make2DCurve.ParentCurve.SilhouetteType == SilhouetteType.None)
                return;

            if (make2DCurve.SegmentVisibility == HiddenLineDrawingSegment.Visibility.Hidden
                && this.addHid == false) return;

            var crv = make2DCurve.CurveGeometry.DuplicateCurve();

            if (crv == null) return;

            var attr = new ObjectAttributes();

            HiddenLineDrawingObject source = make2DCurve.ParentCurve.SourceObject;
            ObjRef srcRef = new ObjRef((Guid)source.Tag);
            RhinoObject sourceObj = srcRef.Object();

            if (this.colorBySrc && sourceObj != null)
            {
                attr.PlotColorSource = ObjectPlotColorSource.PlotColorFromObject;
                attr.ColorSource = ObjectColorSource.ColorFromObject;
                Color objColor = sourceObj.Attributes.DrawColor(doc);
                Color dispColor = sourceObj.Attributes.ComputedPlotColor(doc);
                attr.ObjectColor = objColor;
                attr.PlotColor = dispColor;
            }

            // Processes visible curves
            if (make2DCurve.SegmentVisibility == HiddenLineDrawingSegment.Visibility.Visible)
            {
                // find source sub object
                ComponentIndex ci = make2DCurve.ParentCurve.SourceObjectComponentIndex;
                
                // find midpoint, if an edge is broken up into multiple segments in the make2D, each segment
                // is weighed individually.
                Point3d start = crv.PointAtStart;
                Point3d end = crv.PointAtEnd;
                Point3d mid = start + (start - end) / 2;

                // find concavity of original edge at segment midpoint
                ObjRef sourceObjRef = new ObjRef((Guid)source.Tag);
                Concavity crvMidConcavity = SubObjConcavity.GetConcavity(sourceObjRef, ci, mid);


                // silhouette type determines of the segment is an outline
                SilhouetteType silType = make2DCurve.ParentCurve.SilhouetteType;
                attr.SetUserString("Siltype", silType.ToString());
                // sort segments into layers based on outline and concavity

                if (silType == SilhouetteType.SectionCut && addClip)
                {
                    attr.LayerIndex = clipLyrIdx;

                }
                else if (silType == SilhouetteType.Boundary ||
                    silType == SilhouetteType.Crease ||
                    silType == SilhouetteType.Tangent ||
                    silType == SilhouetteType.TangentProjects)
                {
                    attr.LayerIndex = outLyrIdx;
                    bool segmented = SegmentAndAddToDoc(doc, attr, crv, intersects, intersectsBB);
                    if (segmented) { return; }
                }
                else if (crvMidConcavity == Concavity.Convex)
                {
                    attr.LayerIndex = convexLyrIdx;
                    bool segmented = SegmentAndAddToDoc(doc, attr, crv, intersects, intersectsBB);
                    if (segmented) { return; }
                }
                else
                {
                    attr.LayerIndex = concaveLyrIdx;
                }
            }
            // process hidden curves: add them to the hidden layer
            else if (make2DCurve.SegmentVisibility == HiddenLineDrawingSegment.Visibility.Hidden)
            {
                attr.LayerIndex = hidLyrIdx;
            }
            else { return; }

            AddtoDoc(doc, crv, attr);
        }


        /// <summary>
        /// Helper method to segment input curves based on the intersectionSegments 
        /// property. Sections of the curve that intersect with intersectionSegments are
        /// put under the concave layer, other sections are put under convex.
        /// </summary>
        /// <returns> whether or not SegmentAndAddToDoc was successful </returns>
        private bool SegmentAndAddToDoc(RhinoDoc doc, ObjectAttributes attribs, Curve crv, 
            Curve[] intersects, BoundingBox intersectsBB)
        {
            //if (intersectionSegments == null) { return false; }
            if (intersects.Length == 0) { return false; }
            if (BoundingBoxOperations.BoundingBoxIntersects(crv.GetBoundingBox(false),
                intersectsBB) == false) { return false; }
            CurveBooleanDifference crvBD =
                new CurveBooleanDifference(crv, intersects);
            crvBD.CalculateOverlap();
            Curve[] remaining = crvBD.GetResultCurves();
            Curve[] overlap = crvBD.GetOverlapCurves();

            foreach (Curve remainingCrv in remaining)
            {
                AddtoDoc(doc, remainingCrv, attribs);
            }
            foreach (Curve overlappingCrv in overlap)
            {
                attribs.LayerIndex = concaveLyrIdx;
                AddtoDoc(doc, overlappingCrv, attribs);
            }

            return true;
        }


        /// <summary>
        /// Helper method to add a curve to a specific layer in the Rhinodoc.
        /// </summary>
        private void AddtoDoc(RhinoDoc doc, Curve crv, ObjectAttributes attr)
        {
            crv.Transform(flatten);
            Guid crvId = doc.Objects.AddCurve(crv, attr);
            RhinoObject addedCrv = doc.Objects.FindId(crvId);
            addedCrv.Select(true);
        }


        /// <summary>
        /// Helper method to create a layer heirarchy in preparation of adding sorted
        /// curves to the document.
        /// </summary>
        private void GenerateLayers(RhinoDoc doc)
        {
            string[] level2Lyrs = { null, "WT_Outline", "WT_Convex", "WT_Concave" };
            // add clipping/silhouette layers if necessary
            if (this.addSil) { level2Lyrs[0] = "WT_Silhouette"; }
            else if (this.addClip) { level2Lyrs[0] = "WT_Cut"; }

            LM = new LayerManager(doc);

            LM.Add("WT_Visible", "WT_Make2D");
            LM.Add(level2Lyrs, "WT_Visible");

            LM.GradientWeightAssign(level2Lyrs, 0.15, 1.5);

            if (this.addHid)
            {
                LM.Add("WT_Hidden", "WT_Make2D");
                Layer hiddenLyr = LM.GetLayer("WT_Hidden");
                int ltIdx = doc.Linetypes.Find("Hidden");
                if (ltIdx >= 0) { hiddenLyr.LinetypeIndex = ltIdx; }
                hiddenLyr.PlotWeight = 0.1;
            }

            clipLyrIdx = LM.GetIdx("WT_Cut");
            silLyrIdx = LM.GetIdx("WT_Silhouette");
            hidLyrIdx = LM.GetIdx("WT_Hidden");
            outLyrIdx = LM.GetIdx("WT_Outline");
            convexLyrIdx = LM.GetIdx("WT_Convex");
            concaveLyrIdx = LM.GetIdx("WT_Concave");
        }



        /// <summary>
        /// Helper method to generate the outline of selected geometry.
        /// </summary>
        private void MakeOutline(RhinoDoc doc, GeometryContainer gc)
        {
            PolylineCurve[] outlines3D = MeshOutline.GetOutline(gc, currentViewport);
            Curve[] outlines2D = CurvesMake2D(doc, outlines3D, out _);

            var attr = new ObjectAttributes();
            attr.LayerIndex = silLyrIdx;

            foreach (Curve crv in outlines2D) AddtoDoc(doc, crv, attr);
        }



        /// <summary>
        /// Helper method to process intersection curves and compute bounding box and 
        /// an array of unflattened make2D intersection curves.
        /// </summary>
        private Curve[] CurvesMake2D (RhinoDoc doc, Curve[] crvs3D, out BoundingBox bb)
        {
            List<Curve> crvs2D = new List<Curve>();
            bb = new BoundingBox();

            if (crvs3D.Length == 0) return crvs2D.ToArray();

            // generate this drawing only if there are intersects
            ALWMake2D awlMake2D = new ALWMake2D(crvs3D,
                currentViewport, addClip, addHid);
            HiddenLineDrawing hld = awlMake2D.GetMake2D(doc);

            if (hld == null) { return crvs2D.ToArray(); }
            //TODO: error handling

            foreach (var make2DCurve in hld.Segments)
            {
                //Check for parent curve. Discard if not found.
                if (make2DCurve?.ParentCurve == null ||
                    make2DCurve.ParentCurve.SilhouetteType == SilhouetteType.None)
                    continue;

                var crv = make2DCurve.CurveGeometry.DuplicateCurve();
                crvs2D.Add(crv);
            }

            bb = hld.BoundingBox(false);
            return crvs2D.ToArray();
        }
    }
}
