﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace OpenCNCPilot
{
	public partial class GrblSettingsWindow : Window
    {
		Dictionary<int, double> CurrentSettings = new Dictionary<int, double>();
		Dictionary<int, TextBox> SettingsBoxes = new Dictionary<int, TextBox>();

		public event Action<string> SendLine;

        public GrblSettingsWindow()
        {
            InitializeComponent();
        }

		static Regex settingParser = new Regex(@"\$([0-9]+)=([0-9\.]+)");
		public void LineReceived(string line)
		{
			if (!line.StartsWith("$"))
				return;

			try
			{
				Match m = settingParser.Match(line);
				int number = int.Parse(m.Groups[1].Value);
				double value = double.Parse(m.Groups[2].Value, Util.Constants.DecimalParseFormat);

				if (!CurrentSettings.ContainsKey(number))
				{
					RowDefinition rowDef = new RowDefinition();
					rowDef.Height = new GridLength(25);
					gridMain.RowDefinitions.Add(rowDef);

					TextBox valBox = new TextBox
					{
						Text = value.ToString(Util.Constants.DecimalOutputFormat),
						VerticalAlignment = VerticalAlignment.Center
					};
					Grid.SetRow(valBox, gridMain.RowDefinitions.Count - 1);
					Grid.SetColumn(valBox, 1);
					gridMain.Children.Add(valBox);

					if (Util.GrblCodeTranslator.Settings.ContainsKey(number))
					{
						Tuple<string, string, string> labels = Util.GrblCodeTranslator.Settings[number];

						Label name = new Label
						{
							Content = labels.Item1,
							HorizontalAlignment = HorizontalAlignment.Right
						};
						Grid.SetRow(name, gridMain.RowDefinitions.Count - 1);
						Grid.SetColumn(name, 0);
						gridMain.Children.Add(name);

						valBox.ToolTip = $"{labels.Item1} ({labels.Item2}):\n{labels.Item3}";
					}

					CurrentSettings.Add(number, value);
					SettingsBoxes.Add(number, valBox);
				}
				else
				{
					SettingsBoxes[number].Text = value.ToString(Util.Constants.DecimalOutputFormat);
				}
			}
			catch { }
		}

		private void ButtonApply_Click(object sender, RoutedEventArgs e)
		{
			List<Tuple<int, double>> ToSend = new List<Tuple<int, double>>();

			foreach(KeyValuePair<int, double> kvp in CurrentSettings)
			{
				double newval;

				if (!double.TryParse(SettingsBoxes[kvp.Key].Text, System.Globalization.NumberStyles.Float, Util.Constants.DecimalParseFormat, out newval))
				{
					MessageBox.Show($"Value \"{SettingsBoxes[kvp.Key].Text}\" is invalid for Setting \"{Util.GrblCodeTranslator.Settings[kvp.Key].Item1}\"");
					return;
				}

				if (newval == kvp.Value)
					continue;

				ToSend.Add(new Tuple<int, double>(kvp.Key, newval));
			}

			if (SendLine == null)
				return;

			foreach (Tuple<int, double> setting in ToSend)
			{
				SendLine.Invoke($"${setting.Item1}={setting.Item2.ToString(Util.Constants.DecimalOutputFormat)}");
				CurrentSettings[setting.Item1] = setting.Item2;
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			e.Cancel = true;
			Hide();
		}
	}
}
