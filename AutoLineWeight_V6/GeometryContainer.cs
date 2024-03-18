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

        public GeometryContainer(ObjRef[] objRefs) 
        { 
            foreach(ObjRef objRef in objRefs)
            {

            }
        }
    }
}
