﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Sync.Tools;
using Sync.Tools.ConfigGUI;

namespace ConfigGUI.ConfigurationRegion.ConfigurationItemCreators
{
    class FontConfigurationItemCreator:ConfigurationItemCreatorBase
    {
        public override Panel CreateControl(BaseConfigurationAttribute attr, PropertyInfo prop, object configuration_instance)
        {
            var font_str = Tools.GetConigValue(prop, configuration_instance);
            FontAttribute fattr = attr as FontAttribute;

            var panel = base.CreateControl(attr, prop, configuration_instance);
            var font_box = new TextBox() { Text = font_str, Width = 160, VerticalContentAlignment = VerticalAlignment.Center };
            var button = new Button() { Content = DefaultLanguage.BUTTON_FONT, Width = 75, Margin = new Thickness(5, 0, 5, 0) };

            panel.Children.Add(font_box);
            panel.Children.Add(button);

            button.Click += (s, e) =>
            {
                var fontDialog = new System.Windows.Forms.FontDialog();
                font_str = Tools.GetConigValue(prop, configuration_instance);

                fontDialog.Font = new System.Drawing.Font(font_str, 20);

                if (fontDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    font_box.Text = fontDialog.Font.Name;
                prop.SetValue(configuration_instance, new ConfigurationElement($"{font_box.Text}"));
            };
            font_box.LostFocus += (s, e) =>
            {
                if (fattr.Check(font_box.Text))
                    prop.SetValue(configuration_instance, new ConfigurationElement($"{font_box.Text}"));
            };

            return panel;
        }
    }
}
