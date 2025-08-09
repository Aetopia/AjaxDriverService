using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

sealed class MainForm : Form
{
    internal MainForm()
    {
        ClientSize = new(256, 85);
        Text = "NVIDIA Driver Resolver";
        Font = SystemFonts.MessageBoxFont;
        MinimizeBox = false; MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;

        TableLayoutPanel tableLayoutPanel = new()
        {
            Dock = DockStyle.Fill,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            AutoSize = true,
            Enabled = false
        };

        ComboBox comboBox1 = new()
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };

        ComboBox comboBox2 = new()
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };

        Button button1 = new()
        {
            Text = "🢅",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Bottom
        };

        Controls.Add(tableLayoutPanel);

        tableLayoutPanel.RowStyles.Add(new() { SizeType = SizeType.AutoSize });
        tableLayoutPanel.RowStyles.Add(new() { SizeType = SizeType.AutoSize });
        tableLayoutPanel.RowStyles.Add(new() { SizeType = SizeType.AutoSize });

        tableLayoutPanel.Controls.Add(comboBox1, 0, tableLayoutPanel.Controls.Count);
        tableLayoutPanel.Controls.Add(comboBox2, 0, tableLayoutPanel.Controls.Count);
        tableLayoutPanel.Controls.Add(button1, 0, tableLayoutPanel.Controls.Count);

        comboBox1.SelectedIndexChanged += (_, _) =>
        {
            SuspendLayout();

            comboBox2.Items.Clear();
            foreach (var driver in ((Device)comboBox1.SelectedItem).Drivers)
                comboBox2.Items.Add(driver);

            comboBox2.Enabled = comboBox2.Items.Count > 1;
            if (comboBox2.Items.Count > 0) comboBox2.SelectedIndex = 0;

            ResumeLayout();
        };

        button1.Click += async (_, _) =>
        {
            await ((Driver)comboBox2.SelectedItem).GetAsync();
            Close();
        };

        Shown += async (_, _) =>
        {
            SuspendLayout();

            foreach (var device in await AjaxDriverService.DriverManualLookupAsync())
                comboBox1.Items.Add(device);

            tableLayoutPanel.Enabled = comboBox1.Items.Count > 0;
            comboBox1.Enabled = comboBox1.Items.Count > 1;
            if (comboBox1.Items.Count > 0) comboBox1.SelectedIndex = 0;

            ResumeLayout();
        };
    }
}