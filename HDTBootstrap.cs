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
using Hearthstone_Deck_Tracker;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Hearthstone.Entities;
using Card = Hearthstone_Deck_Tracker.Hearthstone.Card;
using HearthDb.Enums;
using System.Net;
using System.Windows;
using Newtonsoft.Json;

namespace MyHsHelper
{
    public class CardWikiData
    {
        public string DbfId { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Keywords { get; set; } // 原始 keywords 字段
        public List<string> KeywordsList { get; set; } // 新增 KeywordsList 属性
        public string StringTags { get; set; }
        public List<string> TagsList { get; set; } // 新增 TagsList 性
        public string WikiMechanics { get; set; } // wikiMechanics 属性
        public List<string> WikiMechanicsList { get; set; } // 新增 wikiMechanicsList 属性
        public string WikiTags { get; set; } // wikiTags 属性
        public List<string> WikiTagsList { get; set; } // 新增 wikiTagsList 属性
        public string WikiHiddenTags { get; set; } // 原始 wikiHiddenTags 属性
        public List<string> WikiHiddenTagsList { get; set; } // 新增 wikiHiddenTagsList 属性
        public List<string> Races { get; set; } // 新增 Races 属性
        public List<string> RacesList { get; set; } // 新增 Races 属性
        public string LocName { get; set; } // 新增 LocName 属性
    }


    /// <summary>
    /// Wires up your plug-ins' logic once HDT loads it in to the session.
    /// </summary>
    /// <seealso cref="Hearthstone_Deck_Tracker.Plugins.IPlugin" />
    public class HDTBootstrap : IPlugin
    {
        /// <summary>
        /// The Plug-in's running instance
        /// </summary>
        public MyHsHelper InsMyHsHelper;

        private List<Entity> cardsOnMyBoard = new List<Entity>();
        private List<Entity> cardsInHand = new List<Entity>();
        private List<Entity> cardsInMyControl = new List<Entity>();
        private Dictionary<int, List<(string myCardLocName, CardWikiData myCardWikiData, string locName, CardWikiData cardWikiData, List<string> MatchedTags)>> relatedCardsDict = new Dictionary<int, List<(string myCardLocName, CardWikiData myCardWikiData, string locName, CardWikiData cardWikiData, List<string> MatchedTags)>>();

        /// <summary>
        /// The author, so your name.
        /// </summary>
        /// <value>The author's name.</value>
        public string Author => "Li";
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

            this.MenuItem.Click += (sender, args) => { OnButtonPress(); };
        }

        public void OnButtonPress() => SettingsView.Flyout.IsOpen = true;



        public void OnLoad()
        {
            InsMyHsHelper = new MyHsHelper();
            AddMenuItem();
        }

        /// <summary>
        /// Called when during the window clean-up.
        /// </summary>
        public void OnUnload()
        {
            Settings.Default.Save();

            InsMyHsHelper?.CleanUp();
            InsMyHsHelper = null;
        }

        /// <summary>
        /// Called when [update].
        /// </summary>
        public void OnUpdate()
        {
            GetGameEntitiesInfo();
        }

