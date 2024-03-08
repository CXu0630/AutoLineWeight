# AutoLineWeight
This is a project for a Rhino plugin that creates weighted 2D representations of 3D geometry based on formal relationships between source edges and their adjacent faces.
If an edge only has one adjacent face, or one of its two adjacent faces is hidden, it is defined as an "outline". If both faces are present and the line is on a convex corner, it is defined as "convex". All other visible lines are defined as "concave". Hidden lines are also processed. Results are baked onto layers according to their assigned weight.
