using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Core = Hearthstone_Deck_Tracker.API.Core;

namespace MyHsHelper.Controls
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : ScrollViewer
    {
        private static Flyout _flyout;

        // ToDo: The window shouldn't be statically named
        private static string panelName = "pluginStackPanelView";
        private StackPanel stackPanel;

        public SettingsView()
        {
            InitializeComponent();
            this.getPanel();
            initTranslation();
        }

        public static Flyout Flyout
        {
            get
            {
                if (_flyout == null)
                {
                    _flyout = CreateSettingsFlyout();
                }
                return _flyout;
            }
        }

        public IEnumerable<Orientation> OrientationTypes => Enum.GetValues(typeof(Orientation)).Cast<Orientation>();

        private static Flyout CreateSettingsFlyout()
        {
            var settings = new Flyout();
            settings.Position = Position.Left;
            Panel.SetZIndex(settings, 100);
            settings.Header = LocalizeTools.GetLocalized("LabelSettings");
            settings.Content = new SettingsView();
            Core.MainWindow.Flyouts.Items.Add(settings);
            return settings;
        }

        private void BtnShowHide_Click(object sender, RoutedEventArgs e)
        {
            if (stackPanel != null)
            {
                bool IsVis = (stackPanel.Visibility == Visibility.Visible);
                stackPanel.Visibility = IsVis ? Visibility.Collapsed : Visibility.Visible;
                BtnShowHide.Content = IsVis ? LocalizeTools.GetLocalized("LabelShow") : LocalizeTools.GetLocalized("LabelHide");
            }
        }

        private void BtnUnlock_Click(object sender, RoutedEventArgs e)
        {
            if (stackPanel != null)
            {
                bool IsUnlocked = MyHsHelper.inputMoveManager.Toggle();
                BtnShowHide.IsEnabled = !IsUnlocked;
                BtnUnlock.Content = IsUnlocked ? LocalizeTools.GetLocalized("LabelLock") : BtnUnlock.Content = LocalizeTools.GetLocalized("LabelUnlock");

                if (IsUnlocked && (stackPanel.Visibility != Visibility.Visible))
                {
                    stackPanel.Visibility = Visibility.Visible;
                    BtnShowHide.Content = LocalizeTools.GetLocalized("LabelHide");
                }
            }
        }
        /// <summary>
        /// Gets the reference to our display StackPanel.
        /// </summary>
        private void getPanel()
        {
            this.stackPanel = Core.OverlayCanvas.FindChild<StackPanel>(panelName);
        }
        /// <summary>
        /// Does our default translation, just till I fix the XAML Hooks.
        /// </summary>
        public void initTranslation()
        {
            BtnUnlock.Content = LocalizeTools.GetLocalized("LabelUnlock");
            BtnShowHide.Content = LocalizeTools.GetLocalized("LabelShow");
            LblOpacity.Content = LocalizeTools.GetLocalized("LabelOpacity");
            LblScale.Content = LocalizeTools.GetLocalized("LabelScale");
        }
    }
}