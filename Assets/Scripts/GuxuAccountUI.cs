using UnityEngine;
using UnityEngine.UI;

namespace Gxdl {
public partial class GxdlGame {
 GuxuAccountSession accounts;
 bool accountGameOpen;
 InputField phoneInput,codeInput;
 string phoneDraft="",codeDraft="";
 InputField experienceUserInput,experiencePasswordInput;
 string experienceUserDraft="",experiencePasswordDraft="";
 bool experienceLogin;
 Text resendLabel;
 Button resendButton;
 public bool CanPlay=>Rules!=null&&EntryStage==GuxuEntryStage.Ready&&(accounts==null||accounts.CanPlay&&accountGameOpen);
 void InitializeAccountSession(){
  if(!GuxuAccountSession.RequireLogin){GameRules.SelectAccount("");Rules=new GameRules(OriginalConfig.Load(),GameRules.ReadSave());EntryStage=GuxuEntryStage.Ready;Home();PlayOpeningIfNeeded();return;}
  accounts=gameObject.AddComponent<GuxuAccountSession>();accounts.Changed+=RenderAccountState;RenderAccountState();
 }
 void RenderAccountState(){
  if(accounts.CanPlay){
   if(EntryStage!=GuxuEntryStage.Login)return;
   phoneDraft=codeDraft=experienceUserDraft=experiencePasswordDraft="";BeginGameEntry(accounts.SaveScope);return;
  }
  bool saveProgress=accountGameOpen;CancelGameEntry();accountGameOpen=false;
  if(view)view.gameObject.SetActive(false);
  auto=false;StopAllCoroutines();busy=false;battle=false;resolvingDrop=false;storyGeneration++;
  if(storyController){storyController.gameObject.SetActive(false);Destroy(storyController.gameObject);storyController=null;}
  if(saveProgress){try{Rules.Store();}catch(System.Exception error){Debug.LogWarning("[Accounts] Could not save progress: "+error.GetType().Name);}}
  Rules=null;
  if(phoneInput)phoneDraft=phoneInput.text;if(codeInput)codeDraft=codeInput.text;
  if(experienceUserInput)experienceUserDraft=experienceUserInput.text;if(experiencePasswordInput)experiencePasswordDraft=experiencePasswordInput.text;
  Clear();ExpansionScreen="login";var loginBackground=GuxuUiArt.Optional("screen-login");
  var backdrop=Art(loginBackground??Sprite("Game/Guxu/Final/background"),view,0,0,750,1334,false);backdrop.name="Account Login Background";
  var fit=backdrop.gameObject.AddComponent<AspectRatioFitter>();fit.aspectRatio=backdrop.sprite.rect.width/backdrop.sprite.rect.height;fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;
  if(GuxuUiArt.Optional("ui23-panel-dark")){Txt("古 墟 大 陆",view,65,48,620,98,66,gold).name="Account Title";var subtitleShade=Rect("Account Subtitle Backing",view,120,155,510,72).gameObject.AddComponent<GuxuSoftVfx>();subtitleShade.raycastTarget=false;subtitleShade.color=new Color(.02f,.08f,.12f,.85f);Txt("一符破古封 · 一剑问前尘",view,100,169,550,42,25,cream).name="Account Subtitle";}else if(!loginBackground){Img("",view,0,0,750,1334,new Color(.03f,.10f,.14f,.78f));Txt("古 墟 大 陆",view,75,135,600,90,54,gold);}
  float panelHeight=accounts.CanCertify?560:690;var panel=Rect("Account Login",view,45,290,660,panelHeight);
  LoginImage("panel",panel,0,0,660,panelHeight);Txt("账 号 登 录",panel,80,16,500,40,28,gold);
  var consent=Btn((accounts.Consented?"☑":"□")+" 我已阅读并同意",panel,42,65,292,42,()=>{accounts.SetConsent(!accounts.Consented);RenderAccountState();},null,Color.clear,23);consent.name="Account Consent";consent.interactable=!accounts.IsBusy;
  var terms=Btn("用户协议",panel,326,65,132,42,()=>Application.OpenURL(accounts.Config.termsUrl),null,Color.clear,23);terms.interactable=GuxuAccountConfig.Https(accounts.Config.termsUrl);terms.GetComponentInChildren<Text>().color=new Color(.42f,1,.95f);
  Txt("和",panel,457,65,30,42,23,gold);
  var privacy=Btn("隐私政策",panel,486,65,132,42,()=>Application.OpenURL(accounts.Config.privacyUrl),null,Color.clear,23);privacy.interactable=GuxuAccountConfig.Https(accounts.Config.privacyUrl);privacy.GetComponentInChildren<Text>().color=new Color(.42f,1,.95f);
  Txt(accounts.Message=="请选择登录方式"?"":accounts.Message,panel,40,110,580,42,21,cream);
  if(accounts.CanCertify){
   Txt("实名认证和防沉迷验证完成后即可进入游戏",panel,40,160,580,80,27,cream);
   LoginButton("code","继续实名认证",panel,55,285,550,78,accounts.RetryCertification);
   LoginButton("code","退出 / 切换账号",panel,55,395,550,78,accounts.Logout);
  }else{
#if UNITY_EDITOR
   System.Action tapAction=GuxuPhoneTestLogin.Enabled?(System.Action)accounts.LoginTap:accounts.LoginEditorPreview;
#else
   System.Action tapAction=accounts.LoginTap;
#endif
   var tap=LoginButton("tap","",panel,55,155,550,103,tapAction);tap.name="Account Tap Login";tap.interactable=!accounts.IsBusy;
   if(GuxuPhoneTestLogin.Enabled){
    var mode=Btn(experienceLogin?"体验账号密码  ·  切换手机号 ›":"手机验证码  ·  切换体验账号 ›",panel,55,264,550,42,()=>{experienceLogin=!experienceLogin;RenderAccountState();},null,Color.clear,23);
    mode.name="Account Login Mode";mode.interactable=!accounts.IsBusy;
   }else{LoginImage("divider",panel,60,280,100,12);LoginImage("divider",panel,500,280,100,12);
   Txt("手机验证码登录 / 注册",panel,157,264,346,42,25,cream);
   }
   if(GuxuPhoneTestLogin.Enabled&&experienceLogin){
    experienceUserInput=AccountInput("Experience Username",panel,55,318,550,"请输入体验账号",32,false);
    experienceUserInput.contentType=InputField.ContentType.Standard;experienceUserInput.keyboardType=TouchScreenKeyboardType.ASCIICapable;experienceUserInput.SetTextWithoutNotify(experienceUserDraft);
    experiencePasswordInput=AccountInput("Experience Password",panel,55,408,550,"请输入密码",64,true);
    experiencePasswordInput.contentType=InputField.ContentType.Password;experiencePasswordInput.keyboardType=TouchScreenKeyboardType.ASCIICapable;experiencePasswordInput.SetTextWithoutNotify(experiencePasswordDraft);
    var login=LoginButton("login","账号密码登录",panel,55,508,550,105,()=>accounts.LoginExperience(experienceUserInput.text,experiencePasswordInput.text));login.name="Experience Login";login.interactable=!accounts.IsBusy;
    experienceUserInput.interactable=experiencePasswordInput.interactable=!accounts.IsBusy;
    Txt("体验账号独立保存进度，首次登录创建角色",panel,40,625,580,34,21,cream);
   }else{
   phoneInput=AccountInput("Account Phone",panel,55,318,550,"请输入 11 位手机号码",11,false);phoneInput.SetTextWithoutNotify(phoneDraft);
   codeInput=AccountInput("Account Code",panel,55,408,337,"6 位验证码",6,true);codeInput.SetTextWithoutNotify(codeDraft);
   resendButton=LoginButton("code","获取验证码",panel,410,408,195,72,()=>accounts.SendCode(phoneInput.text.Trim()),23);resendButton.name="Account Send Code";
   resendLabel=resendButton.GetComponentInChildren<Text>();
   var phoneLogin=LoginButton("login","",panel,55,508,550,105,()=>accounts.LoginPhone(phoneInput.text.Trim(),codeInput.text.Trim()));phoneLogin.name="Account Phone Login";phoneLogin.interactable=!accounts.IsBusy;
   phoneInput.interactable=codeInput.interactable=!accounts.IsBusy;
   Txt("未注册的手机号验证成功后自动创建账号",panel,40,625,580,34,21,cream);
   }
  }
  Rect("Account Footer Shade",view,0,1030,750,304).gameObject.AddComponent<GuxuStoryShade>();
  DrawLoginNotices();
  Txt(GuxuPhoneTestLogin.Enabled?"真机测试包 · 短信与实名验证已跳过":"实名信息由 TapTap 官方认证页面处理",view,65,1235,620,40,20,cream);
#if UNITY_EDITOR
  if(!GuxuPhoneTestLogin.Enabled)Txt("编辑器测试 · TapTap 按钮暂时跳过验证",view,45,1290,660,30,18,gold);
#endif
 }
 InputField AccountInput(string name,Transform parent,float x,float y,float width,string hint,int length,bool code){
  var background=LoginImage("input-normal",parent,x,y,width,72);background.name=name;background.raycastTarget=true;
  if(!GuxuUiArt.Optional("ui23-input-field"))LoginImage(code?"shield":"phone",background.transform,20,16,32,40);else Txt(code?"◇":"◉",background.transform,20,16,32,40,25,cream);
  var text=Txt("",background.transform,68,0,width-86,72,27,GuxuUiArt.Optional("ui23-input-field")?cream:new Color(.19f,.25f,.31f),TextAnchor.MiddleLeft);text.supportRichText=false;
  var placeholder=Txt(hint,background.transform,68,0,width-86,72,24,GuxuUiArt.Optional("ui23-input-field")?new Color(.72f,.79f,.77f):new Color(.36f,.40f,.44f),TextAnchor.MiddleLeft);
  var input=background.gameObject.AddComponent<InputField>();input.textComponent=text;input.placeholder=placeholder;input.characterLimit=length;
  input.targetGraphic=background;input.transition=GuxuUiArt.Optional("ui23-input-field")?Selectable.Transition.ColorTint:Selectable.Transition.SpriteSwap;
  input.spriteState=new SpriteState{highlightedSprite=LoginSprite("input-focus"),selectedSprite=LoginSprite("input-focus"),pressedSprite=LoginSprite("input-focus"),disabledSprite=LoginSprite("input-normal")};
  input.contentType=InputField.ContentType.IntegerNumber;input.keyboardType=code?TouchScreenKeyboardType.NumberPad:TouchScreenKeyboardType.PhonePad;
  input.lineType=InputField.LineType.SingleLine;return input;
 }
 Sprite LoginSprite(string key){
  string path="Game/Guxu/Expansion/UI/Login/"+key;if(sprites.TryGetValue(path,out var known)&&known)return known;
  var texture=Resources.Load<Texture2D>(path);if(!texture)throw new System.InvalidOperationException("Missing login slice: "+key);
  Vector4 border=key.StartsWith("input-")?new Vector4(27,26,27,26):key.StartsWith("code-")?new Vector4(31,26,31,26):Vector4.zero;
  var result=UnityEngine.Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border);result.name="Login/"+key;return sprites[path]=result;
 }
 Image LoginImage(string key,Transform parent,float x,float y,float width,float height){
  var replacement=key=="panel"?GuxuUiArt.Optional("ui23-panel-dark",true):key.StartsWith("input-")?GuxuUiArt.Optional("ui23-input-field",true):null;
  var image=Art(replacement??LoginSprite(key),parent,x,y,width,height,false);if(replacement)image.pixelsPerUnitMultiplier=key=="panel"?2*replacement.rect.width/512f:replacement.rect.height/height;image.name="Login Art "+key;return image;
 }
 Button LoginButton(string kind,string label,Transform parent,float x,float y,float width,float height,System.Action action,int size=26){
  bool primary=kind=="login";var sprite=GuxuUiArt.Optional(primary?"ui23-button-primary":"ui23-button-secondary",true);
  var image=sprite?Art(sprite,parent,x,y,width,height,false):LoginImage(kind+"-normal",parent,x,y,width,height);image.raycastTarget=true;if(sprite)image.pixelsPerUnitMultiplier=sprite.rect.height/height;
  var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.ColorTint;
  button.onClick.AddListener(()=>action());string text=label.Length>0?label:kind=="tap"?"TapTap 登录":"验证并登录";
  if(sprite||label.Length>0)Txt(text,image.transform,16,0,width-32,height,size,primary?ink:cream);return button;
 } void UpdateAccountUI(){MonitorGameEntry();if(accounts&&resendLabel&&resendButton){int remaining=accounts.ResendSeconds;resendLabel.text=remaining>0?remaining+"秒后重发":"获取验证码";resendButton.interactable=!accounts.IsBusy&&remaining==0;}}
 void DrawAccountEntry(){if(accounts&&CanPlay)SealButton("账号",view,590,165,120,45,ShowAccountPanel).name="Account Settings";}
 void ShowAccountPanel(){if(!CanPlay||busy)return;auto=false;var p=OpenModal("账号",410);
#if UNITY_EDITOR
  string status="编辑器测试，已跳过账号与实名验证";
#else
  string status="已完成本次实名认证与防沉迷检查";
#endif
  if(GuxuPhoneTestLogin.Enabled)status="真机测试账号，未进行短信与实名验证";
  Txt(accounts.DisplayName+"\n"+status,p,45,110,590,120,27,ink);SealButton("退出 / 切换账号",p,155,280,370,70,accounts.Logout);}
}
}




