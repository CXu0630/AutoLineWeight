using Eto.Forms;
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

            var layout = new Eto.Forms.DynamicLayout { Spacing = new Eto.Drawing.Size(5, 5) };
            layout.AddRow(cbox1);
            layout.AddRow(cbox2);
            layout.AddRow(cbox3);
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
            testArgs.testOpt1 = (bool)cbox1.Checked;
            testArgs.testOpt2 = (bool)cbox2.Checked;
            testArgs.testOpt3 = (bool)cbox3.Checked;
            Close(true);
        }

        private void AbortButton_Click(object sender, EventArgs e)
        {
            Close(false);
        }
    }
}
