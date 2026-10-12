using System;
using CayTheGioi.Bootstrap;
using CayTheGioi.Domain;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CayTheGioi.Presentation
{
    // Composition only. Never writes resources, progression, saved tutorial IDs or camera poses.
    [DefaultExecutionOrder(2000)]
    public sealed class ReferenceUiLayout : MonoBehaviour
    {
        private RectTransform safe, weatherCard;
        private GameBootstrapper game;
        private UpgradePanelView upgrades;
        private ShopView shop;
        private AchievementView achievements;
        private SettingsView settings;
        private RebirthView rebirth;
        private Text weatherText, pointsText, achievementProgress;
        private RectTransform achievementFill;
        private RectTransform shopContent, achievementContent;
        private RectTransform spotlight;
        private readonly RectTransform[] dim = new RectTransform[4];
        private readonly RectTransform[] border = new RectTransform[4];
        private string shopFilter = "all", achievementFilter = "all";
        private float nextLayout;
        private string lastComposition;
        private readonly Vector2 topLeft = new Vector2(0, 1);
        private readonly Vector2 topRight = new Vector2(1, 1);

        public void Initialize()
        {
            if (safe != null) return;
            safe = transform.Find("SafeArea") as RectTransform;
            game = FindAnyObjectByType<GameBootstrapper>();
            if (safe == null || game == null) { enabled = false; return; }
            upgrades = GetComponent<UpgradePanelView>(); shop = GetComponent<ShopView>();
            achievements = GetComponent<AchievementView>(); settings = GetComponent<SettingsView>();
            rebirth = GetComponent<RebirthView>();
            weatherCard = GameUiTheme.Card("WeatherCard", safe);
            weatherCard.GetComponent<Image>().color = new Color(.08f,.16f,.22f,.95f);
            weatherText = GameUiTheme.Label("Weather", weatherCard, new Vector2(70,-16), new Vector2(220,80),24);
            GameUiTheme.Icon("icon_sun_hud", weatherCard, new Vector2(14,-24), new Vector2(48,48));
            pointsText = GameUiTheme.Label("Points", rebirth.OpenButton.transform, new Vector2(15,-9),new Vector2(250,66),22);
            pointsText.alignment = TextAnchor.MiddleCenter;
            AddOutsideClose("SettingsBlocker", settings.Close);
            AddOutsideClose("ShopBlocker", shop.Close);
            AddOutsideClose("AchievementBlocker", achievements.Close);
            // Upgrade previously had no outside-click layer. It stays a sibling below its panel.
            var shade = new GameObject("UpgradeShade", typeof(RectTransform),typeof(Image),typeof(Button));
            shade.transform.SetParent(safe,false);
            Stretch((RectTransform)shade.transform);
            shade.GetComponent<Image>().color = new Color(0,0,0,.45f);
            shade.GetComponent<Button>().onClick.AddListener(upgrades.Close);
            shade.transform.SetSiblingIndex(upgrades.Panel.GetSiblingIndex());
            shade.SetActive(false);
            Header(upgrades.Panel,"NÂNG CẤP CÂY");
            var settingsPanel = Find("SettingsBlocker/SettingsPanel");
            Header(settingsPanel,"CÀI ĐẶT");
            settingsPanel.Find("SettingsTitle").gameObject.SetActive(false);
            Back(settingsPanel,settings.Close);
            var shopPanel = Find("ShopBlocker/ShopPanel");
            Header(shopPanel,"SHOP");
            shopContent = MakeScroll(shopPanel,"ShopListContainer",180,105);
            Tabs(shopPanel,new[]{"Tất cả","VFX","Trang trí"},i=>shopFilter=i==0?"all":i==1?"vfx":"decor");
            var achPanel = Find("AchievementBlocker/AchievementPanel");
            Header(achPanel,"THÀNH TỰU");
            achievementContent = MakeScroll(achPanel,"ListContainer",235,85);
            Tabs(achPanel,new[]{"Tất cả","Phát triển","Thiên nhiên","Khám phá"},i=>achievementFilter=new[]{"all","growth","nature","explore"}[i],175);
            achievementProgress=GameUiTheme.Label("Completion",achPanel,new Vector2(30,-100),new Vector2(600,35),21);
            var track = Solid("ProgressTrack",achPanel,new Color(.23f,.21f,.16f));
            Place(track,topLeft,30,-145,600,12);
            achievementFill=Solid("Fill",track,new Color(.36f,.75f,.12f));
            Header(Find("RebirthBlocker/RebirthPanel"),"TRÙNG SINH");
            foreach(var row in upgrades.Panel.GetComponentsInChildren<Transform>(true))
            {
                if(row.name=="RootRow" || row.name=="LeavesRow" || row.name=="TrunkRow" || row.name=="SpiritAbsorptionRow")
                    GameUiTheme.Icon(row.name=="RootRow"?"icon_resource_water":row.name=="LeavesRow"?"icon_resource_leaf":row.name=="TrunkRow"?"icon_resource_wood_cube":"icon_category_vfx",row,new Vector2(12,-25),new Vector2(48,56));
            }
            Layout();
        }

        private void LateUpdate()
        {
            if(safe==null)return;
            string composition=safe.rect.size+"|"+settings.IsVisible+shop.IsVisible+achievements.IsVisible+upgrades.IsOpen+rebirth.IsVisible+rebirth.IsConfirming+shopFilter+achievementFilter+game.Tutorial.GetCurrentStepId();
            if(composition==lastComposition && Time.unscaledTime<nextLayout)return;
            lastComposition=composition;nextLayout=Time.unscaledTime+.25f;Layout();
        }

        public void Layout()
        {
            float w=safe.rect.width, h=safe.rect.height;
            if(w<1 || h<1)return;
            float side=Mathf.Min(730,w*.40f), ph=Mathf.Min(940,h-160);
            bool left = settings.IsVisible || achievements.IsVisible;
            bool right = shop.IsVisible || upgrades.IsOpen;
            float center = left ? side+(w-side)*.5f : right ? (w-side)*.5f : w*.5f;
            var camera=FindAnyObjectByType<OrbitCameraRig>().SceneCamera;
            camera.ResetProjectionMatrix();
            var projection=camera.projectionMatrix;projection.m02+=1-2*center/w;camera.projectionMatrix=projection;
            // Main navigation follows the images; the four real gameplay resources remain visible.
            Place(Find("SettingsOpen"),topLeft,24,-22,130,112);
            Place(Find("AchievementOpen"),topLeft,24,-156,130,112);
            Place(Find("ShopOpen"),new Vector2(0,0),center-395,45,360,104);
            Place((RectTransform)upgrades.OpenButton.transform,new Vector2(0,0),center+35,45,360,104);
            Place(Find("CoinCard"),new Vector2(.5f,1),-165,-22,310,82);
            Find("CoinCard").GetComponent<Image>().color=new Color(.08f,.16f,.22f,.95f);
            Find("CoinCard/Coin").GetComponent<Text>().color=Color.white;
            Place((RectTransform)rebirth.OpenButton.transform,new Vector2(.5f,1),165,-22,310,82);
            foreach(var t in rebirth.OpenButton.GetComponentsInChildren<Text>()) if(t!=pointsText)t.enabled=false;
            long total=0; foreach(var p in game.State.permanent.stagePoints) total=total>long.MaxValue-p.points?long.MaxValue:total+p.points;
            pointsText.text="Điểm trùng sinh\n"+total;
            pointsText.color=Color.white;
            rebirth.OpenButton.GetComponent<Image>().sprite=null;
            rebirth.OpenButton.GetComponent<Image>().color=new Color(.08f,.16f,.22f,.95f);
            foreach(var name in new[]{"SettingsOpen","AchievementOpen"})
            {var button=Find(name);button.GetComponent<Image>().sprite=null;button.GetComponent<Image>().color=new Color(.08f,.16f,.22f,.95f);button.GetComponentInChildren<Text>().color=Color.white;}
            shop.OpenButton.GetComponentInChildren<Text>().text="SHOP";
            upgrades.OpenButton.GetComponentInChildren<Text>().text="NÂNG CẤP";
            Place(weatherCard,topRight,-24,-22,300,100);
            weatherText.color=Color.white;
            var weather=game.State.run.weather;
            var weatherIcon = weatherCard.Find("Icon");
            if (weatherIcon != null)
            {
                weatherIcon.gameObject.SetActive(true);
                var img = weatherIcon.GetComponent<Image>();
                string wId = weather == null ? "calm" : weather.weatherId;
                if (wId == "thunderstorm")
                {
                    img.sprite = GameUiTheme.Sprite("icon_category_vfx");
                    img.color = new Color(1f, 0.85f, 0.45f, 1f);
                }
                else if (wId == "rainy")
                {
                    img.sprite = GameUiTheme.Sprite("icon_resource_water");
                    img.color = new Color(0.65f, 0.88f, 1f, 1f);
                }
                else if (wId == "sunny")
                {
                    img.sprite = GameUiTheme.Sprite("icon_sun_hud");
                    img.color = new Color(1f, 0.95f, 0.70f, 1f);
                }
                else
                {
                    img.sprite = GameUiTheme.Sprite("icon_sun_hud");
                    img.color = new Color(0.90f, 0.92f, 0.85f, 0.95f);
                }
            }
            double seconds=weather==null?0:Math.Max(0,weather.remainingOnlineSeconds);
            weatherText.text=(weather==null?"Bình yên":WeatherSystem.GetWeatherDisplayName(weather.weatherId))+"\n"+((int)seconds/60).ToString("00")+":"+((int)seconds%60).ToString("00");
            string[] names={"WaterCard","PhotosynthesisCard","GrowthCard","SpiritCard"};
            for(int i=0;i<4;i++)
            {
                var card=Find(names[i]); Place(card,topLeft,center-370+i*186,-120,176,96);
                Place(card.Find("Icon") as RectTransform,topLeft,10,-25,32,45);
                Place(card.Find("ResourceName") as RectTransform,topLeft,48,-12,120,22);
                Place(card.Find("Balance") as RectTransform,topLeft,48,-34,120,32);
                var rateRt = card.Find("Rate") as RectTransform;
                Place(rateRt,topLeft,48,-68,126,22);
                card.Find("ResourceName").GetComponent<Text>().fontSize=15;
                var rateText = rateRt.GetComponent<Text>();
                rateText.fontSize=14;
                rateText.horizontalOverflow=HorizontalWrapMode.Overflow;
            }
            var status=Find("TreeStatusCard"); Place(status,topLeft,24,-280,245,108);
            Place(status.Find("Status") as RectTransform,topLeft,12,-8,222,92);
            status.Find("Status").GetComponent<Text>().fontSize=15;
            var title=Find("Title"); if(title!=null)title.gameObject.SetActive(false);
            var stage=Find("Stage");
            if(stage!=null)
            {
                Place(stage,new Vector2(.5f,0),0,160,360,30);
                var sText = stage.GetComponent<Text>();
                if (sText != null && game.State != null && game.State.run != null)
                {
                    string sId = game.State.run.stageId;
                    sText.text = sId == "seedling" ? "Mầm" :
                                 sId == "young_tree" ? "Cây Non" :
                                 sId == "large_tree" ? "Cây Lớn" :
                                 sId == "mature_tree" ? "Cây Trưởng Thành" : sId;
                }
            }
            var controls=Find("Controls"); if(controls!=null)Place(controls,new Vector2(.5f,0),0,8,Mathf.Min(1100,w-40),30);

            // Orbit lock button relocated to bottom-right HUD
            var evComponent = GetComponent<EvolutionPanelView>();
            if (evComponent != null && evComponent.OrbitButton != null)
            {
                var orbitRt = (RectTransform)evComponent.OrbitButton.transform;
                if (orbitRt.parent != safe) orbitRt.SetParent(safe, false);
                Place(orbitRt, new Vector2(1, 0), -24, 24, 175, 48);
                var img = orbitRt.GetComponent<Image>();
                if (img != null) { img.sprite = null; img.color = new Color(.08f, .16f, .22f, .95f); }
                var txt = orbitRt.GetComponentInChildren<Text>();
                if (txt != null) { txt.color = Color.white; txt.fontSize = 16; }
            }

            LayoutUpgrade(side,ph);
            var sp=Find("SettingsBlocker/SettingsPanel"); Side(sp,false,side,ph);
            Place(sp.Find("SettingsTitle") as RectTransform,topLeft,130,-25,side-160,55);
            Place(sp.Find("MusicLabel") as RectTransform,topLeft,36,-155,side-72,44);
            Place(sp.Find("MusicToggleBtn") as RectTransform,topRight,-36,-218,side-72,62);
            Place(sp.Find("OrbitLabel") as RectTransform,topLeft,36,-340,side-72,44);
            Place(sp.Find("OrbitToggleBtn") as RectTransform,topRight,-36,-400,200,60);
            Place(sp.Find("ZoomNote") as RectTransform,topLeft,36,-510,side-72,80);
            Place(sp.Find("ContinueBtn") as RectTransform,new Vector2(.5f,0),0,150,side-90,82);
            Place(sp.Find("ExitBtn") as RectTransform,new Vector2(.5f,0),0,48,side-90,82);
            LayoutShop(side,ph);
            LayoutAchievements(side,ph);
            var rp=Find("RebirthBlocker/RebirthPanel");
            float rw=Mathf.Min(780,w-60), rh=rebirth.IsConfirming?Mathf.Min(720,h-80):Mathf.Min(920,h-80);
            Place(rp,new Vector2(.5f,.5f),0,0,rw,rh);
            rp.Find("ReferenceHeader/ReferenceTitle").GetComponent<Text>().text=rebirth.IsConfirming?"XÁC NHẬN TRÙNG SINH":"TRÙNG SINH";
            Place(rp.Find("RebirthText") as RectTransform,topLeft,35,-110,rw-70,rh-240);
            rp.Find("RebirthText").GetComponent<Text>().fontSize=24;
            Place(rp.Find("RebirthOverview") as RectTransform,topLeft,40,-110,rw-80,rh-245);
            Place(rp.Find("RebirthBegin") as RectTransform,new Vector2(.5f,0),0,35,rw-100,82);
            Place((RectTransform)rebirth.ConfirmButton.transform,new Vector2(1,0),-35,35,rw*.53f,82);
            Place((RectTransform)rebirth.CancelButton.transform,new Vector2(0,0),35,35,rebirth.IsConfirming?rw*.30f:100,82);
            if(!rebirth.IsConfirming)Place((RectTransform)rebirth.CancelButton.transform,topLeft,18,-12,80,60);
            rebirth.CancelButton.GetComponentInChildren<Text>().text=rebirth.IsConfirming?"Hủy":"←";
            rebirth.ConfirmButton.GetComponentInChildren<Text>().text="TRÙNG SINH";
            LayoutTutorial(center,h);
        }

        private void LayoutUpgrade(float width,float height)
        {
            var panel=upgrades.Panel; Side(panel,true,width,height);
            Find("UpgradeShade").gameObject.SetActive(upgrades.IsOpen);
            if(upgrades.IsOpen) { Find("UpgradeShade").SetSiblingIndex(panel.GetSiblingIndex()-1); }
            var close=panel.GetComponentInChildren<Button>(true);
            foreach(var b in panel.GetComponentsInChildren<Button>(true))
                if(b.name.Contains("Close")) { Place((RectTransform)b.transform,topLeft,18,-18,92,62); b.GetComponentInChildren<Text>().text="←"; }
            foreach(var t in panel.GetComponentsInChildren<Text>(true)) if(t.name=="Heading")t.gameObject.SetActive(false);
            int i=0;
            foreach(var id in new[]{"root","leaves","trunk","spirit_absorption"})
            {
                var row=upgrades.FindRow(id); if(row==null)continue;
                var r=(RectTransform)row.details.transform.parent;
                Place(r,topLeft,24,-100-i++*130,width-48,124);
                var image=r.GetComponent<Image>() ?? r.gameObject.AddComponent<Image>();GameUiTheme.Paper(image,"row_upgrade_bg");image.raycastTarget=true;
                if(r.Find("ReferenceIcon")==null){GameUiTheme.Icon(id=="root"?"icon_resource_water":id=="leaves"?"icon_resource_leaf":id=="trunk"?"icon_resource_wood_cube":"icon_category_vfx",r,new Vector2(12,-25),new Vector2(48,56));r.GetChild(r.childCount-1).name="ReferenceIcon";}
                Place((RectTransform)row.details.transform,topLeft,70,-10,width-245,74);
                Place((RectTransform)row.reason.transform,topLeft,70,-88,width-245,24);
                Place((RectTransform)row.button.transform,topRight,-14,-31,130,62);
                row.details.fontSize=18; row.reason.fontSize=15;
                // UpgradePanelView owns the live cost label; layout must not overwrite it.
            }
            var ev=GetComponent<EvolutionPanelView>();
            ev.Layout(false,width);
            var feedback=panel.Find("Feedback"); if(feedback!=null)Place((RectTransform)feedback,topLeft,26,-850,width-52,36);
        }

        private void LayoutShop(float width,float height)
        {
            var p=Find("ShopBlocker/ShopPanel"); Side(p,true,width,height);
            Place(p.Find("Title") as RectTransform,topLeft,135,-18,width-160,75);
            p.Find("Title").GetComponent<Text>().color=Color.white;
            Place(p.Find("CloseBtn") as RectTransform,topLeft,18,-18,92,62);
            p.Find("CloseBtn").GetComponentInChildren<Text>().text="←";
            Place(p.Find("Feedback") as RectTransform,topLeft,28,-height+85,width-56,65);
            var tabs=p.Find("ReferenceTabs"); LayoutTabs((RectTransform)tabs,width,115);
            int i=0;
            foreach(Transform row in shopContent)
            {
                if(!row.name.StartsWith("ShopRow_"))continue;
                // Prototype currently contains only an aura VFX; do not invent purchasable decorations.
                bool show=shopFilter!="decor"; row.gameObject.SetActive(show); if(!show)continue;
                float cw=(width-72)/2;
                Place((RectTransform)row,topLeft,(i%2)*(cw+18),-(i/2)*350,cw,330); i++;
                Place(row.Find("Info") as RectTransform,topLeft,16,-85,cw-32,150);
                row.Find("Info").GetComponent<Text>().fontSize=18;
                if(row.Find("ReferenceIcon")==null) {GameUiTheme.Icon("icon_category_vfx",row,new Vector2(16,-15),new Vector2(cw-32,60));row.GetChild(row.childCount-1).name="ReferenceIcon";}
                Place(row.Find("PreviewBtn") as RectTransform,new Vector2(0,0),10,15,(cw-30)/2,62);
                Place(row.Find("ActionBtn") as RectTransform,new Vector2(1,0),-10,15,(cw-30)/2,62);
            }
            shopContent.sizeDelta=new Vector2(0,Mathf.Max(330,Mathf.Ceil(i/2f)*350));
            var empty=p.Find("EmptyDecoration");
            if(empty==null) {var t=GameUiTheme.Label("EmptyDecoration",p,new Vector2(35,-230),new Vector2(width-70,100),22); t.text="Chưa có vật phẩm trang trí.\nNội dung sẽ được bổ sung sau.";empty=t.transform;}
            empty.gameObject.SetActive(shopFilter=="decor");
        }

        private void LayoutAchievements(float width,float height)
        {
            var p=Find("AchievementBlocker/AchievementPanel"); Side(p,false,width,height);
            Place(p.Find("Title") as RectTransform,topLeft,135,-18,width-160,75);
            p.Find("Title").GetComponent<Text>().color=Color.white;
            Place(p.Find("CloseBtn") as RectTransform,topLeft,18,-18,92,62);p.Find("CloseBtn").GetComponentInChildren<Text>().text="←";
            Place(p.Find("Feedback") as RectTransform,topLeft,28,-height+70,width-56,55);
            LayoutTabs(p.Find("ReferenceTabs") as RectTransform,width,175);
            int completed=0, count=0, shown=0;
            foreach(Transform row in achievementContent)
            {
                if(!row.name.StartsWith("Row_"))continue;
                var id=row.name.Substring(4);var status=game.Achievements.GetStatus(id);count++;if(status.IsCompleted)completed++;
                string category=id=="first_vitality"?"nature":id=="first_rebirth"?"explore":"growth";
                bool show=achievementFilter=="all"||achievementFilter==category;row.gameObject.SetActive(show);if(!show)continue;
                Place((RectTransform)row,topLeft,0,-shown++*112,width-56,104);
                Place(row.Find("Info") as RectTransform,topLeft,14,-8,width-232,88);row.Find("Info").GetComponent<Text>().fontSize=18;
                Place(row.Find("ClaimBtn") as RectTransform,topRight,-12,-22,156,60);
            }
            achievementProgress.text="Hoàn thành: "+completed+" / "+count+"     "+(count==0?0:completed*100/count)+"%";
            Place(achievementFill,topLeft,0,0,(width-60)*(count==0?0:(float)completed/count),12);
            Place(achievementFill.parent as RectTransform,topLeft,30,-145,width-60,12);
            achievementContent.sizeDelta=new Vector2(0,shown*112);
        }

        private void LayoutTutorial(float center,float height)
        {
            var tutorial=GetComponent<TutorialView>();var banner=Find("TutorialBanner");
            if(spotlight==null)
            {
                spotlight=new GameObject("TutorialSpotlight",typeof(RectTransform)).GetComponent<RectTransform>();spotlight.SetParent(safe,false);Stretch(spotlight);
                for(int i=0;i<4;i++){dim[i]=Solid("Dim"+i,spotlight,new Color(0,0,0,.46f));border[i]=Solid("Highlight"+i,spotlight,new Color(1,.85f,.16f,.95f));}
            }
            bool show=tutorial!=null&&tutorial.IsVisible;spotlight.gameObject.SetActive(show);if(!show)return;
            var definition=game.Tutorial.GetStepDefinition(tutorial.ActiveStepId);
            RectTransform target=null;
            if(definition!=null && definition.TargetElement.StartsWith("upgrade_"))
            {
                string id=definition.TargetElement.Substring(8);var row=upgrades.FindRow(id);
                target=upgrades.IsOpen&&row!=null?(RectTransform)row.details.transform.parent:(RectTransform)upgrades.OpenButton.transform;
            }
            else if(definition!=null && definition.TargetElement=="evolution")target=upgrades.IsOpen?(RectTransform)GetComponent<EvolutionPanelView>().EvolveButton.transform:(RectTransform)upgrades.OpenButton.transform;
            float x=center-110,y=height*.44f,bw=220,bh=220;
            if(definition!=null && definition.TargetElement=="tree")
            {
                var tree=FindAnyObjectByType<TreeView>();var cam=FindAnyObjectByType<OrbitCameraRig>().SceneCamera;
                float minX=float.PositiveInfinity,minY=float.PositiveInfinity,maxX=float.NegativeInfinity,maxY=float.NegativeInfinity;
                foreach(var renderer in tree.ModelSlot.GetComponentsInChildren<Renderer>())
                {var bounds=renderer.bounds;for(int i=0;i<8;i++){var world=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));var screen=cam.WorldToScreenPoint(world);Vector2 local;RectTransformUtility.ScreenPointToLocalPointInRectangle(safe,screen,null,out local);minX=Mathf.Min(minX,local.x);maxX=Mathf.Max(maxX,local.x);minY=Mathf.Min(minY,local.y);maxY=Mathf.Max(maxY,local.y);}}
                if(!float.IsInfinity(minX)){x=minX-safe.rect.xMin-12;y=minY-safe.rect.yMin-12;bw=maxX-minX+24;bh=maxY-minY+24;}
            }
            if(target!=null)
            {
                var corners=new Vector3[4];target.GetWorldCorners(corners);
                var a=safe.InverseTransformPoint(corners[0]);var b=safe.InverseTransformPoint(corners[2]);
                x=a.x-safe.rect.xMin-8;y=a.y-safe.rect.yMin-8;bw=b.x-a.x+16;bh=b.y-a.y+16;
            }
            float sw=safe.rect.width;x=Mathf.Clamp(x,0,sw);y=Mathf.Clamp(y,0,height);bw=Mathf.Min(bw,sw-x);bh=Mathf.Min(bh,height-y);
            var bl=Vector2.zero;
            Place(dim[0],bl,0,0,x,height);Place(dim[1],bl,x+bw,0,sw-x-bw,height);
            Place(dim[2],bl,x,0,bw,y);Place(dim[3],bl,x,y+bh,bw,height-y-bh);
            Place(border[0],bl,x,y,bw,3);Place(border[1],bl,x,y+bh-3,bw,3);Place(border[2],bl,x,y,3,bh);Place(border[3],bl,x+bw-3,y,3,bh);
            // Above dimming, below mandatory save/DEAD modals (TutorialView hides for them).
            spotlight.SetSiblingIndex(banner.GetSiblingIndex());banner.SetAsLastSibling();

            bool isTreeStep = definition != null && definition.TargetElement == "tree";
            bool isPanelOpen = upgrades.IsOpen || shop.IsVisible || achievements.IsVisible || settings.IsVisible;

            float bannerW, bannerX, bannerY = -226f, bannerH = 116f;

            if (isTreeStep && !isPanelOpen)
            {
                // When tapping the tree, dock banner to the right side of the tree tap area
                // Keeps resource cards above (-120 to -216), tree center, and tap sphere completely clear
                float treeRight = center + 140f;
                float availRight = sw - treeRight - 24f;
                if (availRight >= 400f)
                {
                    bannerW = Mathf.Min(480f, availRight);
                    bannerX = treeRight + 12f;
                }
                else
                {
                    bannerW = Mathf.Min(500f, sw - 48f);
                    bannerX = Mathf.Clamp(center - bannerW * 0.5f, 24f, sw - bannerW - 24f);
                }
            }
            else if (isPanelOpen)
            {
                // When panel is open on the side, place banner inside the remaining open viewport
                float availW = (sw - Mathf.Clamp(sw * 0.38f, 380f, 520f)) - 48f;
                bannerW = Mathf.Min(480f, availW);
                bannerX = Mathf.Clamp(center - bannerW * 0.5f, 24f, sw - bannerW - 24f);
            }
            else
            {
                bannerW = Mathf.Min(520f, sw - 48f);
                bannerX = Mathf.Clamp(center - bannerW * 0.5f, 24f, sw - bannerW - 24f);
            }

            Place(banner, topLeft, bannerX, bannerY, bannerW, bannerH);
            GameUiTheme.Paper(banner.GetComponent<Image>());
            banner.GetComponent<Image>().raycastTarget = false;

            var titleRt = banner.Find("Title") as RectTransform;
            if (titleRt != null) Place(titleRt, topLeft, 20, -12, bannerW - 40, 26);

            bool hasConfirm = definition != null && definition.RequiresConfirmation;
            float descW = hasConfirm ? bannerW - 170f : bannerW - 40f;
            var desc = banner.Find("Desc").GetComponent<Text>();
            desc.color = GameUiTheme.Ink;
            Place((RectTransform)desc.transform, topLeft, 20, -42, descW, 64);
            desc.fontSize = 15;
        }

        private RectTransform MakeScroll(RectTransform panel,string contentName,float top,float bottom)
        {
            var content=panel.Find(contentName) as RectTransform;
            var viewport=new GameObject("ListViewport",typeof(RectTransform),typeof(Image),typeof(RectMask2D)).GetComponent<RectTransform>();
            viewport.SetParent(panel,false);Stretch(viewport);viewport.offsetMin=new Vector2(28,bottom);viewport.offsetMax=new Vector2(-28,-top);
            viewport.GetComponent<Image>().color=new Color(1,1,1,.005f);
            content.SetParent(viewport,false);content.anchorMin=topLeft;content.anchorMax=new Vector2(1,1);content.pivot=topLeft;content.anchoredPosition=Vector2.zero;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            return content;
        }
        private void AddOutsideClose(string name,Action close)
        {
            var root=Find(name);if(root==null)return;
            var click=root.gameObject.AddComponent<ReferenceOutsideClose>();
            click.Panel=root.GetChild(0) as RectTransform;click.Close=close;
        }
        private void Header(RectTransform panel,string caption)
        {
            var header=Solid("ReferenceHeader",panel,new Color(.25f,.15f,.07f));
            header.anchorMin=new Vector2(0,1);header.anchorMax=Vector2.one;header.pivot=topLeft;header.offsetMin=new Vector2(12,-86);header.offsetMax=new Vector2(-12,-12);
            header.SetAsFirstSibling();
            var title=GameUiTheme.Label("ReferenceTitle",header,new Vector2(115,-10),new Vector2(450,52),30);title.text=caption;title.color=Color.white;title.fontStyle=FontStyle.Bold;
            if(panel.name=="ShopPanel"||panel.name=="AchievementPanel")title.enabled=false;
        }
        private void Back(RectTransform panel,Action close)
        { var b=NewButton("Back",panel,"←",()=>close());Place((RectTransform)b.transform,topLeft,18,-18,92,62); }
        private void Tabs(RectTransform panel,string[] captions,Action<int> action,float y=115)
        {
            var root=new GameObject("ReferenceTabs",typeof(RectTransform)).GetComponent<RectTransform>();root.SetParent(panel,false);
            for(int i=0;i<captions.Length;i++){int index=i;NewButton("Tab_"+i,root,captions[i],()=>{action(index);foreach(var b in root.GetComponentsInChildren<Button>())b.GetComponent<Image>().color=b.name=="Tab_"+index?new Color(.65f,.42f,.85f):Color.white;});}
        }
        private void LayoutTabs(RectTransform root,float width,float y)
        { Place(root,topLeft,24,-y,width-48,50);float cw=(width-48)/root.childCount;for(int i=0;i<root.childCount;i++)Place((RectTransform)root.GetChild(i),topLeft,i*cw,0,cw-6,50); }
        private Button NewButton(string name,Transform parent,string caption,Action action)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);
            var label=GameUiTheme.Label("Label",go.transform,Vector2.zero,new Vector2(100,50),20);label.text=caption;label.alignment=TextAnchor.MiddleCenter;
            var b=go.GetComponent<Button>();b.onClick.AddListener(()=>action());GameUiTheme.StyleButton(b);return b;
        }
        private RectTransform Solid(string name,Transform parent,Color color)
        { var r=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();r.SetParent(parent,false);r.GetComponent<Image>().color=color;r.GetComponent<Image>().raycastTarget=false;return r; }
        private RectTransform Find(string path) {return safe.Find(path) as RectTransform;}
        private void Side(RectTransform panel,bool right,float width,float height)
        { Place(panel,right?topRight:topLeft,right?-24:24,-128,width,height); }
        private static void Stretch(RectTransform r) {r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        private static void Place(RectTransform r,Vector2 a,float x,float y,float w,float h)
        {if(r!=null)GameUiTheme.Place(r,a,new Vector2(x,y),new Vector2(w,h));}
    }

    public sealed class ReferenceOutsideClose : MonoBehaviour, IPointerClickHandler
    {
        public RectTransform Panel;
        public Action Close;
        public void OnPointerClick(PointerEventData data)
        {
            // uGUI bubbles clicks on empty panel space: only the actual backdrop may close it.
            if(Panel!=null && !RectTransformUtility.RectangleContainsScreenPoint(Panel,data.position,data.pressEventCamera))Close?.Invoke();
        }
    }
}
