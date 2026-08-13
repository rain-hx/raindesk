using raindesk.Constant;
using raindesk.Control.Windows;
using raindesk.Util;
using raindesk.ViewModel;
using raindesk.ViewModel.Temp;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace raindesk.Control.Other
{
    public sealed partial class SearchResControl : UserControl
    {
        public SearchResControl(ObservableCollection<IconInfo> iconList)
        {
            this.DataContext = iconList;
            this.InitializeComponent();
        }

        public void SearchListBoxIndexAdd()
        {
            RunTimeStatus.MOUSE_MOVE_COUNT = 0;

            if (SearchListBox.Items.Count > 0)
            {
                if (SearchListBox.SelectedIndex < SearchListBox.Items.Count - 1)
                {
                    SearchListBox.SelectedIndex += 1;
                }
            }
        }

        public void SearchListBoxIndexSub()
        {
            RunTimeStatus.MOUSE_MOVE_COUNT = 0;

            if (SearchListBox.Items.Count > 0)
            {
                if (SearchListBox.SelectedIndex > 0)
                {
                    SearchListBox.SelectedIndex -= 1;
                }
            }
        }

        public void StartupSelectionItem()
        {
            if (SearchListBox.SelectedItem != null)
            {
                IconInfo icon = SearchListBox.SelectedItem as IconInfo;
                if (icon != null)
                {
                    if (icon.AdminStartUp)
                    {
                        ProcessUtil.StartIconApp(icon, IconStartType.ADMIN_STARTUP);
                    }
                    else
                    {
                        ProcessUtil.StartIconApp(icon, IconStartType.DEFAULT_STARTUP);
                    }
                }
            }
        }

        private void SearchListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SearchListBox.ScrollIntoView(SearchListBox.SelectedItem);
        }

        private void IconAdminStart(object sender, RoutedEventArgs e)
        {
            if (SearchListBox.SelectedItem is IconInfo icon)
            {
                ProcessUtil.StartIconApp(icon, IconStartType.ADMIN_STARTUP);
            }
        }

        private void ShowInExplore(object sender, RoutedEventArgs e)
        {
            if (SearchListBox.SelectedItem is IconInfo icon)
            {
                ProcessUtil.StartIconApp(icon, IconStartType.SHOW_IN_EXPLORE);
            }
        }

        private void SystemContextMenu(object sender, RoutedEventArgs e)
        {
            if (SearchListBox.SelectedItem is not IconInfo icon || string.IsNullOrWhiteSpace(icon.Path))
            {
                return;
            }

            var folders = new[] { new System.IO.DirectoryInfo(icon.Path) };
            var scm = new ShellContextMenu();
            var point = new System.Drawing.Point(0, 0);
            scm.ShowContextMenu(folders, point);
        }
    }
}
