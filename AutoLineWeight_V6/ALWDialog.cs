using Eto.Forms;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoLineWeight_V6
{
    internal class ALWDialog : Eto.Forms.Dialog<bool>
    {
        ALWOptions args;
        Eto.Forms.CheckBox colorCheckBox;
        Eto.Forms.CheckBox intersectCheckBox;
        Eto.Forms.CheckBox meshBrepCheckBox;
        Eto.Forms.CheckBox clipCheckBox;
        Eto.Forms.CheckBox hidCheckBox;
        Eto.Forms.CheckBox silCheckBox;

        Eto.Forms.Label clipNote;

        public ALWDialog(ALWOptions args)
        {
            this.args = args ?? throw new ArgumentNullException(nameof(args));

            Title = "Weighted Make2D Options";
            Padding = new Eto.Drawing.Padding(5);

            DynamicLayout layout = new Eto.Forms.DynamicLayout
            {
                Padding = new Eto.Drawing.Padding(5),
                Spacing = new Eto.Drawing.Size(5, 5)
            };

            layout.AddRow(CreateCheckBoxes());
            layout.AddRow(null);
            layout.AddRow(CreateButtons());

            Content = layout;
        }

        public ALWOptions Results => args;

        private Eto.Forms.DynamicLayout CreateCheckBoxes()
        {
            colorCheckBox = new Eto.Forms.CheckBox
            {
                Text = "Color lines by source object",
                Checked = args.colorBySrc,
                ThreeState = false
            };

            intersectCheckBox = new Eto.Forms.CheckBox
            {
                Text = "Include object intersections",
                Checked = args.addIntersect,
                ThreeState = false
            };

            meshBrepCheckBox = new Eto.Forms.CheckBox
            {
                Text = "Include intersections between" +
                "\n" +
                "meshes and polysurfaces / surfaces",
                Checked = args.meshBrep,
                ThreeState = false
            };

            clipCheckBox = new Eto.Forms.CheckBox
            {
                Text = "Include clipping planes",
                Checked = args.addClip,
                ThreeState = false
            };

            hidCheckBox = new Eto.Forms.CheckBox
            {
                Text = "Include hidden lines",
                Checked = args.addHid,
                ThreeState = false
            };

            silCheckBox = new Eto.Forms.CheckBox
            {
                Text = "Include scene silhouette",
                Checked = args.addSil,
                ThreeState = false
            };

            clipNote = new Eto.Forms.Label
            {
                Text = "Silhouette and clipping planes are " +
                "\nmutually exclusive, choose at most " +
                "\none of the two to generate.",
                TextColor = Eto.Drawing.Colors.LightGrey
            };

            // ensure that the starting state of the dependent fields are correct
            if (args.addSil)
            {
                clipCheckBox.Checked = false;
                args.addClip = false;
                clipCheckBox.Enabled = false;
            }

            if (!args.addIntersect)
            {
                meshBrepCheckBox.Checked = false;
                args.meshBrep = false;
                meshBrepCheckBox.Enabled = false;
            }

            // listeners for check box changes
            intersectCheckBox.CheckedChanged += (sender, e) =>
            {
                if (intersectCheckBox.Checked.GetValueOrDefault())
                {
                    meshBrepCheckBox.Enabled = true;
                }
                else
                {
                    meshBrepCheckBox.Checked = false;
                    args.meshBrep = false;
                    meshBrepCheckBox.Enabled = false;
                }
            };

            silCheckBox.CheckedChanged += (sender, e) =>
            {
                if (silCheckBox.Checked.GetValueOrDefault())
                {
                    clipCheckBox.Checked = false;
                    args.addClip = false;
                    clipCheckBox.Enabled = false;
                }
                else
                {
                    clipCheckBox.Enabled = true;
                }
            };

            var layout = new Eto.Forms.DynamicLayout { Spacing = new Eto.Drawing.Size(5, 5) };
            layout.AddRow(colorCheckBox);
            layout.AddRow(intersectCheckBox);
            layout.BeginVertical(Padding = new Eto.Drawing.Padding(20, 0, 0, 0));
            layout.AddRow(meshBrepCheckBox);
            layout.EndVertical();
            layout.AddRow(silCheckBox);
            layout.AddRow(clipCheckBox);
            layout.AddRow(clipNote);
            layout.AddRow(hidCheckBox);
            return layout;
        }

        private Eto.Forms.DynamicLayout CreateButtons()
        {
            DefaultButton = new Eto.Forms.Button { Text = "OK" };
            DefaultButton.Click += DefaultButton_Click;

            AbortButton = new Eto.Forms.Button { Text = "Cancel" };
            AbortButton.Click += AbortButton_Click;

            var layout = new Eto.Forms.DynamicLayout { Spacing = new Eto.Drawing.Size(5, 5) };
            layout.AddRow(null, DefaultButton, AbortButton, null);
            return layout;
        }

        private void DefaultButton_Click(object sender, EventArgs e)
        {
            args.colorBySrc = (bool)colorCheckBox.Checked;
            args.addIntersect = (bool)intersectCheckBox.Checked;
            args.meshBrep = (bool)meshBrepCheckBox.Checked;
            args.addClip = (bool)clipCheckBox.Checked;
            args.addHid = (bool)hidCheckBox.Checked;
            args.addSil = (bool)silCheckBox.Checked;
            Close(true);
        }

        private void AbortButton_Click(object sender, EventArgs e)
        {
            Close(false);
        }
    }
}
