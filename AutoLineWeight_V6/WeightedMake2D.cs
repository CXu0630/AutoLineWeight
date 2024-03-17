/*
-----------------------------------------------------------------------------------------

This command creates a weighted 2D representation of user-selected 3D geometry.
It weighs the 2D lines based on formal relationships between the source geometry
edges and its adjacent faces. If an edge only has one adjacent face, or one of
its two adjacent faces is hidden, it is defined as an "WT_Outline". If both faces are
present and the line is on a convex corner, it is defined as "WT_Convex". All other
visible lines are defined as "WT_Concave". Hidden lines are also processed. Results
are baked onto layers according to their assigned weight.

-----------------------------------------------------------------------------------------
created 11/28/2023
Ennead Architects

Chloe Xu
chloe.xu@ennead.com
edited:01/04/2024

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
using System.Runtime.InteropServices;

namespace AutoLineWeight_V6
{
    public class WeightedMake2D : Command
    {
        /// <summary>
        /// creates a weighted 2D representation of user-selected 3D geometry based
        /// on formal relationships between user-selected source geometry and its
        /// adjacent faces.
        /// </summary>

        // TODO: UPDATE TO BE ARGUMENTS TO FUNCTIONS AND NOT PROPERTIES?
        // would that even be a good idea?
        // initize make2D properties
        ObjRef[] objRefs;
        RhinoViewport currentViewport;

        // initialize transformation (move and flatten)
        Transform flatten;

        // initialize intersection properties
        Curve[] intersects = { };
        BoundingBox intersectionBB;
        Curve[] intersectionSegments = { };

        // initialize user options
        bool colorBySource = true;
        bool includeIntersect = true;
        bool includeClipping = false;
        bool includeHidden = false;
        bool includeSilhouette = false;

        // initialize layer management
        LayerManager LM;
        int clipIdx;
        int silhouetteIdx;
        int hiddenIdx;
        int outlineIdx;
        int convexIdx;
        int concaveIdx;


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
            WMSelector selectObjs = new WMSelector();
            selectObjs.SetDefaultValues(includeClipping, includeHidden, 
                includeSilhouette, includeIntersect, colorBySource);
            this.objRefs = selectObjs.GetSelection();
            // aquires user options
            this.colorBySource = selectObjs.colorBySource;
            this.includeIntersect = selectObjs.includeIntersect;
            this.includeClipping = selectObjs.includeClipping;
            this.includeHidden = selectObjs.includeHidden;
            this.includeSilhouette = selectObjs.includeSceneSilhouette;
            // resolves conflict between silhouette and clipping
            if (this.includeSilhouette) { this.includeClipping = false; }

            // error aquireing user selection
            if (objRefs == null) { return Result.Cancel; }

            foreach (ObjRef objRef in objRefs)
            {
                RhinoApp.WriteLine(objRef.Object().ObjectType.ToString());
                RhinoObject id = objRef.InstanceDefinitionPart();
                if (id == null) { continue; }
                id.DuplicateGeometry();
            }

            // start stopwatch for entire process
            Stopwatch watch0 = Stopwatch.StartNew();


            if (this.includeIntersect)
            {
                // calculate the intersection between selected breps
                Stopwatch watch1 = Stopwatch.StartNew();
                // calculate intersections between breps
                BrepIntersects calcBrepInts = new BrepIntersects(objRefs);
                intersects = calcBrepInts.GetIntersects();
                MakeIntersects(doc);
                watch1.Stop();
                RhinoApp.WriteLine("Calculating geometry intersects {0} miliseconds.",
                    watch1.ElapsedMilliseconds.ToString());
            }


            // compute a make2D for selected geometry plus intersections
            Stopwatch watch2 = Stopwatch.StartNew();
            GenericMake2D createMake2D = new GenericMake2D(objRefs, intersects,
                currentViewport, includeClipping, includeHidden);
            HiddenLineDrawing make2D = createMake2D.GetMake2D();
            if (make2D == null) { return Result.Failure; }
            // recalculate flatten
            RecalcTransformation(make2D);
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
            foreach (HiddenLineDrawingSegment make2DCurve in make2D.Segments)
            { SortMake2DSegment(doc, make2DCurve); }
            doc.Views.Redraw();
            watch4.Stop();
            RhinoApp.WriteLine("Sorting Curves took {0} miliseconds.",
                watch4.ElapsedMilliseconds.ToString());


            // generate outlines if required
            if (this.includeSilhouette)
            {
                Stopwatch watch5 = Stopwatch.StartNew();
                MakeOutline(doc);
                watch5.Stop();
                RhinoApp.WriteLine("Generating Outline took {0} miliseconds.",
                    watch5.ElapsedMilliseconds.ToString());
            }


            // TODO: PASS AS ARGUMENTS TO FUNCTIONS INSTEAD OF CLEARING AT END
            this.objRefs = new ObjRef[] {};
            this.intersects = new Curve[] {};
            this.intersectionBB = new BoundingBox();
            this.intersectionSegments = new Curve[] {};

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
            Vector3d moveVector = BoundingBoxOperations.VectorLeftBottomOrigin(bb);
            moveVector.Z = 0;
            Transform move2D = Transform.Translation(moveVector);
            this.flatten = move2D * flatten;
        }


        /// <summary>
        /// Method used to sort HiddenLineDrawing curves based on the formal relationships 
        /// between their source edges and their adjacent faces.
        /// </summary>
        private void SortMake2DSegment(RhinoDoc doc, HiddenLineDrawingSegment make2DCurve)
        {
            // Check for parent curve. Discard if not found.
            if (make2DCurve?.ParentCurve == null ||
                make2DCurve.ParentCurve.SilhouetteType == SilhouetteType.None)
                return;

            if (make2DCurve.SegmentVisibility == HiddenLineDrawingSegment.Visibility.Hidden
                && this.includeHidden == false) return;

            var crv = make2DCurve.CurveGeometry.DuplicateCurve();

            if (crv == null) return;

            var attr = new ObjectAttributes();

            HiddenLineDrawingObject source = make2DCurve.ParentCurve.SourceObject;
            RhinoObject sourceObj = doc.Objects.Find((Guid)source.Tag);

            if (this.colorBySource)
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

                if (silType == SilhouetteType.SectionCut && includeClipping)
                {
                    attr.LayerIndex = clipIdx;

                }
                else if (silType == SilhouetteType.Boundary ||
                    silType == SilhouetteType.Crease ||
                    silType == SilhouetteType.Tangent ||
                    silType == SilhouetteType.TangentProjects)
                {
                    attr.LayerIndex = outlineIdx;
                    bool segmented = SegmentAndAddToDoc(doc, attr, crv);
                    if (segmented) { return; }
                }
                else if (crvMidConcavity == Concavity.Convex)
                {
                    attr.LayerIndex = convexIdx;
                    bool segmented = SegmentAndAddToDoc(doc, attr, crv);
                    if (segmented) { return; }
                }
                else
                {
                    attr.LayerIndex = concaveIdx;
                }
            }
            // process hidden curves: add them to the hidden layer
            else if (make2DCurve.SegmentVisibility == HiddenLineDrawingSegment.Visibility.Hidden)
            {
                attr.LayerIndex = hiddenIdx;
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
        private bool SegmentAndAddToDoc(RhinoDoc doc, ObjectAttributes attribs, Curve crv)
        {
            //if (intersectionSegments == null) { return false; }
            if (intersectionSegments.Length == 0) { return false; }
            if (BoundingBoxOperations.BoundingBoxCoincides(crv.GetBoundingBox(false),
                intersectionBB) == false) { return false; }
            CurveBooleanDifference crvBD =
                new CurveBooleanDifference(crv, intersectionSegments);
            crvBD.CalculateOverlap();
            Curve[] remaining = crvBD.GetResultCurves();
            Curve[] overlap = crvBD.GetOverlapCurves();

            foreach (Curve remainingCrv in remaining)
            {
                AddtoDoc(doc, remainingCrv, attribs);
            }
            foreach (Curve overlappingCrv in overlap)
            {
                attribs.LayerIndex = concaveIdx;
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
            if (this.includeSilhouette) { level2Lyrs[0] = "WT_Silhouette"; }
            else if (this.includeClipping) { level2Lyrs[0] = "WT_Cut"; }

            LM = new LayerManager(doc);

            LM.Add("WT_Visible", "WT_Make2D");
            LM.Add(level2Lyrs, "WT_Visible");

            LM.GradientWeightAssign(level2Lyrs, 0.15, 1.5);

            if (this.includeHidden)
            {
                LM.Add("WT_Hidden", "WT_Make2D");
                Layer hiddenLyr = LM.GetLayer("WT_Hidden");
                int ltIdx = doc.Linetypes.Find("Hidden");
                if (ltIdx >= 0) { hiddenLyr.LinetypeIndex = ltIdx; }
                hiddenLyr.PlotWeight = 0.1;
            }

            clipIdx = LM.GetIdx("WT_Cut");
            silhouetteIdx = LM.GetIdx("WT_Silhouette");
            hiddenIdx = LM.GetIdx("WT_Hidden");
            outlineIdx = LM.GetIdx("WT_Outline");
            convexIdx = LM.GetIdx("WT_Convex");
            concaveIdx = LM.GetIdx("WT_Concave");
        }



        /// <summary>
        /// Helper method to generate the outline of selected geometry.
        /// </summary>
        private void MakeOutline(RhinoDoc doc)
        {
            MeshOutline outliner = new MeshOutline(objRefs, currentViewport);
            PolylineCurve[] outlines = outliner.GetOutlines();
            GenericMake2D outline2DMaker = new GenericMake2D(outlines, currentViewport,
                includeClipping, includeHidden);
            HiddenLineDrawing outline2D = outline2DMaker.GetMake2D();

            if (outline2D == null)
            {
                return;
            }

            foreach (var make2DCurve in outline2D.Segments)
            {
                // Check for parent curve. Discard if not found.
                if (make2DCurve?.ParentCurve == null ||
                    make2DCurve.ParentCurve.SilhouetteType == SilhouetteType.None)
                    continue;

                var crv = make2DCurve.CurveGeometry.DuplicateCurve();

                var attr = new ObjectAttributes();
                attr.PlotColorSource = ObjectPlotColorSource.PlotColorFromObject;
                attr.ColorSource = ObjectColorSource.ColorFromObject;
                attr.LayerIndex = silhouetteIdx;
                AddtoDoc(doc, crv, attr);
            }
        }



        /// <summary>
        /// Helper method to process intersection curves and compute bounding box and 
        /// an array of unflattened make2D intersection curves.
        /// </summary>
        private void MakeIntersects(RhinoDoc doc)
        {
            if (intersects.Length == 0) return;

            // generate this drawing only if there are intersects
            GenericMake2D createIntersectionMake2D = new GenericMake2D(intersects,
                currentViewport, includeClipping, includeHidden);
            HiddenLineDrawing intersectionMake2D = createIntersectionMake2D.GetMake2D();

            if (intersectionMake2D == null) { return; }

            List<Curve> intersectionSegmentLst = new List<Curve>();
            foreach (var make2DCurve in intersectionMake2D.Segments)
            {
                //Check for parent curve. Discard if not found.
                if (make2DCurve?.ParentCurve == null ||
                    make2DCurve.ParentCurve.SilhouetteType == SilhouetteType.None)
                    continue;

                var crv = make2DCurve.CurveGeometry.DuplicateCurve();
                intersectionSegmentLst.Add(crv);
            }

            intersectionSegments = intersectionSegmentLst.ToArray();
            intersectionBB = intersectionMake2D.BoundingBox(false);
        }
    }
}