        // 新增方法：获取游戏实体信息
        private void GetGameEntitiesInfo()
        {
            // Card in Bob
            var minionsInBob = new List<Entity>();
            var bgSpellInBob = new List<Entity>();
            if (!(Core.Game.Opponent.Hero?.CardId?.Contains("TB_BaconShopBob") ?? false)) return;

            var entities = Core.Game.Entities.Values
                .Where(x => (x.IsMinion || x.IsBattlegroundsSpell) && x.IsInPlay &&
                            x.IsControlledBy(Core.Game.Opponent.Id))
                .Select(x => x.Clone())
                .ToLookup(x => x.IsMinion);

            minionsInBob = entities[true].ToList();
            bgSpellInBob = entities[false].ToList();
            // concat minionsInBob and bgSpellInBob
            var cardsInBob = minionsInBob.Concat(bgSpellInBob).ToList();
            var cardDataInBob = GetLocNamesAndCardWikiData(cardsInBob);

            var tCardsOnMyBoard = Core.Game.Entities.Values
                .Where(x => (x.IsMinion || x.IsBattlegroundsSpell) && x.IsInPlay &&
                            x.IsControlledBy(Core.Game.Player.Id)).Select(x => x.Clone())
                .Select(entity => entity.Clone()).ToList();
            // update Card On My Board
            InsMyHsHelper.UpdateListIfSame(cardsOnMyBoard, tCardsOnMyBoard);

            // update Card in hand
            var tCardsInHand = Core.Game.Entities.Values
                .Where(x => (x.IsMinion || x.IsBattlegroundsSpell) && x.IsInHand &&
                            x.IsControlledBy(Core.Game.Player.Id)).Select(x => x.Clone()).ToList();
            InsMyHsHelper.UpdateListIfSame(cardsInHand, tCardsInHand);

            // My Trinkets
            var myTrinkets = Core.Game.Player.Trinkets.Where(trinket => trinket.CardId != null).ToList().ToList();
            var tCardsInMyControl = cardsOnMyBoard.Concat(cardsInHand).Where(card => card != null).ToList();
            if (InsMyHsHelper.UpdateListIfSame(cardsInMyControl, tCardsInMyControl)) return;

            var cardDataInMyControl = GetLocNamesAndCardWikiData(tCardsInMyControl);
            // append cardDataInMyControl and bob all tagsList to file. make a separator between cardDataInMyControl and bob tagsList
            File.AppendAllText("cardDataInMyControl.txt", string.Join("\n", cardDataInMyControl.Select(cd => cd.locName + " " + string.Join(",", cd.cardWikiData.TagsList))));
            File.AppendAllText("bobTagsList.txt", string.Join("\n", cardDataInBob.Select(cd => string.Join(",", cd.cardWikiData.TagsList))));

            foreach (var cardData in cardDataInMyControl)
            {
                if (cardData.cardWikiData.TagsList == null) continue;
                var relatedTags = cardData.cardWikiData.TagsList.Select(tag => tag.EndsWith("-related") ? tag.Substring(0, tag.Length - 8) : $"{tag}-related").ToList();
                foreach (var cd in cardDataInBob.Where(cd => cd.cardWikiData.TagsList.Any(tag => relatedTags.Contains(tag))))
                {
                    // if cd.cardWikiData.TagsList contains any one in relatedTags, add cd to relatedCardsDict
                    if (cd.cardWikiData.TagsList.Any(tag => relatedTags.Contains(tag)))
                    {
                        var matchedTags = cd.cardWikiData.TagsList.Where(tag => relatedTags.Contains(tag)).ToList();
                        if (!relatedCardsDict.ContainsKey(cardData.EntityId))
                        {
                            relatedCardsDict[cardData.EntityId] = new List<(string myCardLocName, CardWikiData myCardWikiData, string locName, CardWikiData cardWikiData, List<string> MatchedTags)>();
                        }
                        relatedCardsDict[cardData.EntityId].Add((cardData.locName, cardData.cardWikiData, cd.locName, cd.cardWikiData, matchedTags));
                    }
                }
            }

            // console log relatedCardsDict
            Console.WriteLine("----------------------");
            foreach (var cardData in relatedCardsDict)
            {
                Console.WriteLine(cardData.Key + " " + string.Join(",", cardData.Value.Select(cd => cd.myCardLocName + " " + cd.locName)));
                // tagList
                Console.WriteLine(string.Join(",", cardData.Value.Select(cd => string.Join(",", cd.myCardWikiData.TagsList))));
                Console.WriteLine(string.Join(",", cardData.Value.Select(cd => string.Join(",", cd.cardWikiData.TagsList))));
                Console.WriteLine("----------------------");
            }
        }


        public List<(string locName, int EntityId, CardWikiData cardWikiData)> GetLocNamesAndCardWikiData(List<Entity> cards)
        {
            var result = new List<(string locName, int EntityId, CardWikiData cardWikiData)>();

            foreach (var card in cards.Where(card => card.CardId != null))
            {
                if (!(Core.Game.Opponent.Hero?.CardId?.Contains("TB_BaconShopBob") ?? false)) continue;
                if (card.CardId == null) continue;
                Cards.All.TryGetValue(card.CardId.ToString(), out var dbCard);
                if (dbCard == null) continue;
                // remove suffix "_G" if exists at the end of cardId
                if (card.CardId.EndsWith("_G"))
                    card.CardId = card.CardId.Substring(0, card.CardId.Length - 2);
                if (card.GetTag(GameTag.BACON_TRIPLED_BASE_MINION_ID) != 0)
                {
                    //get the base minion by dbfId
                    var baseMinion = Cards.AllByDbfId[card.GetTag(GameTag.BACON_TRIPLED_BASE_MINION_ID)];
                    if (baseMinion != null)
                    {
                        card.CardId = baseMinion.Id;
                    }
                }
                var locName = dbCard.GetLocName(Locale.zhCN);
                var cardWikiData = InsMyHsHelper.FindCardInfo(card.CardId);

                if (cardWikiData != null)
                {
                    result.Add((locName, card.Id, cardWikiData));
                }
                else
                {
                    Console.WriteLine(locName + " tagsList is null " + card.CardId);
                    // append to file 
                    File.AppendAllText("null_tagsList.txt", locName + " " + card.CardId + "\n");
                }
            }
            return result;
        }
    }
}