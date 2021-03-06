﻿using ConfigGUI.ConfigurationI18n;
using ConfigGUI.ConfigurationRegion;
using Sync.Plugins;
using Sync.Tools;
using Sync.Tools.ConfigurationAttribute;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ConfigGUI
{
    /// <summary>
    /// ConfigWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigWindow : Window
    {
        private ConfigurationItemFactory m_item_factory;
        private IEnumerable<PluginConfigurationManager> m_pluginConfigurationManagers;

        private static ConfigWindow s_window_instance;
        private static bool s_initialize = false;

        private static bool s_requireRestart = false;
        public  static bool RequireRestart
        {
            get => s_requireRestart;
            set
            {
                if (s_initialize == true)
                {
                    s_requireRestart = value | s_requireRestart;
                    if (value)
                        s_window_instance.Title = $"{DefaultLanguage.WINDOW_TITLE} ({DefaultLanguage.WINDOW_TITLE_REQUIRE_RESTART})";
                }
            }
        }

        public ConfigWindow(ConfigurationItemFactory itemFactory)
        {
            m_item_factory = itemFactory;
            s_window_instance = this;

            InitializeComponent();
            InitializeConfigPanel();

            Title = DefaultLanguage.WINDOW_TITLE;
            Save_Button.Content = DefaultLanguage.BUTTON_SAVE;

            s_initialize = true;
        }

        #region Plugins Config

        private void InitializeConfigPanel()
        {
            Type config_manager_type = typeof(PluginConfigurationManager);
            m_pluginConfigurationManagers = config_manager_type.GetField("ConfigurationSet", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null) as IEnumerable<PluginConfigurationManager>;

            List<TreeViewItem> tree_view_list = new List<TreeViewItem>();
            //each configuration manager
            foreach (var manager in m_pluginConfigurationManagers)
            {
                //get plguin name
                string plugin_name = config_manager_type.GetField("name", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(manager) as string;
                Plugin plugin = manager.GetType().GetField("instance", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(manager) as Plugin;

                var tree_item = new TreeViewItem() { Header = plugin_name };
                if(plugin != null)
                {
                    tree_item.Selected += (s, e) =>
                    {
                        configRegion.Content = GetPluginInformationPanel(plugin);
                    };
                }

                //get List<PluginConfiuration>
                var config_items_field = config_manager_type.GetField("items", BindingFlags.NonPublic | BindingFlags.Instance);
                var config_items_list = config_items_field.GetValue(manager);

                //List<PluginConfiuration>.GetEnumerator
                var enumerator = config_items_field.FieldType.GetMethod("GetEnumerator", BindingFlags.Public | BindingFlags.Instance)
                    .Invoke(config_items_list, null) as IEnumerator;

                //each List<PluginConfiuration>
                while (enumerator.MoveNext())
                {
                    var config_item = enumerator.Current;
                    var config_instance = config_item.GetType()
                        .GetField("config", BindingFlags.NonPublic | BindingFlags.Instance)
                        .GetValue(config_item);
                    var config_type = config_instance.GetType();
                    var holder_attr = config_type.GetCustomAttribute<ConfigurationHolderAttribute>();

                    //Create config panle
                    var panle = GetConfigPanel(config_type, config_instance, holder_attr);
                    if (panle.Children.Count != 0)
                    {
                        var sub_tree_item = new TreeViewItem() { Header = config_type.Name };

                        sub_tree_item.Selected += (s, e) =>
                        {
                            e.Handled = true;
                            //Get config panle
                            var content = GetConfigPanel(config_type, config_instance,holder_attr);
                            configRegion.Content = content;
                        };

                        tree_item.Items.Add(sub_tree_item);
                    }
                }

                if(tree_item.Items.Count != 0)
                    tree_view_list.Add(tree_item);
            }

            tree_view_list.Sort((a, b) => ((string)a.Header).CompareTo((string)b.Header));

            foreach (var view in tree_view_list)
                configsTreeView.Items.Add(view);
        }

        private Dictionary<object, ConfigurationPanel> m_configuration_region_dict = new Dictionary<object, ConfigurationPanel>();
        private Dictionary<Plugin, Panel> m_plugin_panel_dict = new Dictionary<Plugin, Panel>();

        private Panel GetConfigPanel(Type config_type, object config_instance, ConfigurationHolderAttribute class_holder)
        {
            if (m_configuration_region_dict.TryGetValue(config_instance, out var region))
                return region.Panel;

            region = new ConfigurationPanel(config_type, config_instance,class_holder);
            if (region.Panel.Children.Count != 0)
                m_configuration_region_dict.Add(config_instance, region);
            return region.Panel;
        }

        private Panel GetPluginInformationPanel(Plugin plugin)
        {
            if (m_plugin_panel_dict.TryGetValue(plugin, out var panel))
                return panel;

            panel = new StackPanel();
            Label plugin_name_label = new Label()
            {
                Content = $"Plugin: {plugin.Name}",
                Margin = new Thickness(1)
            };
            Label plugin_author_label = new Label()
            {
                Content = $"Author: {plugin.Author}",
                Margin = new Thickness(1)
            };
            Label plugin_version_label = new Label()
            {
                Content = $"Version: {plugin.GetType().GetCustomAttribute<SyncPluginID>()?.Version??"0.0.0"}",
                Margin = new Thickness(1)
            };
            panel.Children.Add(plugin_name_label);
            panel.Children.Add(plugin_author_label);
            panel.Children.Add(plugin_version_label);

            m_plugin_panel_dict.Add(plugin, panel);
            return panel;
        }

        #endregion Plugins Config

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            SavePluginConfig();
            Hide();
        }

        private void SavePluginConfig()
        {
            foreach (var manager in m_pluginConfigurationManagers)
            {
                manager.SaveAll();
            }
        }

        private async void Save_Button_Click(object sender, RoutedEventArgs e)
        {
            SavedHint_Label.Content = DefaultLanguage.LABEL_SAVED_SAVING;
            SavePluginConfig();
            SavedHint_Label.Content = DefaultLanguage.LABEL_SAVED;
            try
            {
                await Task.Delay(2000);
                SavedHint_Label.Content = "";
            }
            catch (Exception)
            { }
        }
    }
}