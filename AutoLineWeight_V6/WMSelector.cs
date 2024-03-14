/*
-----------------------------------------------------------------------------------------

This class accesses user-selected geometry. It accepts both preselected and postselected 
geometry, deselecting both after processing. This class is customized for use in 
WeightedMake2D and implements options specific to WeightedMake2D.

-----------------------------------------------------------------------------------------
created 11/30/2023
Ennead Architects

Chloe Xu
chloe.xu@ennead.com
Last edited:01/03/2024

-----------------------------------------------------------------------------------------
*/

using Rhino;
using Rhino.DocObjects;
using Rhino.Input.Custom;

namespace AutoLineWeight_V6
{
    public class WMSelector : SimpleSelector
    {
        // initialize user options
        public bool includeClipping {  get; set; }
        public bool includeHidden { get; set; }
        public bool includeSceneSilhouette {  get; set; }
        public bool includeIntersect {  get; set; }
        public bool colorBySource {  get; set; }

        // initialize option toggles
        // these are set as global variables to facilitate access
        private OptionToggle optIncludeClipping;
        private OptionToggle optIncludeHidden;
        private OptionToggle optSceneSilhouette;
        private OptionToggle optIntersect;
        private OptionToggle optColorBySource;

        /// <summary>
        /// Custom setup method for WMSelector.
        /// </summary>
        protected override void SetupGetObject(GetObject getObject)
        {
            // getobject settings
            getObject.GeometryFilter =
                ObjectType.Surface |
                ObjectType.PolysrfFilter |
                ObjectType.Brep |
                ObjectType.Mesh |
                ObjectType.Curve;
            getObject.SubObjectSelect = true;
            getObject.GroupSelect = true;
            getObject.DeselectAllBeforePostSelect = false;
            getObject.EnableClearObjectsOnEntry(false);
            getObject.EnableUnselectObjectsOnExit(false);
            getObject.SetCommandPrompt("Select geometry for the weighted make2d");

            // create option toggles
            optColorBySource = new OptionToggle(colorBySource, "Off", "On");
            optIntersect = new OptionToggle(includeIntersect, "Off", "On");
            optIncludeClipping = new OptionToggle(includeClipping, "Off", "On");
            optIncludeHidden = new OptionToggle(includeHidden, "Off", "On");
            optSceneSilhouette = new OptionToggle(includeSceneSilhouette, "Off", "On");

            // add option toggles to getObject
            getObject.AddOptionToggle("Color_By_Source", ref optColorBySource);
            getObject.AddOptionToggle("Calculate_Intersections", ref optIntersect);
            getObject.AddOptionToggle("Include_Scene_Silhouette", ref optSceneSilhouette);
            getObject.AddOptionToggle("Include_Clipping_Planes", ref optIncludeClipping);
            getObject.AddOptionToggle("Include_Hidden_Lines", ref optIncludeHidden);

            // set warning
            RhinoApp.WriteLine("WARNING: for the current version, including clipping planes " +
                "and generating silhouettes are mutually exclusive. If both are enabled, " +
                "scene silhouettes will be prioritized. Please process these separately.");
        }

        /// <summary>
        /// Custom property updator for WMSelector. Updates properties based on user 
        /// option input.
        /// </summary>
        protected override void ModifyOptions(GetObject getObject)
        {
            this.colorBySource = optColorBySource.CurrentValue;
            this.includeIntersect = optIntersect.CurrentValue;
            this.includeClipping = optIncludeClipping.CurrentValue;
            this.includeHidden = optIncludeHidden.CurrentValue;
            this.includeSceneSilhouette = optSceneSilhouette.CurrentValue;
        }

        public void SetDefaultValues (bool clipping, bool hidden, bool silhouette, 
            bool intersect, bool colorBySource)
        {
            this.colorBySource = colorBySource;
            this.includeIntersect = intersect;
            this.includeClipping = clipping;
            this.includeHidden = hidden;
            this.includeSceneSilhouette = silhouette;
        }
    }
}