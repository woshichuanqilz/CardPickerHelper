﻿using Hearthstone_Deck_Tracker.API;
using MyHsHelper.Controls;
using MyHsHelper.Logic;
using MyHsHelper.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;
using Core = Hearthstone_Deck_Tracker.API.Core;
using Hearthstone_Deck_Tracker.Enums;
using HearthDb.Enums;
using HearthDb;
using System.IO;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using Hearthstone_Deck_Tracker.Hearthstone.Entities;

namespace MyHsHelper
{
    /// <summary>
    /// This is where we put the logic for our Plug-in
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class MyHsHelper : IDisposable
    {
        // ToDo: The window shouldn't be statically named
        private static string panelName = "MyHsHelper";

        public List<CardWikiData> cardDataList;
        public List<(string Item, string Source)> combinedList; // 添加成员变量
        private readonly HttpClient httpClient = new HttpClient();

        public List<string> AllWikiTagsList;
        public List<string> AllKeywordsList;
        public List<string> AllWikiMechanicsList;
        public List<string> AllRacesList;

        /// <summary>
        /// The class that allows us to let the user move the panel
        /// </summary>
        public static InputMoveManager inputMoveManager;

        /// <summary>
        /// The panel reference we will display our plug-in magic within
        /// </summary>
        public PlugInDisplayControl stackPanel;

        /// <summary>
        /// Initializes a new instance of the <see cref="MyHsHelper"/> class.
        /// </summary>
        public MyHsHelper()
        {
            // 打开控制台
            AllocConsole();
            Console.WriteLine("hi there");

            // We are adding the Panel here for simplicity.  It would be better to add it under InitLogic()
            InitViewPanel();
            InitLogic();

            GameEvents.OnGameEnd.Add(CleanUp);
        }

        private void OnOpponentHeroPower()
        {
            Console.WriteLine("OnOpponentHeroPower");
        }

        private void OnOpponentPlayToGraveyard(Hearthstone_Deck_Tracker.Hearthstone.Card card)
        {
            if (Core.Game.Opponent.Hero != null && Core.Game.Opponent.Hero.CardId != null && Core.Game.Opponent.Hero != null && !Core.Game.Opponent.Hero.CardId.Contains("TB_BaconShopBob")) return;
            if (card == null) throw new ArgumentNullException(nameof(card));
            Cards.All.TryGetValue(card.Id, out var dbCard);
            if (dbCard != null)
            {
                Console.WriteLine("OnOpponentPlayToGraveyard:" + dbCard.GetLocName(Locale.zhCN));
            }
        }

        private void OnOpponentGet()
        {
            Console.WriteLine("OnOpponentGet");
        }

        private void OnOpponentCreateInPlay(Hearthstone_Deck_Tracker.Hearthstone.Card card)
        {
        }


        public async Task<BitmapImage> GetCardImageAsync(string cardId)
        {
            while (true)
            {
                // 如果image文件夹不存在，则创建
                if (!Directory.Exists("image"))
                {
                    Directory.CreateDirectory("image");
                }

                var imagePath = System.IO.Path.Combine("image", $"{cardId}.jpg");

                // 检查文件是否存在
                if (File.Exists(imagePath))
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute);
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // 确保图像在加载后可用
                    bitmapImage.EndInit();
                    bitmapImage.Freeze(); // 使图像可以在不同线程中使用
                    return bitmapImage;
                }
                else
                {
                    // 下载图片并保存
                    var url = $"https://static.zerotoheroes.com/hearthstone/cardart/256x/{cardId}.jpg";
                    var imageBytes = await httpClient.GetByteArrayAsync(url);

                    // 使用 FileStream 异步写入文件
                    using (var fileStream = new FileStream(imagePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                    {
                        await fileStream.WriteAsync(imageBytes, 0, imageBytes.Length);
                    }
                }
            }
        }


        public async Task DownloadAndParseJsonAsync()
        {
            const string url =
                "https://hearthstone.wiki.gg/wiki/Special:CargoExport?tables=Card,%20CardTag,%20DerivedCard,%20CustomCard,%20CardTagBg&join%20on=Card.dbfId=CardTag.dbfId,%20CardTag.dbfId=DerivedCard.dbfId,%20DerivedCard.dbfId=CustomCard.dbfId,%20CustomCard.dbfId=CardTagBg.dbfId&fields=CONCAT(Card.dbfId)=dbfId,%20Card.id=id,%20CONCAT(Card.name)=name,%20DerivedCard.minionTypeStrings=Races,%20CardTag.keywords=keywords,%20CardTag.refs=refs,%20CardTag.stringTags=stringTags,%20CONCAT(CustomCard.mechanicTags__full)=wikiMechanics,%20CONCAT(CustomCard.refTags__full)=wikiTags,%20CONCAT(CustomCard.hiddenTags__full)=wikiHiddenTags&where=CardTagBg.isPoolSpell=1%20OR%20CardTagBg.isPoolTrinket=1%20OR%20CardTagBg.isPoolMinion=1&limit=2000&format=json";

            var handler = new HttpClientHandler()
            {
                Proxy = new WebProxy("http://127.0.0.1:10081"),
                UseProxy = true
            };

            using (var client = new HttpClient(handler))
            {
                // 下载 JSON 数据
                try
                {
                    var json = await client.GetStringAsync(url);
                    // 解析 JSON 数据并赋值给成员变量
                    cardDataList = JsonConvert.DeserializeObject<List<CardWikiData>>(json);

                    // 等待任务完成并获取结果

                    // 遍历 cardData 中的每个项
                    foreach (var card in cardDataList.Where(card => card.Races != null && card.Races.Count == 1 && string.IsNullOrEmpty(card.Races[0])))
                    {
                        card.Races[0] = "Neutral"; // 替换为空字符串的子项为 "Neutral"
                    }

                    // remove the card with Id start with "BGDUO" use for statement
                    cardDataList = cardDataList.Where(card => !card.Id.StartsWith("BGDUO")).ToList();

                    // 合并所有列表并标记来源 make it class member 
                    combinedList = new List<(string Item, string Source)>();

                    foreach (var card in cardDataList)
                    {
                        Cards.All.TryGetValue(card.Id, out var dbCard);
                        if (dbCard != null)
                        {
                            card.LocName = dbCard.GetLocName(Locale.zhCN);
                        }

                        // 处理 wikiTags 字段 should split by "&amp;&amp;"
                        card.WikiTagsList = card.WikiTags?.Split(new[] { "&amp;&amp;" }, StringSplitOptions.None).ToList();
                        // 处理 wikiMechanics 字段 should split by "&amp;&amp;"
                        card.WikiMechanicsList = card.WikiMechanics?.Split(new[] { "&amp;&amp;" }, StringSplitOptions.None).ToList();
                        // remove duplicated item in wikiMechanicsList with wikiTagsList ignore case and if WikiMechanicsList is not null
                        if (card.WikiTagsList != null)
                            card.WikiMechanicsList = card.WikiMechanicsList?.Where(item => !card.WikiTagsList.Contains(item, StringComparer.OrdinalIgnoreCase)).ToList();
                        // remove duplicated item in wikiMechanicsList with keywordsList ignore case
                        if (card.KeywordsList != null)
                            card.WikiMechanicsList = card.WikiMechanicsList?.Where(item => !card.KeywordsList.Contains(item, StringComparer.OrdinalIgnoreCase)).ToList();

                        // 处理 keywords 字段 and make it lower case
                        card.KeywordsList = card.Keywords?.ToLower().Split(' ').ToList(); // 将 keywords 转换为 List<string>
                        // 处理 Races 字段 should split by "&amp;&amp;"
                        card.RacesList = card.Races[0]?.Split(new[] { "&amp;&amp;" }, StringSplitOptions.None).ToList();
                        // TagsList 等于 WikiMechanicsList 和 WikiTagsList 全部Lower处理后的 的并集. 
                        if (card.WikiTagsList == null)
                        {
                            card.WikiTagsList = new List<string>();
                        }
                        if (card.RacesList == null)
                        {
                            card.RacesList = new List<string>();
                        }
                        if (card.WikiMechanicsList == null)
                        {
                            card.WikiMechanicsList = new List<string>();
                        }

                        Console.WriteLine(card.Id);
                        // Concat RacesList and WikiMechanicsList and WikiTagsList
                        card.TagsList = card.RacesList?.Concat(card.WikiMechanicsList)
                            .Concat(card.WikiTagsList)
                            .Select(item => item.ToLower()).Distinct().ToList();
                        // lower case all items in TagsList
                        card.TagsList = card.TagsList.Select(item => item.ToLower()).Distinct().ToList();

                        // 合并所有列表并标记来源, 重复内容不添加
                        if (card.RacesList != null)
                            combinedList.AddRange(card.RacesList.Select(item => (item, "Races")).Distinct());
                        if (card.WikiMechanicsList != null)
                            combinedList.AddRange(card.WikiMechanicsList.Select(item => (item, "WikiMechanics"))
                                .Distinct());
                        if (card.WikiTagsList != null)
                            combinedList.AddRange(card.WikiTagsList.Select(item => (item, "WikiTags")).Distinct());

                        if (card.KeywordsList != null)
                            combinedList.AddRange(card.KeywordsList.Select(item => (item, "Keywords")).Distinct());
                        combinedList = combinedList
                            .GroupBy(item => item.Item1.ToLower()) // 忽略大小写
                            .Select(group => group.First()) // 选择每组的第一个项
                            .ToList();
                    }



                    // 提取所有 WikiTags , Keywords, WikiMechanics, Races
                    AllWikiTagsList = combinedList.Where(item => item.Source == "WikiTags").Select(item => item.Item1).Distinct().ToList();
                    AllKeywordsList = combinedList.Where(item => item.Source == "Keywords").Select(item => item.Item1).Distinct().ToList();
                    AllWikiMechanicsList = combinedList.Where(item => item.Source == "WikiMechanics").Select(item => item.Item1).Distinct().ToList();
                    AllRacesList = combinedList.Where(item => item.Source == "Races").Select(item => item.Item1).Distinct().ToList();
                    Console.WriteLine("Download done");

                    //return await Task.FromResult(new List<CardWikiData>()); // 返回空列表
                }
                catch (HttpRequestException)
                {
                    // 处理请求异常
                    MessageBox.Show("Download data error. Please check the proxy code in this downloadAndParseJsonAsync function.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            var tjson = JsonConvert.SerializeObject(cardDataList, Formatting.Indented); // 将列表序列化为 JSON 字符串
            // write to json file
            System.IO.File.WriteAllText("cardDataList.json", tjson);
        }

        // find card info in cardDataListby cardId
        public CardWikiData FindCardInfo(string cardId)
        {
            return cardDataList.FirstOrDefault(card => card.Id == cardId);
        }

        // update list if not same source not be changed outside the function
        public bool UpdateListIfSame(List<Entity> source, List<Entity> newValue)
        {
            //if source and newValue count is same and all entities id is same
            if (source.Count == newValue.Count && source.All(entity => newValue.Any(e => e.CardId == entity.CardId)))
            {
                return true;
            }

            // 清空 source 并添加 newValue 的内容
            source.Clear();
            source.AddRange(newValue);

            return false;
        }

        /// <summary>
        /// Check the game type to see if our Plug-in is used.
        /// </summary>
        private void GameTypeCheck()
        {
            // ToDo : Enable toggle Props
            if (Core.Game.CurrentGameType == HearthDb.Enums.GameType.GT_RANKED ||
                Core.Game.CurrentGameType == HearthDb.Enums.GameType.GT_CASUAL ||
                Core.Game.CurrentGameType == HearthDb.Enums.GameType.GT_FSG_BRAWL ||
                Core.Game.CurrentGameType == HearthDb.Enums.GameType.GT_ARENA)
            {
                InitLogic();
            }
        }

        private void InitLogic()
        {
            var task = DownloadAndParseJsonAsync();
        }

        private void InitViewPanel()
        {
            stackPanel = new PlugInDisplayControl();
            stackPanel.Name = panelName;
            stackPanel.Visibility = System.Windows.Visibility.Collapsed;
            Core.OverlayCanvas.Children.Add(stackPanel);

            Canvas.SetTop(stackPanel, Settings.Default.Top);
            Canvas.SetLeft(stackPanel, Settings.Default.Left);

            inputMoveManager = new InputMoveManager(stackPanel);

            Settings.Default.PropertyChanged += SettingsChanged;
            SettingsChanged(null, null);
        }

        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            stackPanel.RenderTransform = new ScaleTransform(Settings.Default.Scale / 100, Settings.Default.Scale / 100);
            stackPanel.Opacity = Settings.Default.Opacity / 100;
        }

        public void CleanUp()
        {
            if (stackPanel != null)
            {
                Core.OverlayCanvas.Children.Remove(stackPanel);
                Dispose();
            }
        }

        public void Dispose()
        {
            inputMoveManager.Dispose();
        }

        // P/Invoke 声明
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
    }
}