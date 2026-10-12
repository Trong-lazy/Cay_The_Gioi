using System.Globalization;
using CayTheGioi.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace CayTheGioi.Presentation
{
    public sealed class FoundationHudView : MonoBehaviour
    {
        [SerializeField] private Text resourceText;
        [SerializeField] private Text stageText;
        private GameState state;
        private int taps;
        private ResourceSystem resources;
        private VitalitySystem vitality;
        private WeatherSystem weather;
        private HealthStatusSystem health;
        private AchievementSystem achievements;
        private readonly RectTransform[] cards = new RectTransform[4];
        private readonly Text[] balances = new Text[4];
        private readonly Text[] rates = new Text[4];
        private RectTransform statusCard, coinCard;
        private Text statusLabel, coinLabel;
        private float lastCardWidth;

        public string ResourceContent => resourceText != null ? resourceText.text : "";
        public Text StageText => stageText;

        public void Bind(GameState state, string stageName, int tapsPerReward)
        {
            this.state = state;
            taps = tapsPerReward;
            EnsureCards();
            if (stageText != null) stageText.text = stageName;
            Refresh();
        }

        public void BindSystems(ResourceSystem resourceSystem, VitalitySystem vitalitySystem, WeatherSystem weatherSystem = null, HealthStatusSystem healthSystem = null, AchievementSystem achievementSystem = null)
        {
            Unsubscribe();
            resources = resourceSystem;
            vitality = vitalitySystem;
            weather = weatherSystem;
            health = healthSystem;
            achievements = achievementSystem;

            if (resources != null) resources.Changed += Refresh;
            if (vitality != null) vitality.Changed += Refresh;
            if (weather != null)
            {
                weather.WeatherChanged += OnWeatherChanged;
                weather.StatusChanged += Refresh;
            }
            if (health != null)
            {
                health.HealthChanged += Refresh;
                health.StatusChanged += Refresh;
            }
            if (achievements != null)
            {
                achievements.StateChanged += Refresh;
            }
            Refresh();
        }

        private void OnWeatherChanged(string newWeather) => Refresh();

        public void Refresh()
        {
            if (state == null || resourceText == null) return;
            var rate = resources != null ? resources.Rates() : default;
            string full = state.vitality.bankSeconds >= 3600 ? " — Sức sống đã đầy" : "";

            string statusStr = "";
            if (state.run.status != null && state.run.status.statusId == "burning")
            {
                string regionName = state.run.status.impactRegionId == "leaves" ? "Lá" :
                                    state.run.status.impactRegionId == "root" ? "Rễ" :
                                    state.run.status.impactRegionId == "trunk" ? "Thân" : "Cây";
                statusStr = string.Format(CultureInfo.InvariantCulture, " · Đang cháy ({0}) {1:0.#}s", regionName, state.run.status.remainingOnlineSeconds);
            }

            string weatherStr = "";
            if (state.run.weather != null)
            {
                string wName = WeatherSystem.GetWeatherDisplayName(state.run.weather.weatherId);
                weatherStr = string.Format(CultureInfo.InvariantCulture, "\nThời tiết: {0} ({1:0.#}s)", wName, state.run.weather.remainingOnlineSeconds);
            }

            long coin = state.permanent != null ? state.permanent.coin : 0;
            resourceText.text = string.Format(CultureInfo.InvariantCulture,
                "Water {0:0.##}  ({6:+0.##;-0.##;0}/s)\nGrowth {1:0.##}  (+{7:0.##}/s)\nHP {2:0.##} · {8}{11}\nSức sống {3:0.##}s / 3600s  {9}\nTap {4}/{5}{10}{12}\nCoin: {13}",
                state.run.water, state.run.growth, state.run.hp, state.vitality.bankSeconds, state.vitality.tapProgress, taps,
                rate.WaterProduced - rate.WaterConsumed, rate.GrowthProduced, state.run.life,
                state.run.life == "ALIVE" && state.vitality.bankSeconds > 0 ? "+5%" : "", full,
                statusStr, weatherStr, coin);
            resourceText.text+="\nQuang hợp "+UpgradePanelView.Format(state.run.photosynthesis)+" ("+UpgradePanelView.Format(rate.PhotosynthesisProduced-rate.PhotosynthesisConsumed)+"/s)";
            if(state.run.spiritAbsorptionUnlocked) resourceText.text+="\nLinh khí "+UpgradePanelView.Format(state.run.spirit)+" (+"+UpgradePanelView.Format(rate.SpiritProduced)+"/s)";
            balances[0].text = UpgradePanelView.Format(state.run.water);
            balances[1].text = UpgradePanelView.Format(state.run.photosynthesis);
            balances[2].text = UpgradePanelView.Format(state.run.growth);
            balances[3].horizontalOverflow = HorizontalWrapMode.Overflow;
            balances[3].fontSize = state.run.spiritAbsorptionUnlocked ? 26 : 18;
            balances[3].text = state.run.spiritAbsorptionUnlocked ? UpgradePanelView.Format(state.run.spirit) : "Chưa mở";
            double[] net = { rate.WaterProduced-rate.WaterConsumed, rate.PhotosynthesisProduced-rate.PhotosynthesisConsumed, rate.GrowthProduced, rate.SpiritProduced };
            for (int i=0; i<4; i++) rates[i].text = i==3 && !state.run.spiritAbsorptionUnlocked ? "Cây Trưởng Thành" : net[i].ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture)+" / giây";
            coinLabel.text = "Coin  " + coin;
            string weatherName = state.run.weather == null ? "Bình yên" : WeatherSystem.GetWeatherDisplayName(state.run.weather.weatherId);
            statusLabel.text = "HP " + UpgradePanelView.Format(state.run.hp) + " · " + (state.run.life=="ALIVE" ? "Sống" : "Chết") + statusStr +
                "\nSức sống " + UpgradePanelView.Format(state.vitality.bankSeconds) + "s / 3600s" + (state.vitality.bankSeconds>0 ? " · +5%" : "") +
                "\nChạm cây " + state.vitality.tapProgress + "/" + taps + " · " + weatherName;

            if (stageText != null && state.run != null)
            {
                string sId = state.run.stageId;
                stageText.text = sId == "seedling" ? "Mầm" :
                                 sId == "young_tree" ? "Cây Non" :
                                 sId == "large_tree" ? "Cây Lớn" :
                                 sId == "mature_tree" ? "Cây Trưởng Thành" : sId;
            }
        }

        private void EnsureCards()
        {
            if (statusCard != null || resourceText == null) return;
            Transform parent = resourceText.transform.parent;
            resourceText.gameObject.SetActive(false); // Keep text for accessibility/QA consumers.
            var oldBackdrop = parent.Find("ResourceBackdrop");
            if (oldBackdrop != null) oldBackdrop.gameObject.SetActive(false);
            var saveHint = parent.Find("SaveHint");
            if (saveHint != null) saveHint.gameObject.SetActive(false);
            string[] names = { "Water", "Photosynthesis", "Growth", "Spirit" };
            string[] labels = { "NƯỚC", "QUANG HỢP", "GROWTH", "LINH KHÍ" };
            string[] icons = { "icon_resource_water", "icon_resource_leaf", "icon_resource_wood_cube", "icon_category_vfx" };
            for (int i=0;i<4;i++)
            {
                cards[i]=GameUiTheme.Card(names[i]+"Card", parent);
                GameUiTheme.Icon(icons[i],cards[i],new Vector2(14,-20),new Vector2(40,56));
                GameUiTheme.Label("ResourceName",cards[i],new Vector2(64,-10),new Vector2(150,24),17).fontStyle=FontStyle.Bold;
                cards[i].Find("ResourceName").GetComponent<Text>().text=labels[i];
                balances[i]=GameUiTheme.Label("Balance",cards[i],new Vector2(64,-34),new Vector2(150,32),26);
                balances[i].fontStyle=FontStyle.Bold;
                rates[i]=GameUiTheme.Label("Rate",cards[i],new Vector2(64,-68),new Vector2(150,24),14);
                rates[i].color=GameUiTheme.Muted;
                rates[i].horizontalOverflow=HorizontalWrapMode.Overflow;
            }
            statusCard=GameUiTheme.Card("TreeStatusCard",parent);
            statusLabel=GameUiTheme.Label("Status",statusCard,new Vector2(18,-12),new Vector2(454,108),20);
            coinCard=GameUiTheme.Card("CoinCard",parent);
            GameUiTheme.Icon("icon_coin_leaf",coinCard,new Vector2(12,-10),new Vector2(38,38));
            coinLabel=GameUiTheme.Label("Coin",coinCard,new Vector2(58,-14),new Vector2(132,32),22);
            LayoutCards();
        }

        public void LayoutCards()
        {
            if (GetComponent<ReferenceUiLayout>() != null) return;
            if (statusCard==null) return;
            Canvas.ForceUpdateCanvases();
            var safe=(RectTransform)statusCard.parent;
            lastCardWidth=safe.rect.width;
            bool portrait=Screen.width<Screen.height;
            int columns=portrait?2:4;
            for(int i=0;i<4;i++) GameUiTheme.Place(cards[i],new Vector2(0,1),new Vector2(24+(i%columns)*232,-104-(i/columns)*110),new Vector2(220,98));
            GameUiTheme.Place(statusCard,new Vector2(0,1),new Vector2(24,portrait?-328:-218),new Vector2(490,104));
            GameUiTheme.Place(coinCard,new Vector2(1,1),new Vector2(portrait?-24:-520,portrait?-94:-24),new Vector2(200,60));
        }

        private void LateUpdate()
        {
            if(statusCard!=null && Mathf.Abs(((RectTransform)statusCard.parent).rect.width-lastCardWidth)>.01f) LayoutCards();
        }

        private void Unsubscribe()
        {
            if (resources != null) resources.Changed -= Refresh;
            if (vitality != null) vitality.Changed -= Refresh;
            if (weather != null)
            {
                weather.WeatherChanged -= OnWeatherChanged;
                weather.StatusChanged -= Refresh;
            }
            if (health != null)
            {
                health.HealthChanged -= Refresh;
                health.StatusChanged -= Refresh;
            }
            if (achievements != null)
            {
                achievements.StateChanged -= Refresh;
            }
        }

        private void OnDestroy() { Unsubscribe(); }

        public void ShowUnavailable()
        {
            if (stageText != null) stageText.text = "Không thể khởi tạo phiên";
        }

#if UNITY_EDITOR
        public void Configure(Text resources, Text stage) { resourceText = resources; stageText = stage; }
#endif
    }
}
