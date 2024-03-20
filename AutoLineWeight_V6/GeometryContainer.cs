/*
-----------------------------------------------------------------------------------------
created 03/16/2024

Chloe Xu
guangyu.xu0630@gmail.com
Last edited:03/20/2024
-----------------------------------------------------------------------------------------
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace AutoLineWeight_V6
{
    /// <summary>
    /// This class stores RhinoObjects parsed from ObjRefs. Each type of RhinoObject is 
    /// explicitly and separately stored so that functionality specific to each type of
    /// object can be easily accessed.
    /// </summary>
    internal class GeometryContainer
    {
        public List<Brep> breps = new List<Brep>();
        public List<Mesh> meshes = new List<Mesh>();
        public List<Curve> curves = new List<Curve>();
        public List<InstanceObject> blocks = new List<InstanceObject>();
        public List<string> blockIds = new List<string>();

        public bool includeRenderMesh = false;
        public List<Mesh> renderMeshes = new List<Mesh>();

        /// <summary>
        /// Creates a new instance of an empity GeometryContainer
        /// </summary>
        public GeometryContainer(bool incRenderMesh = false) 
        { 
            this.includeRenderMesh = incRenderMesh; 
        }

        /// <summary>
        /// Creates a new instance of GeometryContainer. Guids of original geometry are
        /// set within the userdictionary of each object to trace back to original object.
        /// </summary>
        /// <param name="objRefs"> array of ObjRefs to parse and store within 
        /// GeometryContainer </param>
        public GeometryContainer(ObjRef[] objRefs, bool incRenderMesh = false) 
        { 
            foreach(ObjRef objRef in objRefs)
            {
                Brep brep = objRef.Brep();
                if (brep != null)
                {
                    brep.UserDictionary.Set("GUID", objRef.ObjectId);
                    breps.Add(brep);
                    continue;
                }

                Curve crv = objRef.Curve();
                if (crv != null)
                {
                    crv.UserDictionary.Set("GUID", objRef.ObjectId);
                    curves.Add(crv);
                    continue;
                }

                Mesh mesh = objRef.Mesh();
                if (mesh != null)
                {
                    mesh.UserDictionary.Set("GUID", objRef.ObjectId);
                    meshes.Add(mesh);
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

            // process render meshes
            if (incRenderMesh) GetRenderMeshes();
        }

        /// <summary>
        /// Creates a new instance of GeometryContainer. Constructor usually used for 
        /// merging GeometryContainers.
        /// </summary>
        /// <param name="breps"></param>
        /// <param name="meshes"></param>
        /// <param name="curves"></param>
        /// <param name="blocks"></param>
        /// <param name="blockIds"></param>
        public GeometryContainer(List<Brep> breps, List<Mesh> meshes, List<Curve> curves, 
            List<InstanceObject> blocks, List<string> blockIds, List<Mesh> renderMeshes, 
            bool includeRenderMesh = false)
        {
            // if the input for any of the fields is null, then use an empity list
            this.breps = breps ?? this.breps;
            this.meshes = meshes ?? this.meshes;
            this.curves = curves ?? this.curves;
            this.blocks = blocks ?? this.blocks;
            this.blockIds = blockIds ?? this.blockIds;

            this.renderMeshes = renderMeshes ?? this.renderMeshes;
            this.includeRenderMesh = includeRenderMesh;
        }

        /// <summary>
        /// Get render meshes of all the breps within the container.
        /// </summary>
        public void GetRenderMeshes()
        {
            this.includeRenderMesh = true;

            RhinoObject[] brepROs = new RhinoObject[breps.Count];
            for (int i = 0; i < breps.Count; i++)
            {
                Guid guid;
                breps[i].UserDictionary.TryGetGuid("GUID", out guid);
                brepROs[i] = new ObjRef(guid).Object();
            }

            ObjRef[] rMeshRefs = RhinoObject.GetRenderMeshes(brepROs, true, true);
            renderMeshes = Array.ConvertAll(rMeshRefs, rMeshRef => rMeshRef.Mesh()).ToList();
        }

        /// <summary>
        /// Recursive method that extracts nested geometry within blocks (InstanceObjects)
        /// stored within the GeometryContainer. Does not change original GeometryContainer.
        /// </summary>
        /// <returns> A new GeometryContainer with all blocks and nested blocks exploded. </returns>
        public GeometryContainer ExplodeBlocks()
        {
            // base case: when there are no blocks, return
            if(blocks.Count == 0) return this;
            
            GeometryContainer mergedGC = MergeContainer(new GeometryContainer());
            mergedGC.blocks = new List<InstanceObject>();
            mergedGC.blockIds = new List<string>();

            foreach (InstanceObject block in blocks)
            {
                Transform xform = block.InstanceXform;
                InstanceDefinition idef = block.InstanceDefinition;
                if (idef != null)
                {
                    Guid[] objIds = idef.GetObjectIds();
                    ObjRef[] objRefs = Array.ConvertAll(objIds, id => new ObjRef(id));
                    GeometryContainer cBlock = new GeometryContainer(objRefs, includeRenderMesh);
                    
                    // recursive case: there are nested blocks
                    if(cBlock.blocks.Count > 0)
                    {
                        cBlock = cBlock.ExplodeBlocks();
                    }

                    // blocks are transformed after they are returned
                    foreach(Brep brep in cBlock.breps) { brep.Transform(xform); }
                    foreach(Curve curve in cBlock.curves) { curve.Transform(xform); }
                    foreach(Mesh mesh in cBlock.meshes) { mesh.Transform(xform); }
                    foreach (Mesh mesh in cBlock.renderMeshes) { mesh.Transform(xform); }

                    mergedGC = mergedGC.MergeContainer(cBlock);
                }
            }
            return mergedGC;
        }

        /// <summary>
        /// Merges this GeometryContainer with the geometry from another container. Does
        /// not alter the geometry of input containers or this container.
        /// </summary>
        /// <param name="container"></param>
        /// <returns> A new GeometryContainer with merged data. </returns>
        public GeometryContainer MergeContainer (GeometryContainer container)
        {
            List<Brep> newBreps = new List<Brep>(breps);
            List<Curve> newCrvs = new List<Curve>(curves);
            List<Mesh> newMeshes = new List<Mesh>(meshes);
            List<InstanceObject> newBlocks = new List<InstanceObject>(blocks);
            List<string> newBlockIds = new List<string>(blockIds);
            List<Mesh> newRMeshes = new List<Mesh>(renderMeshes);

            newBreps.AddRange(container.breps);
            newCrvs.AddRange(container.curves);
            newMeshes.AddRange(container.meshes);
            newBlocks.AddRange(container.blocks);
            newBlockIds.AddRange(container.blockIds);
            newRMeshes.AddRange(container.renderMeshes);

            return new GeometryContainer(newBreps, newMeshes, newCrvs, newBlocks, 
                newBlockIds, newRMeshes, includeRenderMesh || container.includeRenderMesh);
        }

        /// <summary>
        /// Returns the total number of geometries stored, not counting geometries nested
        /// in blocks.
        /// </summary>
        /// <returns> total number of geometries </returns>
        public int GetCount()
        {
            return GetGeoCount() + blocks.Count;
        }

        public int GetGeoCount()
        {
            return breps.Count + curves.Count + meshes.Count;
        }

        public GeometryBase[] GetAllGeometries()
        {
            GeometryBase[] allGeo = new GeometryBase[GetCount() - blocks.Count];
            int i = 0;
            foreach (Brep brep in breps) { allGeo[i] = brep; i++; }
            foreach (Curve crv in curves) { allGeo[i] = crv; i++; }
            foreach (Mesh mesh in meshes) { allGeo[i] = mesh; i++; }
            return allGeo;
        }
    }
}
