using HearthDb;
using Hearthstone_Deck_Tracker.Plugins;
using Hearthstone_Deck_Tracker.Utility.Logging;
using MyHsHelper.Controls;
using MyHsHelper.Properties;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Controls;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HearthMirror;
using Hearthstone_Deck_Tracker;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Hearthstone.Entities;

namespace MyHsHelper
{
    /// <summary>
    /// Wires up your plug-ins' logic once HDT loads it in to the session.
    /// </summary>
    /// <seealso cref="Hearthstone_Deck_Tracker.Plugins.IPlugin" />
    public class HDTBootstrap : IPlugin
    {
        /// <summary>
        /// The Plug-in's running instance
        /// </summary>
        public MyHsHelper pluginInstance;

        /// <summary>
        /// The author, so your name.
        /// </summary>
        /// <value>The author's name.</value>
        public string Author => "Author Name";
        // ToDo: put your name as the author


        public string ButtonText => LocalizeTools.GetLocalized("LabelSettings");

        // ToDo: Update the Plug-in Description in StringsResource.resx        
        public string Description => LocalizeTools.GetLocalized("TextDescription");

        /// <summary>
        /// Gets or sets the main <see cref="MenuItem">Menu Item</see>.
        /// </summary>
        /// <value>The main <see cref="MenuItem">Menu Item</see>.</value>
        public MenuItem MenuItem { get; set; } = null;

        public string Name => LocalizeTools.GetLocalized("TextName");

        /// <summary>
        /// The gets plug-in version.from the assembly
        /// </summary>
        /// <value>The plug-in assembly version.</value>
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// Adds the menu item.
        /// </summary>
        private void AddMenuItem()
        {
            this.MenuItem = new MenuItem()
            {
                Header = Name
            };

            this.MenuItem.Click += (sender, args) =>
            {
                OnButtonPress();
            };
        }

        public void OnButtonPress() => SettingsView.Flyout.IsOpen = true;

        public void OnLoad()
        {
            pluginInstance = new MyHsHelper();
            AddMenuItem();
        }

        /// <summary>
        /// Called when during the window clean-up.
        /// </summary>
        public void OnUnload()
        {
            Settings.Default.Save();

            pluginInstance?.CleanUp();
            pluginInstance = null;
        }

        /// <summary>
        /// Called when [update].
        /// </summary>
        public void OnUpdate()
        {
            // Card in Bob
            var bgs_in_bob = new List<Entity>();
            var minions_in_bob = new List<Entity>();
            if (Core.Game.Opponent.Hero?.CardId.Contains("TB_BaconShopBob") ?? false)
            {
                // do something
                var entities = Core.Game.Entities.Values.Where(x => (x.IsMinion || x.IsBattlegroundsSpell) && x.IsInPlay && x.IsControlledBy(Core.Game.Opponent.Id)).Select(x => x.Clone());
                // seperate entities to minion and BattlegroundsSpell
                minions_in_bob = entities.Where(x => x.IsMinion).ToList();
                bgs_in_bob = entities.Where(x => x.IsBattlegroundsSpell).ToList();
            }

            // Card on my board
            List<Entity> CardsOnMyBoard = new List<Entity>();
            foreach (var entity in Core.Game.Entities.Values.Where(x => (x.IsMinion || x.IsBattlegroundsSpell) && x.IsInPlay && x.IsControlledBy(Core.Game.Player.Id)).Select(x => x.Clone()))
            {
                CardsOnMyBoard.Add(entity.Clone());
            }

            // Card in hand
            List<Entity> CardsInHand = Core.Game.Entities.Values.Where(x => (x.IsMinion || x.IsBattlegroundsSpell) && x.IsInPlay && x.IsControlledBy(Core.Game.Player.Id)).Select(x => x.Clone()).ToList();

            // My Trinkets
            List<Entity> myTrinkets = Core.Game.Player.Trinkets.Where(trinket => trinket.CardId != null).ToList().ToList();

            Log.Info("My Info");
        }
    }
}