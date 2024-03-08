using Rhino;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;

namespace AutoLineWeight_V6
{
    public class LayerManager
    {
        RhinoDoc doc;
        // these should be accessible
        Dictionary<string, int> layerIdxs = new Dictionary<string, int>();
        Dictionary<string, Layer> layers = new Dictionary<string, Layer>();

        // these are for internal use only
        Dictionary<string, bool> preexisting = new Dictionary<string, bool>();
        Dictionary<string, Guid> savedGuids = new Dictionary<string, Guid>();

        public LayerManager(RhinoDoc doc)
        {
            this.doc = doc;
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static LayerManager Instance { get; private set; }

        public void Add(string lyrName, string parent)
        {
            if (lyrName == null) return;

            if (parent != null && !layers.ContainsKey(parent))
            {
                this.Add(parent, null);
            }

            Layer layer = doc.Layers.FindName(lyrName);

            if (layer == null)
            {
                preexisting[lyrName] = false;
                layer = new Layer();
                layer.Name = lyrName;
                if (parent != null) { layer.ParentLayerId = GetGuid(parent); }
                // potential bug that parent layer may not exist and hence no Guid may be
                // found is prevented by adding parent layer first.
                doc.Layers.Add(layer);
                layer = doc.Layers.FindName(lyrName);
            }
            else
            {
                try { preexisting.Add(lyrName, true); }
                catch { } // do nothing if preexisting already contains lyrName
            }

            layers[lyrName] = layer;
            layerIdxs[lyrName] = layer.Index;
        }

        public void Add(string[] lyrName, string parent)
        {
            foreach (string lyr in lyrName) { Add(lyr, parent); }
        }

        /// <summary>
        /// Assigns lineweights to layers represented by an array of strings containing 
        /// their names. Lineweights assigned according to the equation: 
        /// 
        /// w = x * (n - i) ^ 1.5
        /// 
        /// where n is the number of layers and i is the index of the layer in the array
        /// of strings.
        /// 
        /// any space (" ") within layer names will be interpreted as a break point 
        /// between two or more layer names which are intended to share the same weight.
        /// </summary>
        /// <param name="lyrs"> array of strings containing layer names, spaces within 
        /// strings will be interpreted as a breakpoint between two layers that share the
        /// same weight.</param>
        public void GradientWeightAssign(string[] lyrs, double x, double y)
        {
            int n = lyrs.Length;
            for (int i = 0; i < n; i++)
            {
                string lyrName = lyrs[i];
                if (lyrName == null) continue;

                double w = x * Math.Pow((n - i), y);

                string[] coLevel = lyrName.Split(' ');
                foreach (string subLyr in coLevel)
                {
                    Layer lyr = GetLayer(subLyr);
                    if (lyr == null) continue;
                    bool preexists = true;
                    preexisting.TryGetValue(lyrName, out preexists);
                    if (!preexists) { lyr.PlotWeight = w; }
                }
            }
        }

        public Dictionary<string, Layer> GetLayers()
        {
            return this.layers;
        }

        public Dictionary<string, int> GetLayerIdxs()
        {
            return this.layerIdxs;
        }

        public Layer GetLayer(string lyrName)
        {
            Layer lyr;
            try { lyr = layers[lyrName]; }
            catch
            {
                lyr = doc.Layers.FindName(lyrName);
            }
            return lyr;
        }

        public Guid GetGuid(string lyrName)
        {
            Guid guid;
            if (!savedGuids.TryGetValue(lyrName, out guid))
            {
                Layer lyr = GetLayer(lyrName);
                if (lyr == null) { return guid; }
                // !!!!!! POTENTIAL BUG !!!!!!
                // what happens when trying to set an unassigned guid as the parent
                // of a layer?
                guid = lyr.Id;
            }
            savedGuids[lyrName] = guid;
            return guid;
        }

        public int GetIdx(string lyrName)
        {
            int idx;
            try { idx = layerIdxs[lyrName]; }
            catch
            {
                Layer lyr = doc.Layers.FindName(lyrName);
                if (lyr == null) { idx = -1; }
                else { idx = lyr.Index; }
            }
            return idx;
        }
    }
}