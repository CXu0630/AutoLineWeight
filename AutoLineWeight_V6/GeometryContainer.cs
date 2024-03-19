using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace AutoLineWeight_V6
{
    internal class GeometryContainer
    {
        public List<Brep> breps = new List<Brep>();
        public List<Mesh> meshes = new List<Mesh>();
        public List<Curve> curves = new List<Curve>();
        public List<InstanceObject> blocks = new List<InstanceObject>();
        public List<string> blockIds = new List<string>();

        public GeometryContainer(ObjRef[] objRefs) 
        { 
            foreach(ObjRef objRef in objRefs)
            {
                Brep brep = objRef.Brep();
                if (brep != null)
                {
                    breps.Add(brep);
                    continue;
                }

                Mesh mesh = objRef.Mesh();
                if (mesh != null)
                {
                    meshes.Add(mesh);
                    continue;
                }

                Curve crv = objRef.Curve();
                if (crv != null)
                {
                    curves.Add(crv);
                    continue;
                }

                RhinoObject obj = objRef?.Object();
                if (obj.ObjectType == ObjectType.InstanceReference)
                {
                    InstanceObject iref = obj as InstanceObject;
                    Guid id = iref.Id;
                    if (blockIds.Contains(id.ToString())) { continue; }
                    blockIds.Add(id.ToString());
                    blocks.Add(iref);
                }
            }
        }

        public GeometryContainer(RhinoObject[] objs)
        {
            foreach(RhinoObject obj in objs)
            {
                if (obj.ObjectType == ObjectType.Brep)
                {
                    breps.Add((Brep)obj.Geometry);
                }

                if (obj.ObjectType == ObjectType.Curve)
                {
                    curves.Add((Curve)obj.Geometry);
                }

                if (obj.ObjectType == ObjectType.Mesh)
                {
                    curves.Add((Curve)obj.Geometry);
                }
            }
        }
    }
}
