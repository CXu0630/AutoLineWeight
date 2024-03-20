This is a Rhino plugin that creates weighted 2D representations of 3D geometry based on formal relationships between source edges and their adjacent faces. The plugin uses a single command that is a variation of the Make2D command:

# **WeightedMake2D**


## **Download**
https://www.food4rhino.com/en/app/auto-line-weight?lang=en

## Line weights are assigned according to the following **rules**: 

- If an edge only has one adjacent face, or one of its two adjacent faces is hidden, it is defined as an "outline".
- If both faces are present and the line is on a convex corner (mountain), it is defined as "convex".
- All other visible lines are defined as "concave" (on a valley or on a flat surface).

## **Additional Options**:

- **Color by source**: output lines are colored the same as their source objects (by layer if false)
- **Calculate intersections**: intersection lines between objects can be drawn so that there is no need to boolean union geometry
- **Mesh x Brep intersections**: include intersections between mesh and brep objects (will be false if intersections are not calculated)
- **Include scene silhouette**: add a silhouette (MeshOutline) to the drawing
- **Include clipping planes**: use clipping planes active in the viewport to clip geometry when generating drawing
- **Include hidden lines**: add lines not visible from the viewport in a separate layer
  
## **Notes**:

- If you find your WeightedMake2D too slow, consider turning off the options to calculate intersections and include scene silhouette. These are essentially the same as the Intersect and MeshOutline commands in Rhino and will significantly slow down WeightedMake2D. With these turned off, your output will be similar to line weights in the Pen Viewport, but with additional differentiation between concave and convex lines.
- For the current version (1.1), the scene silhouette and clipping plane options are mutually exclusive. Scene silhouette, when enabled, will be prioritized.
- For the current version (1.1), the plugin can run for Rhino 7 but SubD does not work properly. I am working to rectify this.
- For the current version (1.1), Mesh does not include concavity information. I am working to rectify this.

As this plugin is based on Make2D, you can expect about as much accuracy and limitations as the Make2D command. Your drawings will still need to be processed after using this plugin. Formal relationships defined for assigning line weights are based largely on my personal preferences and previous drawings I've been asked to create. These preferences may vary by person and by case. 

This plugin is a work in progress, and **I'm open to feedback on bugs or feature requests** (or just say hi :D).

## **Version Records**:

1.0
- published on food4rhino
- core functionality of generating weighted drawings for breps

1.1
- source code available
- type-explicit geometry storing
- block and nested block support
- mesh x brep (polysurface) intersection support
