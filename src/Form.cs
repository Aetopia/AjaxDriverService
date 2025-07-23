using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
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
        ClientSize = new(400 / 2, 300 / 2);

        TableLayoutPanel tableLayoutPanel = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Enabled = false
        };

        Controls.Add(tableLayoutPanel);

        ComboBox comboBox = new()
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        tableLayoutPanel.RowStyles.Add(new() { SizeType = SizeType.AutoSize });
        tableLayoutPanel.Controls.Add(comboBox, 0, tableLayoutPanel.Controls.Count);

        var buttons = new Button[4];
        Dictionary<string, Dictionary<string, string>> collection = [];

        for (int index = 0; index < buttons.Length; index++)
        {
            Button button = buttons[index] = new()
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Enabled = false
            };

            button.Click += (sender, args) => { using (Process.Start((string)((Button)sender).Tag)) { } };

            tableLayoutPanel.RowStyles.Add(new()
            {
                SizeType = SizeType.Percent,
                Height = 25
            });
            tableLayoutPanel.Controls.Add(button, 0, tableLayoutPanel.Controls.Count);
        }

        buttons[0].Text = "Standard";
        buttons[1].Text = "Standard Studio";
        buttons[2].Text = "DCH";
        buttons[3].Text = "DCH Studio";

        comboBox.SelectedIndexChanged += (_, _) =>
        {
            SuspendLayout();

            if (!collection.TryGetValue((string)comboBox.SelectedItem, out var item))
                return;

            foreach (var button in buttons)
            {
                if (!item.TryGetValue(button.Text, out var value))
                {
                    button.Enabled = false;
                    button.Tag = null;
                    continue;
                }

                button.Enabled = true;
                button.Tag = value;
            }

            ResumeLayout();
        };

        Load += async (_, _) =>
        {
            SuspendLayout();

            foreach (var item in collection = await Drivers.GetAsync())
                comboBox.Items.Add(item.Key);

            if (tableLayoutPanel.Enabled = collection.Any())
                comboBox.SelectedIndex = 0;

                tableLayoutPanel.Enabled = true;

                ResumeLayout();
        };
    }
}