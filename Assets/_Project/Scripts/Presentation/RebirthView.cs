using System;
using System.Globalization;
using CayTheGioi.Bootstrap;
using CayTheGioi.Domain;
using CayTheGioi.Configuration;
using UnityEngine;
using UnityEngine.UI;
namespace CayTheGioi.Presentation
{
    public sealed class RebirthView : MonoBehaviour
    {
        private GameBootstrapper bootstrap;
        private PrototypeConfig config;
        private PrototypeRules rules;
        private TreeView tree;
        private OrbitCameraRig rig;
        private TreeInputAdapter input;
        private FoundationHudView hud;
        private GameObject blocker;
        private RectTransform panel;
        private Text text;
        private Text overviewText;
        private Button beginButton;
        private bool confirming;
        public Button BeginButton => beginButton;
        public bool IsConfirming => IsVisible && confirming;
        private string previewRunId, feedback="";
        private bool previousInput, previousLock, deadShowing, pendingRefresh;
        public Button OpenButton {get;private set;}
        public Button ConfirmButton {get;private set;}
        public Button CancelButton {get;private set;}
        public bool IsVisible => blocker!=null&&blocker.activeSelf;
        public string Content => text==null?"":text.text;
        public void Bind(GameBootstrapper b,PrototypeConfig profile,TreeView target,OrbitCameraRig camera,TreeInputAdapter adapter,FoundationHudView view, PrototypeRules runtimeRules)
        { rules=runtimeRules;
            bootstrap=b;config=profile;tree=target;rig=camera;input=adapter;hud=view;
            Transform safe=transform.Find("SafeArea")??transform;
            OpenButton=Button("RebirthOpen",safe,"Rebirth",new Vector2(.5f,0),new Vector2(0,28),new Vector2(300,88));OpenButton.onClick.AddListener(Open);
            blocker=new GameObject("RebirthBlocker",typeof(RectTransform),typeof(Image));blocker.transform.SetParent(safe,false);
            var r=(RectTransform)blocker.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            blocker.GetComponent<Image>().color=new Color(0,0,0,.6f);
            panel=new GameObject("RebirthPanel",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();panel.SetParent(r,false);
            panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);panel.sizeDelta=new Vector2(980,800);panel.GetComponent<Image>().color=new Color(.91f,.96f,.9f);
            text=Label("RebirthText",panel,new Vector2(0,1),new Vector2(28,-24),new Vector2(924,640),28);
            ConfirmButton=Button("RebirthConfirm",panel,"Xác nhận Rebirth",new Vector2(0,0),new Vector2(28,28),new Vector2(420,88));ConfirmButton.onClick.AddListener(Confirm);
            CancelButton=Button("RebirthCancel",panel,"Hủy",new Vector2(1,0),new Vector2(-28,28),new Vector2(300,88));CancelButton.onClick.AddListener(Cancel);
            overviewText=Label("RebirthOverview",panel,new Vector2(0,1),new Vector2(36,-110),new Vector2(690,590),24);
            beginButton=Button("RebirthBegin",panel,"TRÙNG SINH",new Vector2(.5f,0),new Vector2(0,40),new Vector2(420,82));
            beginButton.onClick.AddListener(BeginConfirmation);
            blocker.SetActive(false);b.Rebirth.Completed+=OnCommitted;Refresh();
        }
        public void Open()
        {
            if(!bootstrap.Rebirth.GetQuote().CanConfirm)return;
            Show();confirming=false;Refresh();GetComponent<ReferenceUiLayout>()?.Layout();
        }
        public void BeginConfirmation() { if(!IsVisible)return; confirming=true; Refresh();GetComponent<ReferenceUiLayout>()?.Layout(); }
        private void Show()
        {
            if(!IsVisible){previousInput=input.GameplayAllowed;previousLock=rig.TransitionLocked;}
            input.GameplayAllowed=false;input.Cancel();rig.TransitionLocked=true;
            var upgrades=GetComponent<UpgradePanelView>();if(upgrades!=null)upgrades.Close();
            var offline=GetComponent<OfflineRewardView>();if(offline!=null)offline.Clear();
            previewRunId=bootstrap.State.run.runId;feedback="";blocker.SetActive(true);blocker.transform.SetAsLastSibling();
        }
        public void Cancel()
        {
            if(bootstrap.State.run.life=="DEAD")return;
            Hide();
        }
        private void Hide()
        { blocker.SetActive(false);input.GameplayAllowed=previousInput;input.Cancel();rig.TransitionLocked=previousLock;previewRunId=null;feedback=""; }
        public void Confirm()
        {
            if(!IsVisible)return;
            var result=bootstrap.Rebirth.TryRebirth(previewRunId);
            if(result.Success){if(IsVisible)Hide();Refresh();}
            else {feedback=result.Reason.StartsWith("SAVE_FAILED")?"Chưa lưu được: chưa nhận point, chưa reset. Bấm xác nhận để thử lại.":"Chưa thể Rebirth: "+result.Reason;Refresh();}
        }
        private void OnCommitted()
        {
            Hide();deadShowing=false;pendingRefresh=true;
            var offline=GetComponent<OfflineRewardView>();if(offline!=null)offline.Clear();
            var visual=config.FindVisual(bootstrap.State.run.stageId);
            try {tree.ShowStage(visual);rig.OrbitLocked=bootstrap.State.settings.orbitLocked;rig.SetStagePose(visual.cameraDistance,visual.cameraPitch,visual.cameraTargetHeight);hud.Bind(bootstrap.State,rules.FindStage(bootstrap.State.run.stageId).displayName,rules.tapsPerReward);}
            catch(Exception error){Debug.LogError("[CayTheGioi] Rebirth view failed: "+error.Message);}
            bootstrap.Resources.NotifyExternalCommit();
        }
        public void Refresh()
        {
            if(bootstrap==null||bootstrap.Rebirth==null)return;
            var quote=bootstrap.Rebirth.GetQuote();bool dead=bootstrap.State.run.life=="DEAD";
            OpenButton.interactable=quote.CanConfirm&&!dead;
            OpenButton.GetComponentInChildren<Text>().text=quote.Reason=="LOCKED"?"Rebirth · Cây Non":"Rebirth";
            if(!IsVisible)return;
            CancelButton.gameObject.SetActive(!dead);ConfirmButton.interactable=quote.CanConfirm;
            if(dead)confirming=true;
            text.gameObject.SetActive(confirming);
            ConfirmButton.gameObject.SetActive(confirming);
            beginButton.gameObject.SetActive(!confirming);
            beginButton.interactable=quote.CanConfirm;
            overviewText.gameObject.SetActive(!confirming);
            string wallets="ĐIỂM TRÙNG SINH HIỆN CÓ\n\n";
            foreach(var stage in rules.stages)
            {
                wallets+=stage.displayName.ToUpperInvariant()+"   × "+PermanentEffects.Points(bootstrap.State.permanent,stage.stageId)+"\n";
                foreach(var effectEntry in rules.pointEffects) if(effectEntry.stageId==stage.stageId)
                    wallets+="   +"+(effectEntry.perPoint*100).ToString("0.#",CultureInfo.InvariantCulture)+"% "+effectEntry.targetId+" / điểm\n";
                wallets+="\n";
            }
            overviewText.text=wallets+"Trùng sinh bây giờ sẽ nhận:\n+1 ĐIỂM "+quote.StageName.ToUpperInvariant();
            string effect="";
            foreach(var e in rules.pointEffects)if(e.stageId==quote.StageId)effect=e.targetId=="protection"?string.Format(CultureInfo.InvariantCulture,"+{0:0.##} Protection",e.perPoint):string.Format(CultureInfo.InvariantCulture,"+{0:0.##}% {1}",e.perPoint*100,e.targetId=="water"?"Root Water":e.targetId=="growth"?"Trunk Growth":e.targetId=="photosynthesis"?"Leaves Quang hợp":"Hấp thụ Linh khí");
            text.text=(dead?"CÂY ĐÃ CHẾT — Forced Rebirth":"Xác nhận Rebirth")+"\n"+quote.StageName+" → Mầm · +1 point "+quote.StageName+" ("+quote.Points+" → "+(quote.Points==long.MaxValue?"tràn số":(quote.Points+1).ToString())+")\nEffect riêng: "+effect+"; cap không tăng.\nReset: Water "+rules.initialWater+", Quang hợp "+rules.initialPhotosynthesis+", Growth "+rules.initialGrowth+", Linh khí 0 (khóa)"+", level0, Mầm, HP"+rules.maxHp+", status, Weather Bình yên, thống kê run.\nGiữ: point, Coin, Achievement/claim, cosmetic/equip, tutorial, settings, mốc đạt; Sức sống và tap dở.\nVí: Mầm "+PermanentEffects.Points(bootstrap.State.permanent,"seedling")+" · Cây Non "+PermanentEffects.Points(bootstrap.State.permanent,"young_tree")+" · Cây Lớn "+PermanentEffects.Points(bootstrap.State.permanent,"large_tree")+" · Trưởng Thành "+PermanentEffects.Points(bootstrap.State.permanent,"mature_tree")+"\n"+feedback+(quote.CanConfirm?"":"\n"+quote.Reason);
        }
        private void LateUpdate()
        {
            if(bootstrap==null)return;
            bool dead=bootstrap.State.run.life=="DEAD";
            if(dead&&!deadShowing){Show();deadShowing=true;Refresh();Canvas.ForceUpdateCanvases();GetComponent<ReferenceUiLayout>()?.Layout();}
            else if(!dead&&deadShowing){Hide();deadShowing=false;}
            bool portrait=Screen.width<Screen.height;
            if(GetComponent<ReferenceUiLayout>()==null)
            {
                var open=(RectTransform)OpenButton.transform;open.anchorMin=open.anchorMax=open.pivot=new Vector2(.5f,0);open.anchoredPosition=new Vector2(0,28);
                panel.sizeDelta=new Vector2(Mathf.Min(980,((RectTransform)blocker.transform.parent).rect.width-40),800);
                ((RectTransform)text.transform).sizeDelta=new Vector2(panel.rect.width-56,640);
            }
            if(IsVisible)
            {
                blocker.transform.SetAsLastSibling();
                // A pending catch-up must be retried before a Rebirth transaction is allowed.
                var errors=blocker.transform.parent.Find("SaveLoadBlocker");if(errors!=null&&errors.gameObject.activeSelf)errors.SetAsLastSibling();
            }
            if(pendingRefresh){pendingRefresh=false;var u=GetComponent<UpgradePanelView>();if(u!=null)u.Refresh();var e=GetComponent<EvolutionPanelView>();if(e!=null)e.Refresh();}
            Refresh();
        }
        private void OnDestroy(){if(bootstrap!=null&&bootstrap.Rebirth!=null)bootstrap.Rebirth.Completed-=OnCommitted;}
        private static Text Label(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size,int font)
        {
            var t=new GameObject(name,typeof(RectTransform),typeof(Text)).GetComponent<Text>();t.transform.SetParent(parent,false);var r=(RectTransform)t.transform;r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=position;r.sizeDelta=size;
            t.font=UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=font;t.color=new Color(.08f,.23f,.15f);t.raycastTarget=false;return t;
        }
        private static Button Button(string name,Transform parent,string caption,Vector2 anchor,Vector2 pos,Vector2 size)
        {
            var b=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button)).GetComponent<Button>();b.transform.SetParent(parent,false);var r=(RectTransform)b.transform;r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=pos;r.sizeDelta=size;
            b.GetComponent<Image>().color=new Color(.74f,.87f,.75f);var t=Label("Label",r,new Vector2(.5f,.5f),Vector2.zero,size,28);t.text=caption;t.alignment=TextAnchor.MiddleCenter;return b;
        }
    }
}
