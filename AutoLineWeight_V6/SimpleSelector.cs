/*
-----------------------------------------------------------------------------------------

This class accesses user-selected geometry. It accepts both preselected and postselected 
geometry, deselecting both after processing. This class can be inherited from as a parent
class for any selector that needs both preselect and postselect. Options can be added as
needed.

-----------------------------------------------------------------------------------------
created 11/30/2023
Ennead Architects

Chloe Xu
chloe.xu@ennead.com
Last edited:01/03/2024

-----------------------------------------------------------------------------------------
*/

using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;

namespace AutoLineWeight_V6
{
    public class SimpleSelector
    {
        string promptMessage = "Select geometry.";
        ObjRef[] selection;

        public SimpleSelector()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static SimpleSelector Instance { get; private set; }

        protected Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Initialize getobject
            GetObject getObject = new GetObject();
            // GO settings
            SetupGetObject(getObject);

            // For loop runs once for each change in options, once for preselected geometry,  
            // and once for postselected geometry
            for (; ; )
            {
                GetResult res = getObject.GetMultiple(1, 0);

                // Case: User selects an option, set all option values to chosen values.
                if (res == GetResult.Option)
                {
                    ModifyOptions(getObject);
                    continue;
                }
                // Case: No geometry is selected.
                else if (res != GetResult.Object)
                {
                    this.Deselect();
                    doc.Views.Redraw();
                    RhinoApp.WriteLine("No valid geometry was selected.");
                    return Result.Cancel;
                }
                // Case: User preselected objects.
                else if (getObject.ObjectsWerePreselected)
                {
                    // this part is very much "if it works, don't touch it"
                    // referenced from McNeel's example classes
                    getObject.EnablePreSelect(false, true);
                    continue;
                }
                // Base case: postselect.
                break;
            }

            RhinoApp.WriteLine("A total of {0} objects were selected.", getObject.ObjectCount);

            selection = getObject.Objects();

            this.Deselect();
            doc.Views.Redraw();

            return Result.Success;
        }

        /// <summary>
        /// Helper method that deselects any selected geometry. Uses an only preselect
        /// GetObject to obtain current selection and deselect.
        /// </summary>
        protected void Deselect()
        {
            GetObject getRemaining = new GetObject();
            getRemaining.EnablePreSelect(true, false);
            getRemaining.EnablePostSelect(false);

            GetResult rem = getRemaining.GetMultiple(1, 0);

            if (rem == GetResult.Object)
            {
                if (getRemaining.ObjectsWerePreselected)
                {
                    for (int i = 0; i < getRemaining.ObjectCount; i++)
                    {
                        RhinoObject obj = getRemaining.Object(i).Object();
                        if (null != obj)
                            obj.Select(false);
                    }
                }
            }
        }

        /// <summary>
        /// Helper method to facilitate inheritance accross classes that implement
        /// user selection with preselect and postselect. Changes GetObject settings
        /// in a modular way.
        /// </summary>
        /// <param name="getObject"> GetObject object to modify </param>
        protected virtual void SetupGetObject(GetObject getObject)
        {
            getObject.GeometryFilter =
                ObjectType.Surface |
                ObjectType.PolysrfFilter |
                ObjectType.Brep |
                ObjectType.Curve;
            getObject.SubObjectSelect = true;
            getObject.SetCommandPrompt(promptMessage);
            getObject.GroupSelect = true;
            getObject.EnableClearObjectsOnEntry(false);
            getObject.EnableUnselectObjectsOnExit(false);
            getObject.DeselectAllBeforePostSelect = false;
        }

        /// <summary>
        /// Helper method to facilitate inheritance accross classes that implement
        /// user selection with preselect and postselect. Changes class properties based
        /// on user options.
        /// </summary>
        /// <param name="getObject"></param>
        protected virtual void ModifyOptions(GetObject getObject) { return; }

        /// <summary>
        /// This method should primarily be used in debug cases or cases where only simple
        /// select is necessary. For more complex situations, inherit from SimpleSelector
        /// and override SetupGetObject and ModifyOptions.
        /// </summary>
        /// <param name="prompt"></param>
        public void SetPrompt(string prompt)
        {
            this.promptMessage = prompt;
        }

        /// <summary>
        /// Run selector code and obtain results.
        /// </summary>
        /// <returns> an array of ObjRefs containing selection results. </returns>
        public Rhino.DocObjects.ObjRef[] GetSelection()
        {
            this.RunCommand(RhinoDoc.ActiveDoc, RunMode.Interactive);
            return this.selection;
        }
    }
}