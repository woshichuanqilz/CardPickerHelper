using Hearthstone_Deck_Tracker.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Card = Hearthstone_Deck_Tracker.Hearthstone.Card;
using Core = Hearthstone_Deck_Tracker.API.Core;

namespace MyHsHelper.Controls
{
    /// <summary>
    /// Interaction logic for PlugInDisplayControl.xaml
    /// </summary>
    public partial class PlugInDisplayControl : StackPanel
    {

        public PlugInDisplayControl()
        {
            InitializeComponent();
            Logic();
        }

        public void Logic()
        {
            GameEvents.OnPlayerHandMouseOver.Add(PlayerHandMouseOver);
            GameEvents.OnMouseOverOff.Add(OnMouseOff);
        }

        public void OnMouseOff()
        {
            this.Visibility = System.Windows.Visibility.Collapsed;
        }

        public void PlayerHandMouseOver(Card card)
        {

            this.Visibility = System.Windows.Visibility.Visible;
            //this.LblTextArea1.Content = card.Name;
            //this.LblTextArea2.Content = card.Artist;
        }

    }
}