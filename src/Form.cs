using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

sealed class Form : System.Windows.Forms.Form
{
    internal Form()
    {
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = MaximizeBox = false;
        Font = SystemFonts.MessageBoxFont;

        Text = "NVIDIA Driver Resolver";
        ClientSize = new(256, 85);
        MaximumSize = MinimumSize = Size;

        TableLayoutPanel tableLayoutPanel = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Enabled = false
        };

        Controls.Add(tableLayoutPanel);

        ComboBox comboBox1 = new()
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        tableLayoutPanel.RowStyles.Add(new() { SizeType = SizeType.AutoSize });
        tableLayoutPanel.Controls.Add(comboBox1, 0, 0);

        ComboBox comboBox2 = new()
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        tableLayoutPanel.RowStyles.Add(new() { SizeType = SizeType.AutoSize });
        tableLayoutPanel.Controls.Add(comboBox2, 0, 1);

        Button button = new()
        {
            Text = "🢅",
            Dock = DockStyle.Bottom,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        tableLayoutPanel.RowStyles.Add(new() { SizeType = SizeType.AutoSize });
        tableLayoutPanel.Controls.Add(button, 0, 2);

        Dictionary<string, Dictionary<string, string>> collection = [];

        comboBox1.SelectedIndexChanged += async (_, _) =>
        {
            SuspendLayout(); try
            {
                await Task.Yield();

                comboBox2.Items.Clear();

                if (!collection.TryGetValue((string)comboBox1.SelectedItem, out var value))
                {
                    comboBox2.Enabled = false;
                    return;
                }

                foreach (var item in value)
                {
                    await Task.Yield();
                    comboBox2.Items.Add(item.Key);
                }

                comboBox2.Enabled = comboBox2.Items.Count > 1;
                comboBox2.SelectedIndex = 0;
            }
            finally { ResumeLayout(); }
        };

        button.Click += async (_, _) =>
        {
            tableLayoutPanel.Enabled = false;
            try
            {
                if (!collection.TryGetValue((string)comboBox1.SelectedItem, out var item))
                    return;

                if (!item.TryGetValue((string)comboBox2.SelectedItem, out var value))
                    return;

                using (Process.Start(value)) { }
            }
            finally { tableLayoutPanel.Enabled = true; }
        };

        Load += async (_, _) =>
        {
            await Task.Yield();
            collection = await Drivers.GetAsync();

            SuspendLayout(); try
            {
                foreach (var item in collection)
                {
                    await Task.Yield();
                    comboBox1.Items.Add(item.Key);
                }

                comboBox1.Enabled = comboBox1.Items.Count > 1;
                comboBox1.SelectedIndex = 0;

                tableLayoutPanel.Enabled = true;
            }
            finally { ResumeLayout(); }
        };
    }
}