/*
-----------------------------------------------------------------------------------------
created 11/30/2023

Chloe Xu
guangyu.xu0630@gmail.com
Last edited:03/20/2024
-----------------------------------------------------------------------------------------
*/

using Rhino;
using Rhino.DocObjects;
using Rhino.Input.Custom;

namespace AutoLineWeight_V6
{
    /// <summary>
    /// Accesses user-selected geometry customized for use in WeightedMake2D and 
    /// implements options specific to WeightedMake2D.
    /// </summary>
    public class ALWSelector : SimpleSelector
    {
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
        }
    }
}