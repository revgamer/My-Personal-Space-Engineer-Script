// GOAT Sorter | A Modern Inventory Manager, for a Modern Space Engineer
// Version 1.2.1
// 2026-05-12
// Script Made by Khodrin, Documentation Made by Katarina_Valenxia
// 
// Workshop Page: https://steamcommunity.com/sharedfiles/filedetails/?id=3581915365
// 
// Guide Page: https://steamcommunity.com/sharedfiles/filedetails/?id=3574650371
// 
// Need Help? Join our Discord Server: https://discord.gg/btYca5Wkme
// 
// !!!NEW GSIM  INTEGRATION MOD FOR THIS SCRIPT!!!
// allows fully automated modded item learning and a bunch of Quality of Life features!
// check it out here: https://steamcommunity.com/sharedfiles/filedetails/?id=3613336393

// Enable Tutorials? It's quick guide through the script!
bool Tutorials = true;

// --- Machine Managing ---

// Allow Script to Group and Balance Reactors
bool MRs = true;
// Amount of Uranium per 1.000 Liter of Volume for Reactors.
int UA = 25;

// Allow Script to Group and Balance O2/H2 Generators and Irrigation Systems
bool MGG = true;
// Amount of Ice per 1.000 Liter of Volume for O2/H2 Generators and Irrigation Systems.
int GGI = 2500;

// Note: Reactors, O2/H2 Generators and Irrigation Systems have a Priority of 100.


// --- Adjustable Settings and Tags for Machines ---

// Tags to unmanage Reactors, O2/H2 Generators and Irrigation Systems
public static string[] MTS = { "!GSIM-Manual", "[Manual]" };

// Enables the AutoCrafting Feature.
bool AC = true;
// Reverse the AutoCrafting Priority system?
bool RCP = false;

// Enables the handling of refineries via this script
bool RH = true;
// Always refine Scrap first. (Even if Quota is Needed)
bool RSF = true;
// Refine Ores that are Stored inside [Stock] Containers?
bool STOR = true;
// Show warning if Ores from Type and or Stock Containers cant be moved into Refineries?
bool SCW = true;

// Keep Queueing Ingots if you only have a Survival Kit and no Refineries?
bool SKI = true;

// Ignore Production blocks on Subgrids
bool SGP = true;

// Ignore Inventories of Welder, Grinder, Drills
bool IBT = true;


// --- Adjustable Tags ---

// Tags to Ignore Inventories
public static string[] ITS = { "!GSIM-Locked", "[Locked]", "Control Station", "Seat", "Helm", "Safe Zone" };

// Tags to mark Stock LCDs / Containers
public static string[] SCT =
{
    "!GSIM-Stock",
    "[Stock]",
//    "MyCustomStockTag",
};

// Tags to not count Items for the AutoCrafting Screen
public static string[] HTS =
{
    "!GSIM-Hidden",
    "[Hidden]",
//    "Turret",
//    "MyCustomHiddenTag",
};

// Connector Tags to Ignore untagged Inventories for Connected Subgrids
static string[] SET = { "!GSIM-NoSorting", "[No Sorting]" };

// Connector Tags to Ignore Connected Subgrids completely.
static string[] HET = { "!GSIM-NoGOAT", "[No GOAT]" };


// --- Adjustable Display Tags, make sure that the Formating is Correct! ---

// Tag for the main Auto Crafting panel
static string[] ACT = { "!GSIM-AutoCrafting", "[AutoCrafting]" };
// Tag to Extend the AutoCrafting panel Display.
static string[] ACE = { "!GSIM-AutoEXT:", "[AutoCraftingExtension:" };

// Tags to Display the Script Logs
static string[] LST = { "!GSIM-LogScreen", "[LogScreen]" };

// Tags to Display Script Status
static string[] SST = { "!GSIM-StatusScreen", "!StatusScreen", "[StatusScreen]" };

// Tags for Multi-LCD Blocks to be used as Inventory Screens
static string[] MLT = { "!GSIM-MultiLCD", "[GOAT]" };

// Tags for Single LCD Blocks to be used as Inventory Screens
static string[] IST = { "!GSIM-Inventory", "[Inventory]" };

// Tags for the Container Level LCDs
static string[] CST = { "!GSIM-ContainerLevel", "[ContainerLevel]" };


// --- Color blind UI Settings ---

// Enable Color blind UI colors (replaces green and red)
bool CBM = false;

// The UI color used to represent negative UI elements
Color CBNC = new Color(0.341f, 0.286f, 0.64f);

// The UI color used to represent positive UI elements
Color CBPC = new Color(1f, 1f, 0f);


// --- Misc Settings ---

// TURNING THIS OFF WILL BREAK AUTOMATIC ITEM LEARNING!!!
bool VBPL = true;
    
// UTC Time Offset, can be a negative number. Example: -1.5; for a UTC Offset for -1 hour and 30 Minutes
double UTC = 0.0;

// Script Throttle options, useful for PB Limiters!
bool AT = false;
double ATMS = 0.5;

// Minimum Wait Time for a whole Script cycle in Seconds.
int MinCycle = 10;

// Amount of Lines to display for the [LogScreen]
static int LL = 48;
// Allow Single Type Containers to balance Items between each other.
bool RBTC = false;
// VTEC Kicked In YO
int VTEC = 16;

// -!-!-!- END OF SETTINGS, DO NOT CHANGE ANYTHING BEYOND THIS LINE -!-!-!-

MyItemType A;MyItemType B;MyItemType C;MyItemType D;MyItemType E;MyDefinitionId F;string G="1.2.1";public
 Program(){VTEC
=Math.Max(2,VTEC);H();Runtime.UpdateFrequency=UpdateFrequency.Update1|UpdateFrequency.Update10;I["learning"]=J;I[
"learningOre"]=K;I["resetIndex"]=L;I["removeNotCraftableBlueprints"]=M;I["addItem"]=N;I["addItemOverride"]=O;I["addAllKnownItems"]=P;
I["translateItemNames"]=Q;A=MyItemType.Parse("MyObjectBuilder_Ingot/Uranium");B=MyItemType.Parse(
"MyObjectBuilder_Ore/Ice");C=MyItemType.Parse("MyObjectBuilder_Ore/Iron");D=MyItemType.Parse("MyObjectBuilder_Ingot/Iron");E=MyItemType.Parse(
"MyObjectBuilder_Component/Construction");F=MyDefinitionId.Parse("MyObjectBuilder_BlueprintDefinition/Position0010_StoneOreToIngotBasic");R.S=CBM;R.T=CBNC;R.U=
CBPC;V.W=VBPL;X.Y=UTC;var Z=Me.GetSurface(0);if(Z!=null){var a=new List<string>();var b="ColorfulIcons_Component/SolarCell";
Z.ClearImagesFromSelection();Z.AddImageToSelection(b);Z.GetSelectedImages(a);V.c=a.Contains(b);}}void H(){d=e();f=g();}
public void
 Save
(){}bool h=false;int i=1;MyIni j=new MyIni();List<IMyTextPanel>k=new List<IMyTextPanel>();DateTime l;Dictionary<
IMyTerminalBlock,m>n=new Dictionary<IMyTerminalBlock,m>();Dictionary<string,List<m>>o=new Dictionary<string,List<m>>();Dictionary<m,List
<string>>p=new Dictionary<m,List<string>>();List<IMyTerminalBlock>q=new List<IMyTerminalBlock>();List<IMyShipConnector>r=
new List<IMyShipConnector>();List<IMyMechanicalConnectionBlock>s=new List<IMyMechanicalConnectionBlock>();List<
IMyTerminalBlock>t=new List<IMyTerminalBlock>();Dictionary<string,IMyTerminalBlock>u=new Dictionary<string,IMyTerminalBlock>();List<
IMyReactor>v=new List<IMyReactor>();List<IMyGasGenerator>w=new List<IMyGasGenerator>();List<IMyGasTank>x=new List<IMyGasTank>();
List<IMyBatteryBlock>y=new List<IMyBatteryBlock>();List<MyInventoryItem>z=new List<MyInventoryItem>();MyInventoryItem?ª;
MyInventoryItem µ;MyFixedPoint º=0;MyFixedPoint À=0;MyFixedPoint Á=0;IMyInventory Â;IMyInventory Ã;List<string>Ä=new List<string>();
string Å="";string Æ="";IEnumerator<bool>d;IEnumerator<bool>f;Dictionary<MyItemType,V>Ç=new Dictionary<MyItemType,V>();
Dictionary<string,V>È=new Dictionary<string,V>();string[]É;Dictionary<string,V>Ê=new Dictionary<string,V>();List<V>Ë=new List<V>()
;V Ì;List<string>Í=new List<string>();Dictionary<string,float>Î=new Dictionary<string,float>();Dictionary<string,List<
IMyInventory>>Ï=new Dictionary<string,List<IMyInventory>>();int Ð=0;string Ñ="";int Ò;string Ó;int Ô=0;static List<KeyValuePair<
string,DateTime>>Õ=new List<KeyValuePair<string,DateTime>>();IMyTextSurface Ö;IMyTextSurface Ø;string Ù="";int Ú=0;static List
<string>Û=new List<string>();List<IMyProductionBlock>Ü=new List<IMyProductionBlock>();List<IMyProductionBlock>Ý=new List<
IMyProductionBlock>();List<IMyProductionBlock>Þ=new List<IMyProductionBlock>();List<MyProductionItem>ß=new List<MyProductionItem>();List<à
>á=new List<à>();bool â=false;bool ã=false;DateTime?ä;bool å=false;bool æ=false;ç è=new ç();List<IMyRefinery>é=new List<
IMyRefinery>();MyItemType?ê=null;bool ë=false;bool ì=false;bool í=false;List<IMyTextPanel>î=new List<IMyTextPanel>();Dictionary<
IMyTextSurfaceProvider,IMyTerminalBlock>ï=new Dictionary<IMyTextSurfaceProvider,IMyTerminalBlock>();Dictionary<IMyTextSurface,IMyTerminalBlock
>ð=new Dictionary<IMyTextSurface,IMyTerminalBlock>();Dictionary<IMyTextSurface,IMyTerminalBlock>ñ=new Dictionary<
IMyTextSurface,IMyTerminalBlock>();Dictionary<IMyTextSurface,string>ò=new Dictionary<IMyTextSurface,string>();IMyTextSurface ó;
RectangleF ô;List<string>õ=new List<string>();string[]ö;int ø;int ù;int ú;IMyTextPanel û;List<IMyTextPanel>ü=new List<IMyTextPanel
>();bool ý=false;string þ;string[]ÿ;float Ā;float ā;int Ă;int ă;bool Ą;bool ą;MyCommandLine Ć=new MyCommandLine();
Dictionary<string,Action>I=new Dictionary<string,Action>(StringComparer.OrdinalIgnoreCase);float ć;float Ĉ;string ĉ;string Ċ;
string ċ;string Č;int č;IMySlimBlock Ď;Dictionary<V,float>ď=new Dictionary<V,float>();float Đ;float đ;float Ē;bool ē=false;
List<V>Ĕ=new List<V>();float ĕ;bool Ė;int ė;int Ę;MyItemType?ę;bool Ě;static List<string>ě=new List<string>();static List<
string>Ĝ=new List<string>();bool ĝ=false;int Ğ=0;int ğ=0;bool Ġ;bool ġ;List<MyProductionItem>Ģ=new List<MyProductionItem>();
bool ģ;bool Ĥ=false;List<V>ĥ=new List<V>();List<V>Ħ=new List<V>();int ħ;int Ĩ;int ĩ;int Ī;bool ī=false;List<IMyTerminalBlock
>Ĭ=new List<IMyTerminalBlock>();List<IMyTerminalBlock>ĭ=new List<IMyTerminalBlock>();Į į;Į İ;string ı=
" error: output could not be cleared";IEnumerator<bool>e(){while(true){l=DateTime.UtcNow;ĳ:Ĳ();if(!h||true){Ò=1;Ô=0;Ó="Updating Grid";i=Ĵ(Me,out ą);q.Clear()
;t.Clear();u.Clear();ï.Clear();r.Clear();GridTerminalSystem.GetBlocksOfType(r);s.Clear();GridTerminalSystem.
GetBlocksOfType(s);ĵ.Ķ(Me.CubeGrid,SET,HET);yield return true;k.Clear();GridTerminalSystem.GetBlocksOfType(q);yield return true;foreach
(var ķ in r){if(ķ==null)continue;ĵ.ĸ(ķ);if(!ķ.IsConnected)continue;if(ķ.OtherConnector==null)continue;if(!ķ.CustomName.Ĺ(
HET)&&!ķ.OtherConnector.CustomName.Ĺ(HET)){if(i<ĺ(ķ,q)){Ò=0;Ô=0;Ó="Script paused due to Priority";ģ=true;for(int Ļ=0;Ļ<60;Ļ
++){yield return true;}goto ĳ;}}yield return true;}foreach(var ķ in s){ĵ.ļ(ķ);}yield return true;ĵ.Ľ();ģ=false;yield
return true;Ü.Clear();Ý.Clear();î.Clear();v.Clear();é.Clear();w.Clear();y.Clear();x.Clear();Ĭ.Clear();Ú=0;foreach(var ľ in q){
Ú++;if(Ú%(VTEC*2)==0)yield return true;if(ľ==null)continue;if(!ĵ.Ŀ(ľ.CubeGrid)&&ĵ.ŀ(ľ.CubeGrid))continue;if(!ĵ.Ŀ(ľ.
CubeGrid)&&ĵ.Ł(ľ.CubeGrid)){if(ľ.CustomName.Ĺ(SCT)){}else if(MRs&&ľ is IMyReactor){}else if(MGG&&ľ is IMyGasGenerator){}else if(
!SGP&&ľ is IMyRefinery){}else if(!SGP&&ľ is IMyProductionBlock){}else continue;}if(ľ is IMyTextSurfaceProvider&&ľ.
CustomName.Ĺ(MLT))ï.Add(ľ as IMyTextSurfaceProvider,ľ);if(ľ is IMyProductionBlock&&(!SGP||ĵ.Ŀ(ľ.CubeGrid)))Ü.Add(ľ as
IMyProductionBlock);if(ľ is IMyProductionBlock)Ý.Add(ľ as IMyProductionBlock);if(ľ is IMyTextPanel)î.Add(ľ as IMyTextPanel);if(ľ is
IMyTextPanel&&ĵ.Ŀ(ľ.CubeGrid)&&ľ.CustomName.Ĺ(SST))k.Add(ľ as IMyTextPanel);if(ľ is IMyReactor)v.Add(ľ as IMyReactor);if(ľ is
IMyRefinery&&(!SGP||ĵ.Ŀ(ľ.CubeGrid)))é.Add(ľ as IMyRefinery);if(ľ is IMyGasGenerator)w.Add(ľ as IMyGasGenerator);if(ľ is
IMyBatteryBlock&&ĵ.Ŀ(ľ.CubeGrid))y.Add(ľ as IMyBatteryBlock);if(ľ is IMyGasTank&&ĵ.Ŀ(ľ.CubeGrid))x.Add(ľ as IMyGasTank);if(ľ is
IMyTextSurface&&ľ.CustomName.Ĺ(LST)&&ĵ.Ŀ(ľ.CubeGrid)){Ö=ľ as IMyTextSurface;Ö.ContentType=ContentType.TEXT_AND_IMAGE;}if(ľ is
IMyTextSurface&&ľ.CustomName.Contains("[DEBUG]")&&ĵ.Ŀ(ľ.CubeGrid)){Ø=ľ as IMyTextSurface;Ø.ContentType=ContentType.TEXT_AND_IMAGE;}if(
ľ is IMyTextSurface&&ľ.CustomName.Ĺ(SCT)){Ĭ.Add(ľ);(ľ as IMyTextSurface).ContentType=ContentType.SCRIPT;}if(!ľ.
HasInventory)continue;if(ľ.CustomName.Ĺ(ITS))continue;if(IBT&&ľ is IMyShipToolBase)continue;t.Add(ľ);if(!u.ContainsKey(ľ.CustomName)
){u.Add(ľ.CustomName,ľ);}}yield return true;h=true;}if(RH){foreach(var ł in é){if(ł.CustomName.Ĺ(MTS))continue;ł.
UseConveyorSystem=false;}}Ò=3;Ô=0;Ó="Reading Item Index";Ú=0;if(ë){Me.CustomData="";Å="";Ç.Clear();è.Ń();ë=false;}if(ì){foreach(var Ņ in
V.ń){var ņ=MyItemType.Parse(Ņ.Key);if(Ç.ContainsKey(ņ))continue;Ç.Add(ņ,new V(ņ));}ì=false;yield return true;}if(Me.
CustomData!=""){Ä=Me.CustomData.Split('\n').ToList();Å=Me.CustomData;var Ļ=1;for(;Ļ<Ä.Count;Ļ++){if(Ä[Ļ]=="#### Internals ####")
break;var ª=new V(Ä[Ļ]);if(ª.Ň&&!Ç.ContainsKey(ª.ň)){Ç.Add(ª.ň,ª);if(ª.ŉ())è.Ŋ(ª,Ç);}else if(ª.Ň&&Ç.ContainsKey(ª.ň)){Ç[ª.ň].
ŋ(Ä[Ļ]);}if(ª.Ň&&!Í.Contains(ª.Ō))Í.Add(ª.Ō);Ú++;if(Ú%(VTEC*2)==0)yield return true;}for(;Ļ<Ä.Count;Ļ++){if(Ä[Ļ]==
"#### Ore Yields ####")break;É=Ä[Ļ].Split('~');if(É.Length<7)continue;if(!È.ContainsKey(É[1])){È.Add(É[1],new V(É[3],É[4],É[1],É[2]));}}for(;Ļ
<Ä.Count;Ļ++){è.ō(Ä[Ļ],Ç);}}else{Å="";}if(í){foreach(var Ņ in V.ń){var ņ=MyItemType.Parse(Ņ.Key);if(Ç.ContainsKey(ņ)&&Ņ.
Value.Ŏ!="")Ç[ņ].ŏ=Ņ.Value.Ŏ;}í=false;Å="";yield return true;}Ò=3;Ô=0;Ó="Adding Internal Items";if(!È.ContainsKey("OxygenGas"
)){È.Add("OxygenGas",new V("gas","oxygen","OxygenGas","Gases"));}if(!È.ContainsKey("HydrogenGas")){È.Add("HydrogenGas",
new V("gas","hydrogen","HydrogenGas","Gases"));}if(!È.ContainsKey("Electricity")){È.Add("Electricity",new V("energy",
"electricity","Electricity","Energy"));}yield return true;Ë=Ç.Values.ToList();yield return true;Ë=Ë.OrderBy(Ő=>Ő.ő).ToList();yield
return true;Ë=Ë.OrderBy(Ő=>Ő.Ō).ToList();yield return true;Ê.Clear();foreach(var ª in Ë){if(Ê.ContainsKey(ª.ŏ)){ª.ŏ=Œ(Ê,ª.ŏ);}
Ê.Add(ª.ŏ,ª);}foreach(var ª in È){Ê.Add(ª.Value.ŏ,ª.Value);}Ò=4;Ô=0;Ó="Updating Managed Containers";Ú=0;n.Clear();p.Clear
();o.Clear();Į.œ();foreach(var Ŕ in t){if(Ŕ==null)continue;if(Ŕ.CustomName.Ĺ(SCT)){Ô=1;new Į(Ŕ);while(!Į.ŕ(Ŕ).ŋ(Ë,Ê)){
yield return true;}yield return true;}foreach(var Ŗ in Í){if(Ŕ.DisplayNameText.Contains(Ŗ)){if(!n.ContainsKey(Ŕ))n.Add(Ŕ,new
m(Ŕ));if(!n[Ŕ].ŗ.Contains(Ŗ))n[Ŕ].ŗ.Add(Ŗ);}}Ú++;if(Ú%VTEC==0)yield return true;}foreach(var Ř in Ĭ){new Į(Ř,
GridTerminalSystem,t).ŋ(Ë,Ê);}į=new Į(100,"Reactors");į.ř=0;Ô=3;if(MRs&&Ç.ContainsKey(A)){foreach(var Ś in v){if(Ś==null)continue;if(Ś.
CustomName.Ĺ(MTS))continue;if(Į.ś(Ś))continue;į.Ŝ(Ś);į.N(Ç[A],(int)Math.Round((float)Ś.GetInventory().MaxVolume*UA));Ś.
UseConveyorSystem=false;yield return true;}}İ=new Į(100,"GasGenerators");İ.ř=2;Ô=4;if(MGG&&Ç.ContainsKey(B)){foreach(var ŝ in w){if(ŝ==
null)continue;if(ŝ.CustomName.Ĺ(MTS))continue;if(Į.ś(ŝ))continue;İ.Ŝ(ŝ);İ.N(Ç[B],(int)Math.Round((float)ŝ.GetInventory().
MaxVolume*GGI));ŝ.UseConveyorSystem=false;yield return true;}}Ô=5;Į.Ş();yield return true;Ô=6;n=n.OrderByDescending(ş=>ş.Value.i)
.ToDictionary(Š=>Š.Key,Š=>Š.Value);yield return true;Ï.Clear();foreach(var š in n){foreach(var Ŗ in š.Value.ŗ){if(!o.
ContainsKey(Ŗ))o.Add(Ŗ,new List<m>());o[Ŗ].Add(š.Value);if(!p.ContainsKey(š.Value))p.Add(š.Value,new List<string>());p[š.Value].Add
(Ŗ);}if(š.Value.ŗ.Count!=1)continue;if(!Ï.ContainsKey(š.Value.ŗ[0]))Ï.Add(š.Value.ŗ[0],new List<IMyInventory>());Ï[š.
Value.ŗ[0]].Add(š.Key.GetInventory());}Ô=7;Î.Clear();foreach(var Ŗ in Í){Ā=0f;ā=0f;if(o.ContainsKey(Ŗ)){foreach(var Ŕ in o[Ŗ]
){Ā+=(float)Ŕ.Â.CurrentVolume;ā+=(float)Ŕ.Â.MaxVolume;}}if(ā>0f){Î.Add(Ŗ,Ā/ā);}else{Î.Add(Ŗ,-1f);}}Ò=5;Ô=0;Ó=
"Learning Blueprints";â=ã;bool Ţ=false;foreach(var ţ in Ü){yield return true;if(ţ==null)continue;if(ţ is IMyRefinery)continue;if(ţ is
IMyAssembler&&(ţ as IMyAssembler).Mode==MyAssemblerMode.Disassembly)continue;if(ã){if(ä==null){ä=DateTime.UtcNow;Ţ=true;}if(Ţ){ţ.
ClearQueue();for(int Ļ=ţ.OutputInventory.ItemCount-1;Ļ>=0;Ļ--){Ť(ţ.OutputInventory,ţ.OutputInventory.GetItemAt(Ļ).Value);yield
return true;}continue;}ţ.ShowOnHUD=true;if(!ţ.CustomName.Contains(" is Learning"))ţ.CustomName+=" is Learning";if(ţ.CustomName
.Contains(ı)){if(ţ.OutputInventory.ItemCount==0){ţ.CustomName=ţ.CustomName.Replace(ı,"");}ţ.ClearQueue();continue;}â=true
;Ó="Learning Item Recepies";Ô=1;ß.Clear();ţ.GetQueue(ß);if(ß.Count>1){for(int Ļ=ß.Count-1;Ļ>0;Ļ--){ţ.RemoveQueueItem(Ļ,ß[
Ļ].Amount);}continue;}if(ţ.OutputInventory.ItemCount!=1)continue;ª=ţ.OutputInventory.GetItemAt(0);if(!ª.HasValue)continue
;if(!Ç.ContainsKey(ª.Value.Type)){Ç.Add(ª.Value.Type,new V(ª.Value.Type));}Ç[ª.Value.Type].ť=ß[0].BlueprintId;ä=DateTime.
UtcNow;ţ.ClearQueue();Ĥ=Ť(ţ.OutputInventory,ª.Value);if(!Ĥ){if(!ţ.CustomName.Contains(ı))ţ.CustomName+=ı;}}else{ţ.ShowOnHUD=
false;ţ.CustomName=ţ.CustomName.Replace(" is Learning","");ţ.CustomName=ţ.CustomName.Replace(ı,"");}}if(â)continue;if(!ī){Ô=2
;var Ŧ=Me.GetProperty("GSIMIntegration_filteredBlueprintLib")?.As<string>();var ŧ=Ŧ?.GetValue(Me);MyIniParseResult Ũ;
MyIni ũ=new MyIni();if(ũ.TryParse(ŧ,out Ũ)){Ó="Parsing Library Data";List<string>Ū=new List<string>();ũ.GetSections(Ū);
foreach(var ū in Ū){yield return true;Ó=$"Parsing Library Data {ū}";if(!ũ.Get(ū,"Enabled").ToBoolean(false))continue;if(!ũ.Get(
ū,"Atomic").ToBoolean(false))continue;var Ŭ="";ū.ŭ("::","",out Ŭ);var Ů=ũ.Get(ū,"Results").ToString();if(Ů=="")continue;
MyDefinitionId ů;if(!MyDefinitionId.TryParse(V.Ű+Ŭ,out ů))continue;var Ų=ũ.Get(ū,"DisplayName").ToString().ű();if(Ů.IndexOf(',')>=0)
continue;var ų="";Ů.ŭ("","x",out ų);var Ŵ=1;int.TryParse(ų,out Ŵ);if(Ŵ==0)Ŵ=1;Ů=Ů.TrimStart('0','1','2','3','4','5','6','7','8',
'9','.','x');Ů=Ů.Trim();try{MyItemType ŵ=MyItemType.Parse(Ů);V.Ŷ(ŵ.TypeId+"/"+ŵ.SubtypeId,Ų:Ų,Ŭ:ů.ToString(),ŷ:false,Ŵ:Ŵ);Ÿ
($"Added {Ų} to internal library");}catch{}}yield return true;}ī=true;}Ò=6;Ô=0;Ó="Learning Ore Yields";Ă=0;foreach(var ł
in é){yield return true;if(ł==null)continue;if(!ł.IsWorking)continue;Ă++;if(Ă>2)continue;if(æ){ł.ShowOnHUD=true;ł.
UseConveyorSystem=false;if(!ł.CustomName.Contains(" is Learning"))ł.CustomName+=" is Learning";Ô=1;Ó="Learning Ore Yields";â=true;int Ŵ=X
.Ź(ł);if(ê==null&&ł.InputInventory.ItemCount>0){z.Clear();ł.InputInventory.GetItems(z);for(int Ļ=z.Count-1;Ļ>=0;Ļ--){º=z[
Ļ].Amount;if(!Ç.ContainsKey(z[Ļ].Type))Ç.Add(z[Ļ].Type,new V(z[Ļ].Type));Ť(ł.InputInventory,z[Ļ]);yield return true;}}
else if(ê==null&&ł.OutputInventory.ItemCount>0){z.Clear();ł.OutputInventory.GetItems(z);for(int Ļ=z.Count-1;Ļ>=0;Ļ--){º=z[Ļ]
.Amount;if(!Ç.ContainsKey(z[Ļ].Type))Ç.Add(z[Ļ].Type,new V(z[Ļ].Type));Ť(ł.OutputInventory,z[Ļ]);yield return true;}}else
if(ê==null&&Ğ==0){if(!o.ContainsKey("Ores")){Ÿ("Error: No ores container!");continue;}foreach(var Ŕ in o["Ores"]){Â=Ŕ.Â;z.
Clear();Â.GetItems(z);for(int Ļ=z.Count-1;Ļ>=0;Ļ--){if(!Ç.ContainsKey(z[Ļ].Type))Ç.Add(z[Ļ].Type,new V(z[Ļ].Type));if(!Ç[z[Ļ]
.Type].ŉ())continue;if(!è.ź(Ç[z[Ļ].Type]))è.Ŋ(Ç[z[Ļ].Type],Ç);if(!è.Ż(Ç[z[Ļ].Type]))continue;if(z[Ļ].Amount<100)continue;
if(!Â.CanTransferItemTo(ł.InputInventory,z[Ļ].Type))continue;if(!ł.InputInventory.CanItemsBeAdded(100,z[Ļ].Type))continue;
ê=z[Ļ].Type;Â.TransferItemTo(ł.InputInventory,Ļ,null,true,100);goto ż;}yield return true;}ż:yield return true;}else if(ê.
HasValue&&ł.OutputInventory.ItemCount>0&&ł.InputInventory.ItemCount==0){yield return true;z.Clear();ł.OutputInventory.GetItems(z
);è.Ž(Ç[ê.Value],Ç,z,Ŵ);Ÿ($"Learned Yield for {Ç[ê.Value].ŏ}: {è.ž(Ç[ê.Value])}");ğ=0;ê=null;Ğ=3;break;}}else{ł.ShowOnHUD
=false;ł.CustomName=ł.CustomName.Replace(" is Learning","");}}if(Ğ>0)Ğ--;ğ++;if(â)continue;Ò=7;Ô=0;Ó=
"Updating Item Database";yield return true;foreach(var ª in Ħ){if(Ç.ContainsKey(ª.ň)){Ç[ª.ň].ŏ=ª.ŏ;Ç[ª.ň].Ō=ª.Ō;if(ª.ť!=null)Ç[ª.ň].ť=ª.ť;Ÿ(
$"Item \"{ª.ŏ}\" overridden from external source.");}else{Ç.Add(ª.ň,ª);Ÿ($"Item \"{ª.ŏ}\" imported from external source.");}}Ħ.Clear();yield return true;foreach(var ª in
ĥ){if(Ç.ContainsKey(ª.ň)){if(Ç[ª.ň].ť==null&&ª.ť!=null){Ç[ª.ň].ť=ª.ť;Ÿ(
$"Item \"{ª.ŏ}\" Blueprint learned from external source.");}}else{Ç.Add(ª.ň,ª);Ÿ($"Item \"{ª.ŏ}\" imported from external source.");}}ĥ.Clear();yield return true;foreach(var Ŕ in
t){if(Ŕ==null)continue;z.Clear();Â=Ŕ.GetInventory();if(Ŕ is IMyProductionBlock)Â=((IMyProductionBlock)Ŕ).OutputInventory;
z.Clear();Â.GetItems(z);foreach(var ª in z){if(!Ç.ContainsKey(ª.Type)){Ì=new V(ª.Type);if(!Ì.Ň)continue;Ÿ(
$"Learning new Item {Ì.ŏ}({Ì.Ō}) from Container {Ŕ.CustomName}");Ç.Add(ª.Type,Ì);if(!Í.Contains(Ç[ª.Type].Ō))Í.Add(Ç[ª.Type].Ō);if(Ç[ª.Type].ŉ()&&!è.ź(Ç[ª.Type]))è.Ŋ(Ç[ª.Type],Ç);}
yield return true;}}Ò=8;Ô=0;Ó="Writing Item Index";Æ="";Ä.Clear();i=Ĵ(Me,out ą);Ä.Add("@GOAT-Inventory-Manager");Ä.Add(
$"@GSIM-GridPriotity={i}");Ä.Add($"~Name~Category~TypeId~SubTypeId~Blueprint~");foreach(var ª in Ë){if(ª.Ō!=Æ){Ä.Add($"\n#### {ª.Ō} ####\n");Æ=ª.
Ō;}Ä.Add(ª.ToString());ª.ſ=0;}Ä.Add($"\n#### Internals ####\n");foreach(var ª in È){Ä.Add(ª.Value.ToString());ª.Value.ſ=0
;}Ä.Add($"\n#### Ore Yields ####\n");Ä.Add(è.ToString());if(Me.CustomData==Å||Å=="")Me.CustomData=string.Join("\n",Ä);Ò=9
;Ô=0;Ó="Sorting Items";Ú=0;Ī=t.Count;ĩ=0;foreach(var Ŕ in t){ĩ++;if(Ŕ==null)continue;if(Ŕ is IMyParachute)continue;if(Ŕ
is IMyUserControllableGun)continue;if(Ŕ is IMyGasGenerator&&!Į.ś(Ŕ))continue;if(Ŕ is IMyReactor&&!Į.ś(Ŕ))continue;if(Ŕ.
CustomName.Ĺ(ITS))continue;Â=Ŕ.GetInventory(0);Ô=1;if(Ŕ is IMyProductionBlock)Â=((IMyProductionBlock)Ŕ).OutputInventory;if(Ŕ is
IMyAssembler&&(Ŕ as IMyAssembler).Mode==MyAssemblerMode.Disassembly)Â=((IMyAssembler)Ŕ).InputInventory;Ô=2;Ú++;if(Ú%VTEC==0)yield
return true;if(Â==null)continue;if(Į.ś(Ŕ)&&!Į.ŕ(Ŕ).ƀ)continue;for(int Ļ=Â.ItemCount-1;Ļ>=0;Ļ--){À=0;Á=0;Ú++;if(Ú%VTEC==0)yield
return true;ª=Â.GetItemAt(Ļ);if(!ª.HasValue)continue;µ=ª.Value;Ô=3;if(!Ç.ContainsKey(µ.Type))continue;Ì=Ç[µ.Type];º=ª.Value.
Amount;Ô=4;foreach(var Ƃ in Į.Ɓ){if(Ƃ==null)continue;Ô=5;Ú++;if(Ú%(VTEC/2)==0)yield return true;if(!Ƃ.ƃ(Â,µ.Type))continue;var
Ƅ=false;À=Ƃ.ƅ(µ.Type,µ.Amount,ref Ƅ);if(À<=0)continue;Ô=6;º=À;if(Į.ś(Ŕ)){Á=Į.ŕ(Ŕ).Ɔ(µ.Type,º,Ƃ.i);if(Á==0)continue;if(Ƅ&&
Ƃ.i<=Į.ŕ(Ŕ).i)Ƅ=false;}if(Á>0){º=(MyFixedPoint)Math.Min((double)Á,(double)À);}if(Ƅ&&Á==0){if(Ƃ.Ƈ(Â,Ļ)){Ÿ(
$"Moved {Ì.ŏ}({Ì.Ō}) from {Ŕ.CustomName} to {Ƃ.ŏ}");}}else{if(Ƃ.Ƈ(Â,Ļ,º)){ƈ(º,Ì.ŏ,Ì.Ō,Ŕ.CustomName,Ƃ.ŏ);}}}if(Į.ś(Ŕ)){À=Į.ŕ(Ŕ).Ɔ(µ.Type,º);if(À<=0)continue;º=À;Á=À;}Ô=7;
if(!o.ContainsKey(Ì.Ō))continue;foreach(var Ɖ in o[Ì.Ō]){Ú++;if(Ú%VTEC==0)yield return true;if(Ɖ==null)continue;Ô=8;if(n.
ContainsKey(Ŕ)&&n[Ŕ].ŗ.Contains(Ì.Ō)&&n[Ŕ].i>=Ɖ.i)continue;Ã=Ɖ.Â;if(Ã==null)continue;if(!Â.CanTransferItemTo(Ã,µ.Type))continue;if(
!Ã.CanItemsBeAdded(1,µ.Type))continue;Ô=9;if(Á>0){if(Â.TransferItemTo(Ã,Ļ,null,true,º)){Ÿ(
$"Moved {º}x{Ì.ŏ}({Ì.Ō}) from {Ŕ.CustomName} to {Ɖ.Ŕ.CustomName}");break;}}else{if(Â.TransferItemTo(Ã,Ļ,null,true)){Ÿ($"Moved {Ì.ŏ}({Ì.Ō}) from {Ŕ.CustomName} to {Ɖ.Ŕ.CustomName}");
break;}}}}}yield return true;foreach(var Ƃ in Į.Ɓ){if(Ƃ.Ɗ()){Ÿ($"Balancing {Ƃ.ŏ}");}}foreach(var Ƌ in Ï){if(RBTC&&Ƌ.Value.
Count>1){X.ƌ(Ƌ.Value);Ÿ($"Balancing {Ƌ.Key} Containers");yield return true;}}Ò=10;Ô=0;Ó="Updating Item Counts";foreach(var ƍ
in È){ƍ.Value.ſ=0;ƍ.Value.Ǝ=0;}foreach(var ª in Ç){Ç[ª.Key].ſ=0;Ç[ª.Key].Ə=0;Ç[ª.Key].Ɛ=0;}Ò=11;Ô=0;Ó=
"Updating ordered Items";foreach(var Ƒ in Ü){if(Ƒ==null)continue;if(!Ƒ.IsWorking)continue;ß.Clear();Ƒ.GetQueue(ß);foreach(var ƒ in ß){foreach(
var ª in Ç){if(!ª.Value.ť.HasValue||ª.Value.ť.Value!=ƒ.BlueprintId)continue;if(Ƒ is IMyAssembler&&(Ƒ as IMyAssembler).Mode
==MyAssemblerMode.Disassembly){}else{}}}yield return true;}foreach(var Ŕ in t){if(Ŕ==null)continue;if(Ŕ.CustomName.Ĺ(HTS))
continue;ĝ=false;foreach(var Ɠ in p){if(Ɠ.Key.Ŕ==Ŕ){ĝ=true;break;}}if(STOR&&Į.ś(Ŕ))ĝ=true;z.Clear();Â=Ŕ.GetInventory();if(Â==
null)continue;Â.GetItems(z);foreach(var ª in z){if(!Ç.ContainsKey(ª.Type))continue;Ç[ª.Type].N((int)ª.Amount);if(ĝ||Ŕ is
IMyRefinery){Ç[ª.Type].Ɣ((int)ª.Amount);}if(Ŕ is IMyRefinery&&!Ŕ.CustomName.Ĺ(MTS)&&(Ŕ as IMyRefinery).IsWorking){è.ƕ(ª,Ç,Ŕ as
IMyRefinery);}yield return true;}if(Ŕ is IMyRefinery){z.Clear();Â=(Ŕ as IMyRefinery).OutputInventory;if(Â==null)continue;Â.GetItems
(z);foreach(var ª in z){if(!Ç.ContainsKey(ª.Type))continue;Ç[ª.Type].N((int)ª.Amount);yield return true;}}else if(Ŕ is
IMyAssembler){Â=(Ŕ as IMyAssembler).OutputInventory;if(Â==null)continue;z.Clear();Â.GetItems(z);foreach(var ª in z){if(!Ç.
ContainsKey(ª.Type))continue;Ç[ª.Type].N((int)ª.Amount);yield return true;}}var Ɩ=Ŕ as IMyGasTank;if(Ɩ==null)continue;if(Ɩ.
GetInventory()==null)continue;foreach(var ƍ in È){if(!(Ɩ.BlockDefinition.SubtypeId.ToLower().Contains(ƍ.Value.Ɨ)||(Ɩ.BlockDefinition
.SubtypeId==""&&ƍ.Value.Ɨ=="oxygen")))continue;ƍ.Value.Ƙ((int)Ɩ.Capacity);ƍ.Value.N((int)(Ɩ.FilledRatio*Ɩ.Capacity));}
yield return true;}foreach(var ƙ in y){È["Electricity"].Ƙ((int)(ƙ?.MaxStoredPower*1000000));È["Electricity"].N((int)(ƙ?.
CurrentStoredPower*1000000));}yield return true;foreach(var ª in Ç){ª.Value.ƚ();}foreach(var ª in È){ª.Value.ƚ();}ý=true;Ò=12;Ô=0;Ó=
"Ordering Items";if(!AC)goto ƛ;á.Clear();foreach(var ª in Ç){if(!ª.Value.ť.HasValue)continue;ø=ª.Value.Ɯ();if(ø>0){á.Add(new à(ª.Value.ť
.Value,ø,ª.Value.i!=0?ª.Value.i*-1f:(ª.Value.ſ/ª.Value.Ǝ),ª.Value));}else if(ø<0){á.Add(new à(ª.Value.ť.Value,ø,ª.Value.i
!=0?ª.Value.i*-1f:(ª.Value.Ǝ/ª.Value.ſ),ª.Value));}}Ô=1;á=á.OrderBy(ş=>ş.i).ToList();if(RCP)á.Reverse();Ô=2;foreach(var Ƒ
in Ü){if(Ƒ==null)continue;if(Ƒ.CustomName.Ĺ(MTS))continue;if(Ƒ is IMyRefinery)continue;Ƒ.ClearQueue();}Ô=3;foreach(var Ɲ
in á){ø=Ɲ.ƞ;ù=Ɲ.ƞ;if(ø>0){Þ.Clear();var Ɵ=0;foreach(var Ƒ in Ü){if(Ƒ==null)continue;if(Ƒ.CustomName.Ĺ(MTS))continue;if(!Ƒ.
IsWorking)continue;if(Ƒ is IMyAssembler&&(Ƒ as IMyAssembler).Mode==MyAssemblerMode.Disassembly)continue;if(!Ƒ.CanUseBlueprint(Ɲ.ť
))continue;if(Ƒ is IMyAssembler&&!Ƒ.BlockDefinition.TypeIdString.ToLower().Contains("survivalkit"))Ɵ++;Þ.Add(Ƒ);}foreach(
var Ƒ in Ü){if(Ƒ==null)continue;if(Ƒ.BlockDefinition.TypeIdString.ToLower().Contains("survivalkit")&&Ɵ>0){Þ.Remove(Ƒ);}}
foreach(var Ƒ in Þ){ú=Math.Min(Math.Min((int)Math.Ceiling(ø/(double)Þ.Count),ø),ù);if(ú<=0)continue;Ƒ.AddQueueItem(Ɲ.ť,(
MyFixedPoint)ú);Ÿ($"Queued {ú}x{Ɲ.Ơ.ŏ} at {Ƒ.CustomName}");Ɲ.Ơ.Ɛ+=ú;ù-=ú;}}else if(ø<0){ø=Math.Abs(ø);ù=Math.Abs(ø);Þ.Clear();
foreach(var Ƒ in Ü){if(Ƒ==null)continue;if(Ƒ.CustomName.Ĺ(MTS))continue;if(!Ƒ.IsWorking)continue;if(Ƒ is IMyAssembler&&(Ƒ as
IMyAssembler).Mode==MyAssemblerMode.Assembly)continue;if(!Ƒ.CanUseBlueprint(Ɲ.ť))continue;Þ.Add(Ƒ);}foreach(var Ƒ in Þ){ú=Math.Min(
Math.Min((int)Math.Ceiling(ø/(double)Þ.Count),ø),ù);if(ú<=0)continue;Ƒ.AddQueueItem(Ɲ.ť,(MyFixedPoint)ú);Ÿ(
$"Queued {ú}x{Ɲ.Ơ.ŏ} at {Ƒ.CustomName}");Ɲ.Ơ.Ɛ-=ú;ù-=ú;}}yield return true;}yield return true;var ơ=false;if(SKI&&F!=null){foreach(var ł in é){if(!ł.
BlockDefinition.TypeIdString.ToLower().Contains("survivalkit")){ơ=true;}}if(!ơ){foreach(var Ƒ in Ü){if(Ƒ==null)continue;if(Ƒ.CustomName
.Ĺ(MTS))continue;if(!Ƒ.IsWorking)continue;if(Ƒ is IMyAssembler&&(Ƒ as IMyAssembler).Mode==MyAssemblerMode.Disassembly)
continue;if(!Ƒ.CanUseBlueprint(F))continue;Ģ.Clear();Ƒ.GetQueue(Ģ);for(int Ļ=0;Ļ<Ģ.Count;Ļ++){if(Ģ[Ļ].BlueprintId!=F)continue;Ƒ.
RemoveQueueItem(Ļ,Ģ[Ļ].Amount);}Ƒ.AddQueueItem(F,1000.0);}}}Ò=13;Ô=0;Ó="Assembler Cleanup";foreach(var ţ in Ý){if(ţ==null)continue;if(ţ
.CustomName.Ĺ(MTS))continue;if(ţ is IMyRefinery)continue;if(ţ is IMyAssembler){(ţ as IMyAssembler).CooperativeMode=false;
}if(!ţ.IsQueueEmpty&&ţ.InputInventory.VolumeFillFactor<0.95f)continue;Â=ţ.InputInventory;if(ţ is IMyAssembler&&(ţ as
IMyAssembler).Mode==MyAssemblerMode.Disassembly){Â=ţ.OutputInventory;}if(Â.ItemCount==0)continue;z.Clear();Â.GetItems(z);for(int Ļ=z
.Count-1;Ļ>=0;Ļ--){º=z[Ļ].Amount;if(!Ç.ContainsKey(z[Ļ].Type))continue;Ì=Ç[z[Ļ].Type];Ť(Â,z[Ļ],ţ.CustomName);yield return
true;}yield return true;}ƛ:if(!RH)goto Ƣ;Ò=14;Ô=0;Ó="Refinery Handling";ď.Clear();đ=0;Ē=0;ē=false;Ĕ.Clear();Ě=false;foreach(
var ª in Ç){if(!ª.Value.ƣ)continue;if(ª.Value.Ǝ==0)continue;ď.Add(ª.Value,ª.Value.Ə/(float)ª.Value.Ǝ);}Ô=1;yield return
true;if(RSF){foreach(var ª in Ç){if(!ª.Value.Ƥ())continue;if(ª.Value.Ə==0)continue;đ=1000000f;Đ=1f;Ĕ.Add(ª.Value);}yield
return true;}Ô=2;ƥ();if(ď.Count>0){var Ʀ=ď.OrderBy(ş=>ş.Value).ToList();foreach(var Ƨ in Ʀ){đ=Ƨ.Key.Ǝ-Ƨ.Key.ſ-Ƨ.Key.Ɛ;Ē=Ƨ.Key.
Ǝ-Ƨ.Key.ſ;if(đ<=0f)continue;if(Ƨ.Key.Ɛ>0f)ē=true;Ì=è.ƨ(Ƨ.Key,out Đ,Ç);if(Ì!=null){Ĕ.Add(Ì);break;}}}yield return true;Ô=3
;if(Ĕ.Count==0&&!ē){Ʃ("Refining Mode: Auto");Ʃ("Ore List:");Ĕ=è.ƪ(Ç);}else{Ʃ("Refining Mode: Quota");Ʃ("Ore List:");
foreach(var ª in Ĕ){Ʃ(ª.ŏ);}}yield return true;if(Ĕ.Count==0)goto Ƣ;Ô=4;Ė=false;ė=0;Ę=0;foreach(var ł in é){if(ł==null)continue
;if(!ł.IsWorking)continue;if(ł.CustomName.Ĺ(MTS))continue;Ę++;Â=ł.InputInventory;if(Â.ItemCount==0)continue;z.Clear();Â.
GetItems(z);if(Â.ContainItems(1,Ĕ[0].ň)&&z[0].Type!=Ĕ[0].ň)Ě=true;if(ę!=null&&Ĕ[0].ň==ę.Value&&!Ě)continue;Ô=5;for(int Ļ=z.Count
-1;Ļ>=0;Ļ--){º=z[Ļ].Amount;if(!Ç.ContainsKey(z[Ļ].Type))continue;Ì=Ç[z[Ļ].Type];Ť(Â,z[Ļ],ł.CustomName);yield return true;
}yield return true;}for(int Ļ=0;Ļ<10;Ļ++){yield return true;}Ô=6;ę=Ĕ[0].ň;ĭ.Clear();if(STOR){foreach(var Ƭ in Į.ƫ){if(Ƭ.
Key==null)continue;ĭ.Add(Ƭ.Key);}}foreach(var Ŕ in p){if(Ŕ.Key==null)continue;if(Ŕ.Key.Ŕ.CustomName.Ĺ(ITS))continue;ĭ.Add(Ŕ
.Key.Ŕ);}foreach(var ƭ in Ĕ){foreach(var Ŕ in ĭ){Â=Ŕ.GetInventory();Â.GetItems(z);for(int Ļ=Â.ItemCount-1;Ļ>=0;Ļ--){ª=Â.
GetItemAt(Ļ);if(!ª.HasValue)continue;ĕ=(float)ª.Value.Amount/Ę;if(ª.Value.Type!=ƭ.ň)continue;foreach(var ł in é){if(ł==null)
continue;if(!ł.IsWorking)continue;if(ł.CustomName.Ĺ(MTS))continue;ł.UseConveyorSystem=false;int Ŵ=X.Ź(ł);Ā=(((float)ł.
InputInventory.MaxVolume*0.95f-(float)ł.InputInventory.CurrentVolume)/ª.Value.Type.GetItemInfo().Volume);if(Ā<=0f)continue;if(đ>0){
float Ʈ=100/(float)Ŵ;º=(MyFixedPoint)Math.Min(Math.Min((đ/Đ)*100f*Ʈ,Ā),ĕ*1.001f);}else{º=(MyFixedPoint)Math.Min((ĕ*1.001f),Ā)
;}if(º<=0)continue;Ã=ł.InputInventory;if(!Â.CanTransferItemTo(Ã,ª.Value.Type)&&SCW){Ư(
$"No conveyor connection between \"{Ŕ.CustomName}\" and \"{ł.CustomName}\"");continue;}if(!Ã.CanItemsBeAdded(º,ª.Value.Type))continue;if(đ>0){if(Â.TransferItemTo(Ã,Ļ,0,true,º)){ƈ(º,ƭ.ŏ,ƭ.Ō,Ŕ.
CustomName,ł.CustomName);}}else{if(!Ã.ContainItems(1,ª.Value.Type)&&Ã.ItemCount>=3)continue;if(Â.TransferItemTo(Ã,Ļ,null,true,º)){
ƈ(º,ƭ.ŏ,ƭ.Ō,Ŕ.CustomName,ł.CustomName);Ė=true;}}}}}yield return true;if(Ė)ė++;if(ė>=3)break;}Ƣ:Ò=15;Ô=0;Ó=
"Refilling Bottles";foreach(var Ŕ in Į.ƫ){if(Ŕ.Key==null)continue;if(Ŕ.Key.GetInventory()==null)continue;z.Clear();Ŕ.Key.GetInventory().
GetItems(z);for(int Ļ=z.Count-1;Ļ>=0;Ļ--){Ú++;if(Ú%VTEC==0)yield return true;if(z[Ļ]==null)continue;if(!Ç.ContainsKey(z[Ļ].Type)
)continue;Ì=Ç[z[Ļ].Type];if(!Ì.ư()&&!Ì.Ʊ())continue;foreach(var Ɩ in x){if(Ɩ==null)continue;if(!Ɩ.HasInventory)continue;Ã
=Ɩ.GetInventory();if(!Ŕ.Key.GetInventory().CanTransferItemTo(Ã,z[Ļ].Type))continue;if(!Ã.CanItemsBeAdded(1,z[Ļ].Type))
continue;if(Ŕ.Key.GetInventory().TransferItemTo(Ã,Ļ,0)){Ɩ.RefillBottles();if(Ã.TransferItemTo(Ŕ.Key.GetInventory(),0)){break;}}}
}yield return true;}Ô=1;foreach(var Ŕ in n){if(Ŕ.Key==null)continue;if(Ŕ.Key.GetInventory()==null)continue;z.Clear();Ŕ.
Value.Â.GetItems(z);for(int Ļ=z.Count-1;Ļ>=0;Ļ--){Ú++;if(Ú%VTEC==0)yield return true;if(z[Ļ]==null)continue;if(!Ç.ContainsKey
(z[Ļ].Type))continue;Ì=Ç[z[Ļ].Type];if(!Ì.ư()&&!Ì.Ʊ())continue;foreach(var Ɩ in x){if(Ɩ==null)continue;if(!Ɩ.HasInventory
)continue;Ã=Ɩ.GetInventory();if(!Ŕ.Value.Â.CanTransferItemTo(Ã,z[Ļ].Type))continue;if(!Ã.CanItemsBeAdded(1,z[Ļ].Type))
continue;if(Ŕ.Value.Â.TransferItemTo(Ã,Ļ,0)){Ɩ.RefillBottles();if(Ã.TransferItemTo(Ŕ.Value.Â,0)){break;}}}}yield return true;}Ò=
16;Ó="Waiting for next loop";while((DateTime.UtcNow-l).TotalSeconds<MinCycle){yield return true;}yield return true;if(!Ġ)
continue;Ò=17;Ó="Remove Not Craftable Blueprints";foreach(var ª in Ç){Ú++;if(Ú%(VTEC*2)==0)yield return true;if(ª.Value.ť==null)
continue;ġ=false;foreach(var Ƒ in Ü){if(Ƒ.CanUseBlueprint(ª.Value.ť.Value)){ġ=true;break;}}if(!ġ)ª.Value.ť=null;}Ġ=false;yield
return true;}}string Ʋ;List<string>Ƴ=new List<string>();IMyTerminalBlock ƴ;IMyInventory Ƶ;float ƶ=1f;int Ʒ=16;int Ƹ=0;bool ƹ=
false;DateTime?ƺ;DateTime?ƻ;string Ƽ;float ƽ;DateTime?ƾ;bool ƿ;int ǀ=0;Vector3 ǁ;Color ǂ;int ǃ=0;int Ǆ=0;List<IMyTextPanel>ǅ;
List<IMyTextPanel>ǆ;List<V>Ǉ;List<Į>ǈ=new List<Į>();IEnumerator<bool>g(){while(true){ǀ++;Ǆ=0;ǅ=k.FindAll(ş=>true);yield
return true;ǆ=î.FindAll(ş=>true);yield return true;foreach(var ǉ in ǅ){Ǆ=1;ǉ.ContentType=ContentType.SCRIPT;ó=(IMyTextSurface)
ǉ;ô=new RectangleF((ó.TextureSize-ó.SurfaceSize)/2f,ó.SurfaceSize);j.Clear();X.Ǌ(ǉ.CustomData,ref j);var ǋ=ô.Height>ô.
Width;var ǌ=ó.DrawFrame();var Ǎ=new Vector2(16,16)+ô.Position;ƶ=j.ǎ("fontSize").ToSingle(1f);bool Ǐ=j.ǎ("showKnowledgeBase").
ToBoolean(true);bool ǐ=j.ǎ("tutorialCompleted").ToBoolean(false);if(ǐ)Tutorials=false;if(ó.SurfaceSize.X<300||ó.SurfaceSize.Y<300
){R.Ǒ(ó,ǌ,Ǎ,"No Entry");Ǎ.X+=40*ƶ;R.ǒ(ó,ǌ,Ǎ,"Screen to small!");goto Ǔ;}R.ǔ(ó,ǌ,Ǎ,$"GOAT Sorter {G}",ƶ);R.Ǒ(ó,ǌ,new
Vector2(ô.Right-Ʒ-32*ƶ,Ǎ.Y),"Screen_LoadingBar",ƶ*0.8f,Ǖ:(float)((ǀ/10f)%Math.PI*2f));Ǎ.Y+=30*ƶ;if(Tutorials){if(!Ç.ContainsKey
(D))Ç.Add(D,new V(D));if(!Ç.ContainsKey(C))Ç.Add(C,new V(C));if(!Ç.ContainsKey(E))Ç.Add(E,new V(E));if(!è.ź(Ç[C]))è.Ŋ(Ç[C
],Ç);Ă=R.ǖ(ó,ǌ,Ǎ,$"Hello and, again welcome to the GOAT Sorter Inventory Manager!\nWe hope that this brief Tutorial helps you setting up the Script!"
,ƶ);Ǎ.Y+=30*(Ă+1)*ƶ;Ą=false;foreach(var Ŕ in p){if(Ŕ.Value.Contains(Ç[C].Ō))Ą=true;}if(!Ą){R.ǖ(ó,ǌ,Ǎ,$"It looks like your grid doesn't have a Cargo Container for Ores specified. Please Add \"{Ç[C].Ō}\" to the name of a Cargo Container."
,ƶ);goto Ǔ;}Ą=false;foreach(var Ŕ in p){if(Ŕ.Value.Contains(Ç[D].Ō))Ą=true;}if(!Ą){R.ǖ(ó,ǌ,Ǎ,
$"Next We will need a Container for our Ingots. Please add \"{Ç[D].Ō}\" to the name of a Cargo Container.",ƶ);goto Ǔ;}Ą=false;foreach(var Ŕ in p){if(Ŕ.Value.Contains(Ç[E].Ō))Ą=true;}if(!Ą){R.ǖ(ó,ǌ,Ǎ,
$"Lastly we need a Container for crafted Components. Please add \"{Ç[E].Ō}\" to the name of a Cargo Container.",ƶ);goto Ǔ;}if(AC){if(Ü.Count==0){R.ǖ(ó,ǌ,Ǎ,
$"GOAT Sorter can automatically craft items for you. Please Build an Assembler.",ƶ);goto Ǔ;}if(û==null){R.ǖ(ó,ǌ,Ǎ,$"An AutoCrafting Screen is required to configure Auto crafting. Please Build a screen and include \"{ACT[0]}\" in its name."
,ƶ);goto Ǔ;}if(Ç[E].Ǝ==0){R.ǖ(ó,ǌ,Ǎ,$"You can set a target quota for an item by pressing F on the auto crafting screen and change the number after the \"/\". Go ahead and change the quota for {Ç[E].ŏ}"
,ƶ);goto Ǔ;}}if(ƺ==null){ƺ=DateTime.UtcNow;}R.ǖ(ó,ǌ,Ǎ,$"This completes the basic tutorial. For more information please checkout our guide on steam.\n The tutorial will exit in {Math.Floor(30-(DateTime.UtcNow-ƺ.Value).TotalSeconds)} seconds"
,ƶ);if((DateTime.UtcNow-ƺ.Value).TotalSeconds>=29){Tutorials=false;}}else{Ǎ.Y+=15*ƶ;R.ǒ(ó,ǌ,Ǎ,"Inventory Space:",0.5f*ƶ);
Ǎ.Y+=15*ƶ;Ǎ.X=R.Ǘ(ô,16);foreach(var ǘ in Î){R.ǒ(ó,ǌ,Ǎ,ǘ.Key,0.5f*ƶ);Ǎ.X+=ó.SurfaceSize.X/(ǋ?2f:4f);if(ǘ.Value<0f){R.Ǒ(ó,ǌ
,Ǎ,"Construction",0.5f*ƶ);R.ǒ(ó,ǌ,Ǎ+new Vector2(20,0)*ƶ,"No Container",0.5f*ƶ);}else{R.Ǚ(ó,ǌ,Ǎ,ǘ.Value,0.5f*ƶ,ǚ:(ǋ?0.5f:
0.25f));if(ǘ.Value>=0.8&&ǘ.Value<0.95){R.Ǒ(ó,ǌ,Ǎ+new Vector2(-20,0)*ƶ,"Danger",0.5f*ƶ);}else if(ǘ.Value>=0.95){R.Ǒ(ó,ǌ,Ǎ+new
Vector2(-20,0)*ƶ,"No Entry",0.5f*ƶ);}}if(Ǎ.X<ó.SurfaceSize.X/2f-16&&!ǋ){Ǎ.X+=ó.SurfaceSize.X/4f;}else{Ǎ.Y+=15*ƶ;Ǎ.X=R.Ǘ(ô,16);}
}Ǎ.Y+=15*ƶ;if(Ǎ.X>30)Ǎ.Y+=15*ƶ;Ǎ.X=R.Ǘ(ô,16);if(Ĝ.Count>0){R.Ǒ(ó,ǌ,Ǎ,"Danger",ƶ);R.ǔ(ó,ǌ,Ǎ,"     Problems",ƶ);Ǎ.Y+=30*ƶ;
foreach(var Ǜ in Ĝ){Ă=R.ǖ(ó,ǌ,Ǎ,Ǜ,0.5f*ƶ);Ǎ.Y+=15*Ă*ƶ;}}if(Ǐ){R.ǔ(ó,ǌ,Ǎ,"Knowledge Base",0.5f*ƶ);Ǎ.Y+=15*ƶ;if(ƻ==null||(
DateTime.UtcNow-ƻ.Value).TotalSeconds>=30){ƻ=DateTime.UtcNow;Ƽ=ǜ.ǝ();}R.ǖ(ó,ǌ,Ǎ,Ƽ,0.5f*ƶ);}}j.Clear();j.Ǟ("fontSize",ƶ);j.Ǟ(
"showKnowledgeBase",Ǐ);j.Ǟ("tutorialCompleted",true);ǉ.CustomData=j.ǟ();Ǔ:ǌ.Dispose();yield return true;}if(!ý){yield return true;continue;
}Ǆ=2;R.Ǡ();õ.Clear();û=null;Ǉ=Ç.Values.ToList();yield return true;ǃ=0;foreach(var ǡ in ǆ){if(ǡ==null)continue;if(!ĵ.Ŀ(ǡ.
CubeGrid))continue;if(!ǡ.CustomName.Ĺ(ACT))continue;if(ǡ.CustomName.Ĺ(ACE))continue;û=ǡ;ó=(IMyTextSurface)ǡ;õ=û.GetText().Split(
'\n').ToList();foreach(var Â in õ){if(Â.StartsWith("#")||Â.StartsWith("~"))continue;ö=Â.Split(new[]{'~','/'});foreach(var ª
in Ǉ){ǃ++;if(ǃ>5000){yield return true;ǃ=0;}if(ª.ŏ!=ö[0])continue;ª.Ǣ(Â);}}yield return true;}Ǆ=3;õ.Clear();Ʋ="";õ.Add("~ AutoCrafting Settings:\n~ ItemName~~~~<current amount>/<wanted amount>(modifier, optional)\n~ Available Modifiers, for Explanation and Examples READ THE GUIDE!!!\n~ A = Craft Items, Default behaviour.\n~ D = Disassemble Items.\n~ H = Hide Items from the AutoCrafting Display (but not settings)\n~ [Y:<number>] = defines yield per craft.\n~ [P:<number>] = defines a manual Crafting Priority.\n~ Modifiers can be combined.\n~ More Settings in the Custom Data!\n~ *Note about Ingot Quotas, these are optional, you can leave them at 0 and let the Script Refine everything as needed!"
);foreach(var ª in Ë){if(!ª.ť.HasValue&&!ª.ƣ)continue;if(ª.Ō!=Ʋ){õ.Add($"\n~~~~~~~~ {ª.Ō} ~~~~~~~~");Ʋ=ª.Ō;}õ.Add(ª.ǣ());
}Ǆ=4;if(û!=null)û.WriteText(string.Join("\n",õ));ü.Clear();foreach(var ǡ in ǆ){if(ǡ==null)continue;if(!ĵ.Ŀ(ǡ.CubeGrid))
continue;if(ǡ.CustomName.Ĺ(ACT)&&!ǡ.CustomName.Ĺ(ACE)){ü.Add(ǡ);break;}}Ǆ=5;yield return true;foreach(var ǡ in ǆ){if(ǡ==null)
continue;if(!ĵ.Ŀ(ǡ.CubeGrid))continue;if(ǡ.CustomName.Ĺ(ACE)){ü.Add(ǡ);}}Ǆ=6;yield return true;ü=ü.OrderBy((Ǥ)=>{if(Ǥ.CustomName
.Ĺ(ACT)&&!Ǥ.CustomName.Ĺ(ACE))return-1;int ǥ=0;foreach(var Ǧ in ACE){if(!Ǥ.CustomName.Contains(Ǧ))continue;int.TryParse(Ǥ
.CustomName.Substring(Ǥ.CustomName.IndexOf(Ǧ,StringComparison.Ordinal)+Ǧ.Length,1),out ǥ);}return ǥ;}).ToList();yield
return true;int ǧ=0;int Ǩ=0;ǃ=0;Ǆ=7;foreach(var ǡ in ü){Ǆ=8;if(ǡ==null)continue;ó=(IMyTextSurface)ǡ;ó.ContentType=ContentType.
SCRIPT;ó.Script="";ô=new RectangleF((ó.TextureSize-ó.SurfaceSize)/2f,ó.SurfaceSize);var ǌ=ó.DrawFrame();var Ǎ=ô.Position;bool
ǩ=false;Ʋ="";int Ǫ=ǧ;ǧ=0;bool ǫ=false;if(Ǩ==0){X.Ǌ(ǡ.CustomData,ref j);Ǫ=j.ǎ("More").ToInt32(0);ǫ=j.ǎ("ScrollingEnabled")
.ToBoolean(true);ƶ=j.ǎ("FontSize").ToSingle(1f);Ʒ=j.ǎ("Margin").ToInt32(16);ƽ=j.ǎ("MinScrollWait").ToSingle(8f);ƿ=j.ǎ(
"ColoredIcons").ToBoolean(false);if(ü.Count>1){Ǫ=0;ǫ=false;}}Ǎ.X+=Ʒ;Ǎ.Y+=Ʒ;foreach(var ª in Ë){Ǆ=9;ǃ++;if(ǃ>25){yield return true;ǃ=0;
}if(!ª.ť.HasValue&&!ª.ƣ)continue;if(ª.Ǭ)continue;if(ǧ<Ǫ){ǧ++;continue;}if(Ǎ.Y+((Ʒ*2)+(ª.Ō!=Ʋ?60:30))*ƶ>ô.Height){if(Ǩ==0
&&(ƾ==null||(DateTime.UtcNow-ƾ.Value).TotalSeconds>=ƽ)){j.Clear();j.Ǟ("More",ǫ?ǧ:0);j.Ǟ("ScrollingEnabled",ǫ);j.Ǟ(
"FontSize",ƶ);j.Ǟ("Margin",Ʒ);j.Ǟ("MinScrollWait",ƽ);j.Ǟ("ColoredIcons",ƿ);ǡ.CustomData=j.ǟ();ƾ=DateTime.UtcNow;}ǩ=true;break;}if(
ª.Ō!=Ʋ){R.ǔ(ó,ǌ,Ǎ,ª.Ō,ƶ,Ʒ);Ǎ=new Vector2(R.Ǘ(ô,Ʒ),Ǎ.Y+30*ƶ);Ʋ=ª.Ō;}R.ǭ(ó,ǌ,Ǎ,ª,ƶ,Ʒ,ô,ƿ);Ǎ=new Vector2(R.Ǘ(ô,Ʒ),Ǎ.Y+30*ƶ);
ǧ++;}if(!ǩ&&Ǩ==0&&(ƾ==null||(DateTime.UtcNow-ƾ.Value).TotalSeconds>=ƽ)){j.Clear();j.Ǟ("More",0);j.Ǟ("ScrollingEnabled",ǫ)
;j.Ǟ("FontSize",ƶ);j.Ǟ("Margin",Ʒ);j.Ǟ("MinScrollWait",ƽ);j.Ǟ("ColoredIcons",ƿ);ǡ.CustomData=j.ǟ();ƾ=DateTime.UtcNow;}ǌ.
Dispose();Ǩ++;yield return true;}ñ.Clear();ð.Clear();ò.Clear();Ǆ=10;foreach(var ǡ in ǆ){if(ǡ==null)continue;if(ǡ.CustomName.Ĺ(
CST)){ñ.Add(ǡ,ǡ);}if(ǡ.CustomName.Ĺ(IST)){ð.Add(ǡ,ǡ);}}Ǆ=11;yield return true;foreach(var Ǯ in ï){if(Ǯ.Key==null)continue;Ƴ
=Ǯ.Value.CustomData.Split('@').ToList();foreach(var ª in Ƴ){if(ª.Contains(" GOAT-Level")){int ǯ=-1;int.TryParse(ª.Split(
'G')[0].Trim(),out ǯ);if(ǯ>=0&&Ǯ.Key.SurfaceCount-1>=ǯ&&!ñ.ContainsKey(Ǯ.Key.GetSurface(ǯ))){ñ.Add(Ǯ.Key.GetSurface(ǯ),Ǯ.
Value);}}if(ª.Contains(" GOAT-Inventory")){int ǯ=-1;int.TryParse(ª.Split('G')[0].Trim(),out ǯ);if(ǯ>=0&&Ǯ.Key.SurfaceCount-1
>=ǯ&&!ð.ContainsKey(Ǯ.Key.GetSurface(ǯ))){ð.Add(Ǯ.Key.GetSurface(ǯ),Ǯ.Value);ò.Add(Ǯ.Key.GetSurface(ǯ),ª);}}}}Ǆ=12;foreach
(var ó in ñ){if(ó.Key==null)continue;ó.Key.ContentType=ContentType.SCRIPT;ó.Key.Script="";ƴ=null;if(ó.Key.GetText()==""){
Ā=999999f;foreach(var Ŕ in t){ā=Vector3.RectangularDistance(ó.Value.Position,Ŕ.Position);if(ā<Ā){ƴ=Ŕ;Ā=ā;}}ĉ="SteelPlate"
;Ċ="";č=5;ċ="White";Č=ƴ.CustomName;if(ƴ is IMyGasTank){ĉ=ƴ.BlockDefinition.SubtypeId.Contains("Hydro")?"IconHydrogen":
"IconOxygen";}else if(ƴ.HasInventory){var Â=ƴ.GetInventory();if(Â.ItemCount>0&&Ç.ContainsKey(Â.GetItemAt(0).Value.Type)){ĉ=Ç[Â.
GetItemAt(0).Value.Type].ŏ;}}j.Clear();j.Ǟ("ContainerID",ƴ.EntityId.ToString());j.Ǟ("OffsetX",ó.Value.Position.X-ƴ.Position.X);j.
Ǟ("OffsetY",ó.Value.Position.Y-ƴ.Position.Y);j.Ǟ("OffsetZ",ó.Value.Position.Z-ƴ.Position.Z);j.Ǟ("Name",Č);j.Ǟ("Icon",ĉ);j
.Ǟ("Icon2",Ċ);j.Ǟ("Style",č);j.Ǟ("Font",ċ);j.Ǟ("FindContainerByName","");ó.Key.WriteText(j.ǟ());ó.Key.
ScriptBackgroundColor=Color.Black;ó.Key.ScriptForegroundColor=Color.White;}else{X.Ǌ(ó.Key.GetText(),ref j);ƴ=GridTerminalSystem.
GetBlockWithId(j.ǎ("ContainerID").ToInt64(0));if(ƴ==null){ǁ=Vector3.Zero;ǁ.X=j.ǎ("OffsetX").ToSingle(0f);ǁ.Y=j.ǎ("OffsetY").ToSingle(
0f);ǁ.Z=j.ǎ("OffsetZ").ToSingle(0f);Ā=999999f;foreach(var Ŕ in t){ā=Vector3.RectangularDistance(ó.Value.Position+ǁ,Ŕ.
Position);if(ā<Ā){ƴ=Ŕ;Ā=ā;}}j.Ǟ("ContainerID",ƴ.EntityId.ToString());ó.Key.WriteText(j.ǟ());}Č=j.ǎ("Name").ToString("Name");ĉ=j.
ǎ("Icon").ToString("SteelPlate");Ċ=j.ǎ("Icon2").ToString("");č=j.ǎ("Style").ToInt32(1);ċ=j.ǎ("Font").ToString("White");þ=
j.ǎ("FindContainerByName").ToString("");if(þ!=""){ƴ=GridTerminalSystem.GetBlockWithName(þ);if(ƴ!=null){j.Ǟ("ContainerID",
ƴ.EntityId.ToString());j.Ǟ("OffsetX",ó.Value.Position.X-ƴ.Position.X);j.Ǟ("OffsetY",ó.Value.Position.Y-ƴ.Position.Y);j.Ǟ(
"OffsetZ",ó.Value.Position.Z-ƴ.Position.Z);j.Ǟ("FindContainerByName","");ó.Key.WriteText(j.ǟ());}}}if(ƴ==null)continue;if(ƴ is
IMyGasTank){ć=(float)(ƴ as IMyGasTank).FilledRatio;Ĉ=1f-ć;}else{Ƶ=ƴ.GetInventory();ć=Ƶ.VolumeFillFactor;Ĉ=ć;}ô=new RectangleF((ó.
Key.TextureSize-ó.Key.SurfaceSize)/2f,ó.Key.SurfaceSize);var ǌ=ó.Key.DrawFrame();var Ǎ=ô.Position;MySprite ǰ;if(Ċ=="")Ċ=ĉ;Ǳ
(ƴ,out Ď);if(Ď.BuildLevelRatio<1f){ĉ=Ċ="Danger";}if(!ƴ.IsFunctional){ĉ=Ċ="Cross";}switch(č){case 1:case 2:case 3:ǰ=new
MySprite(){Type=SpriteType.TEXT,Data=Č,Position=Ǎ+new Vector2(ó.Key.SurfaceSize.X/2,0),RotationOrScale=1.5f,Color=(č!=3?ó.Key.
ScriptForegroundColor:R.ǲ(Ĉ)).Shade(R.ǳ),Alignment=TextAlignment.CENTER,FontId=ċ};ǌ.Add(ǰ);Ǎ=new Vector2(4,32);ǰ=new MySprite(){Type=
SpriteType.TEXTURE,Data=Ê.ContainsKey(ĉ)?Ê[ĉ].Ǵ():ĉ,Position=Ǎ+new Vector2(16,16),Size=new Vector2(32,32),Color=ó.Key.
ScriptForegroundColor.Shade(R.ǳ),Alignment=TextAlignment.CENTER};ǌ.Add(ǰ);Ǎ=new Vector2(ó.Key.SurfaceSize.X-32-4,32);ǰ=new MySprite(){Type=
SpriteType.TEXTURE,Data=Ê.ContainsKey(Ċ)?Ê[Ċ].Ǵ():Ċ,Position=Ǎ+new Vector2(16,16),Size=new Vector2(32,32),Color=ó.Key.
ScriptForegroundColor.Shade(R.ǳ),Alignment=TextAlignment.CENTER};ǌ.Add(ǰ);Ǎ=new Vector2(10,84);ǰ=new MySprite(){Type=SpriteType.TEXTURE,Data=
"SquareSimple",Position=Ǎ,Size=new Vector2(ó.Key.SurfaceSize.X-20,20),Color=ó.Key.ScriptForegroundColor.Shade(0.25f).Shade(R.ǳ),
Alignment=TextAlignment.LEFT};ǌ.Add(ǰ);ǰ=new MySprite(){Type=SpriteType.TEXTURE,Data="SquareSimple",Position=Ǎ,Size=new Vector2(ć
*(ó.Key.SurfaceSize.X-20),20),Color=(č==1?ó.Key.ScriptForegroundColor:R.ǲ(Ĉ)).Shade(R.ǳ),Alignment=TextAlignment.LEFT};ǌ.
Add(ǰ);break;case 4:case 5:case 6:ǰ=new MySprite(){Type=SpriteType.TEXT,Data=Č,Position=Ǎ+new Vector2(ó.Key.SurfaceSize.X/2
,0),RotationOrScale=1.5f,Color=(č!=6?ó.Key.ScriptForegroundColor:R.ǲ(Ĉ)).Shade(R.ǳ),Alignment=TextAlignment.CENTER,FontId
=ċ};ǌ.Add(ǰ);Ǎ=new Vector2(4,32);ǰ=new MySprite(){Type=SpriteType.TEXTURE,Data=Ê.ContainsKey(ĉ)?Ê[ĉ].Ǵ():ĉ,Position=Ǎ+new
Vector2(32,32),Size=new Vector2(64,64),Color=ó.Key.ScriptForegroundColor.Shade(R.ǳ),Alignment=TextAlignment.CENTER};ǌ.Add(ǰ);Ǎ=
new Vector2(ó.Key.SurfaceSize.X-64-4,32);ǰ=new MySprite(){Type=SpriteType.TEXTURE,Data=Ê.ContainsKey(Ċ)?Ê[Ċ].Ǵ():Ċ,Position
=Ǎ+new Vector2(32,32),Size=new Vector2(64,64),Color=ó.Key.ScriptForegroundColor.Shade(R.ǳ),Alignment=TextAlignment.CENTER
};ǌ.Add(ǰ);Ǎ=new Vector2(10+64,84);ǰ=new MySprite(){Type=SpriteType.TEXTURE,Data="SquareSimple",Position=Ǎ,Size=new
Vector2(ó.Key.SurfaceSize.X-20-128,20),Color=ó.Key.ScriptForegroundColor.Shade(0.25f).Shade(R.ǳ),Alignment=TextAlignment.LEFT};
ǌ.Add(ǰ);ǰ=new MySprite(){Type=SpriteType.TEXTURE,Data="SquareSimple",Position=Ǎ,Size=new Vector2(ć*(ó.Key.SurfaceSize.X-
20-128),20),Color=(č==4?ó.Key.ScriptForegroundColor:R.ǲ(Ĉ)).Shade(R.ǳ),Alignment=TextAlignment.LEFT};ǌ.Add(ǰ);break;}ǌ.
Dispose();yield return true;}Ǆ=13;foreach(var ó in ð){if(ó.Key==null)continue;ó.Key.ContentType=ContentType.SCRIPT;ó.Key.Script
="";ô=new RectangleF((ó.Key.TextureSize-ó.Key.SurfaceSize)/2f,ó.Key.SurfaceSize);if(ò.ContainsKey(ó.Key)){ÿ=ò[ó.Key].
Split('\n');}else if(ó.Key.GetText()!=""){ÿ=ó.Key.GetText().Split('\n');}else{ÿ=Array.Empty<string>();}Ʒ=16;Ƹ=0;var ǌ=ó.Key.
DrawFrame();var Ǎ=ô.Position;ƶ=1f;ƹ=false;foreach(var Ƨ in ÿ){if(Ƨ.StartsWith("@"))continue;Ƹ++;X.ǵ(Ƨ,ref j);if(Ƹ==1){Ʒ=j.ǎ(
"Margin").ToInt32(16);Ǎ.X+=Ʒ;Ǎ.Y+=Ʒ;}if(Ƨ.StartsWith("Size=")){ƶ=j.ǎ("Size").ToSingle(1f);continue;}if(Ƨ.StartsWith("HideEmpty")
){ƹ=true;}if(Ƨ.StartsWith("ShowEmpty")){ƹ=false;}if(Ƨ.StartsWith("Header=")){þ=j.ǎ("Header").ToString("");if(þ==
"#GridName")þ=Me.CubeGrid.CustomName;R.ǔ(ó.Key,ǌ,Ǎ,þ,ƶ,Ʒ);goto Ƕ;}if(Ƨ.StartsWith("Category=")){Ă=0;ă=j.ǎ("Offset").ToInt32(0);þ=j.
ǎ("Category").ToString("");foreach(var ª in Ê){if(þ!=ª.Value.Ō)continue;if(ƹ&&ª.Value.ſ<=0f)continue;if(Ă<ă){Ă++;continue
;}if(Ǎ.Y+30*ƶ>(ô.Height+ô.Position.Y))break;R.Ƿ(ó.Key,ǌ,Ǎ,ª.Value,ƶ,Ʒ,ô);Ǎ=new Vector2(R.Ǘ(ô,Ʒ),Ǎ.Y+30*ƶ);}continue;}if(Ƨ
.StartsWith("ShortItem=")){þ=j.ǎ("ShortItem").ToString("");foreach(var ª in Ê){if(þ!=ª.Value.ŏ)continue;R.Ǹ(ó.Key,ǌ,Ǎ,ª.
Value,ƶ,Ʒ,ô);goto Ƕ;}continue;}if(Ƨ.StartsWith("Space")){þ=Ƨ.Replace("Space=","");Ă=0;if(!Int32.TryParse(þ,out Ă)){Ă=1;}for(
int Ļ=0;Ļ<Ă;Ļ++){Ǎ=new Vector2(R.Ǘ(ô,Ʒ),Ǎ.Y+30*ƶ);}continue;}if(Ƨ.StartsWith("IconBar=")){þ=j.ǎ("IconBar").ToString("");Ă=j
.ǎ("Target").ToInt32(0);foreach(var ª in Ê){if(þ!=ª.Value.ŏ)continue;if(Ă==0){R.ǹ(ó.Key,ǌ,Ǎ,ª.Value.Ǵ(),ª.Value.Ǻ(),ƶ,Ʒ);
}else{R.ǹ(ó.Key,ǌ,Ǎ,ª.Value.Ǵ(),Math.Min(Math.Max(ª.Value.ſ/Ă,0f),1f),ƶ,Ʒ);}goto Ƕ;}continue;}if(Ƨ.StartsWith("Text=")){þ
=j.ǎ("Text").ToString("");if(þ=="#GridName")þ=Me.CubeGrid.CustomName;R.ǒ(ó.Key,ǌ,Ǎ,þ,ƶ);goto Ƕ;}if(Ƨ.StartsWith(
"CategoryLevel=")){þ=j.ǎ("CategoryLevel").ToString("");Ā=0;ā=0;foreach(var Ŕ in p){if(!Ŕ.Value.Contains(þ))continue;Ƶ=Ŕ.Key.Â;Ā+=(float)
Ƶ.CurrentVolume;ā+=(float)Ƶ.MaxVolume;}R.Ǚ(ó.Key,ǌ,Ǎ,Ā/ā,ƶ,Ʒ);goto Ƕ;}if(Ƨ.StartsWith("DeltaItems")||Ƨ.StartsWith(
"Stonks")){Ă=0;ă=j.ǎ("Offset").ToInt32();þ="";foreach(var ª in Ê){if(ª.Value.ǻ==0)continue;if(Ă<ă){Ă++;continue;}if(Ǎ.Y+30*ƶ>ô.
Height)break;R.Ǽ(ó.Key,ǌ,Ǎ,ª.Value,ƶ,Ʒ);goto Ƕ;}continue;}if(Ƨ.StartsWith("Icon=")){if(Ê.ContainsKey(Ƨ.Replace("Icon=",""))){R
.Ǒ(ó.Key,ǌ,Ǎ,Ê[Ƨ.Replace("Icon=","")].Ǵ(),ƶ);}else{R.Ǒ(ó.Key,ǌ,Ǎ,Ƨ.Replace("Icon=",""),ƶ);}goto Ƕ;}if(Ƨ.StartsWith("Push"
)){þ=Ƨ.Replace("Push=","");Ă=0;if(!Int32.TryParse(þ,out Ă)){Ă=1;}Ǎ=new Vector2(Ǎ.X+Ă*ƶ,Ǎ.Y);continue;}if(Ƨ.StartsWith(
"BG")){Ă=j.ǎ("Repeat").ToInt32(1);þ=j.ǎ("BG").ToString("Grid");Ā=j.ǎ("Opacity").ToSingle(1f);ǂ=X.ǽ(j.ǎ("Color").ToString(
"000000"));Ą=j.ǎ("AspectCorrection").ToBoolean(true);R.Ǿ(ó.Key,ǌ,þ,Ă,Ā,ô,ǂ,Ą);continue;}if(Ƨ.StartsWith("StockStats=")){if(Į.ǿ.
ContainsKey(Ƨ.Replace("StockStats=",""))){Į.ǿ[Ƨ.Replace("StockStats=","")].Ȁ(ó.Key,ǌ,Ǎ,ƶ);goto Ƕ;}continue;}if(Ƨ.StartsWith(
"StockLevel=")){þ=j.ǎ("StockLevel").ToString();if(Į.ǿ.ContainsKey(þ)){Į.ǿ[þ].ȁ(ó.Key,ǌ,Ǎ,ƶ,Ʒ,j.ǎ("Color").ToInt32(0)-1);goto Ƕ;}
continue;}if(Ƨ.StartsWith("StockLevelFull=")){þ=j.ǎ("StockLevelFull").ToString();if(Į.ǿ.ContainsKey(þ)){Į.ǿ[þ].Ȃ(ó.Key,ô,ǌ,Ǎ,Ʒ,j
.ǎ("Color").ToInt32(0)-1);goto Ƕ;}continue;}if(Ƨ.StartsWith("StockVolume=")){þ=j.ǎ("StockVolume").ToString();if(Į.ǿ.
ContainsKey(þ)){Į.ǿ[þ].ȃ(ó.Key,ǌ,Ǎ,ƶ,Ʒ,j.ǎ("Color").ToInt32(0)-1);goto Ƕ;}continue;}if(Ƨ.StartsWith("StockVolumeFull=")){þ=j.ǎ(
"StockVolumeFull").ToString();if(Į.ǿ.ContainsKey(þ)){Į.ǿ[þ].Ȅ(ó.Key,ô,ǌ,Ǎ,Ʒ,j.ǎ("Color").ToInt32(0)-1);goto Ƕ;}continue;}if(Ƨ.StartsWith(
"StockList=")){þ=j.ǎ("StockList").ToString();if(Į.ǿ.ContainsKey(þ)){Į.ǿ[þ].ȅ(ó.Key,ô,ǌ,Ǎ,ƶ,Ʒ,Ç);goto Ƕ;}continue;}foreach(var ª in Ê
){if(Ƨ!=ª.Value.ŏ)continue;R.Ƿ(ó.Key,ǌ,Ǎ,ª.Value,ƶ,Ʒ,ô);goto Ƕ;}continue;Ƕ:Ǎ=new Vector2(R.Ǘ(ô,Ʒ),Ǎ.Y+30*ƶ);}ǌ.Dispose();
yield return true;}ǈ.Clear();foreach(var Ƭ in Į.Ɓ){if(Ƭ.Ȇ)continue;ǈ.Add(Ƭ);}foreach(var ȇ in ǈ){ȇ.Ȉ(Ç);}yield return true;}}
bool ȉ;Vector2 Ȋ;Vector2 ȋ;int Ȍ=128;string ȍ="";Random Ȏ=new Random();RectangleF ȏ;void ȕ(){switch(Ð%16){case 0:case 1:case
2:case 3:Ñ="•---";break;case 4:case 5:case 6:case 7:Ñ="-•--";break;case 8:case 9:case 10:case 11:Ñ="--•-";break;case 12:
case 13:case 14:case 15:Ñ="---•";break;}if(Ò==9){Echo($"{Ñ} {Ò}:{Ô}({Ǆ}) {Ó} ({ĩ}/{Ī})");Echo(
$"Current inventory: {t[ĩ-1].CustomName}\n");}else{Echo($"{Ñ} {Ò}:{Ô}({Ǆ}) {Ó}\n");}if(Tutorials&&k.Count==0){Echo(
$"Please build a screen with \"{SST[0]}\" in its name.\n");}Echo($"Instructions: {Runtime.CurrentInstructionCount}/{Runtime.MaxInstructionCount}\n");Echo(
$"Runtime: {Runtime.LastRunTimeMs}\n");if(â&&ä!=null){Echo($"Learning mode active for {Math.Floor(300-(DateTime.UtcNow-ä.Value).TotalSeconds)} seconds");Echo
("DO NOT RECOMPILE!!!");Echo("this will not save newly learned recipes!");Echo(
"to save newly learned recipes, run the same argument again.\n");}foreach(var ǘ in Î){if(ǘ.Value<0f){Echo($"{ǘ.Key}: Warning, no container");}else{Echo($"{ǘ.Key}: {ǘ.Value*100f:F2}%")
;}}if(Ö!=null){Ù=$"{(DateTime.UtcNow.Second%2==0?DateTime.UtcNow.AddHours(UTC).ToString("HH:mm"):DateTime.UtcNow.AddHours(UTC).ToString("HH mm"))} | {Ò.ToString().PadLeft(2,'0')} - {Ó}\n"
;for(int Ļ=Õ.Count-1;Ļ>=0;Ļ--){Ù+=$"{Õ[Ļ].Value.AddHours(UTC):HH:mm:ss} | {Õ[Ļ].Key}";if(Ļ!=0){Ù+="\n";}}Ö.WriteText(Ù);}
if(Ø!=null){Ø.WriteText(string.Join("\n",Û));}var Ȑ=Me as IMyTextSurfaceProvider;var ȑ=Ȑ.GetSurface(0);if(ȑ==null){Ð++;
return;}var ǌ=ȑ.DrawFrame();ȏ=new RectangleF((ȑ.TextureSize-ȑ.SurfaceSize)/2f,ȑ.SurfaceSize);var Ǎ=new Vector2(ȏ.Center.X,ȏ.
Position.Y+16);ȑ.ContentType=ContentType.SCRIPT;R.Ǿ(ȑ,ǌ,"Grid",1,0.1f,ȏ,Color.Black);R.Ȓ(ȑ,ǌ,Ǎ,$"GOAT Sorter {G}",ȏ.Height<=256?
1f:1.5f);R.ȓ(ȑ,ǌ,new Vector2(ȏ.Center.X,ȏ.Center.Y+30),new Vector2(ȏ.Width,ȏ.Height-60),"SquareHollow",Color.Black,Ȕ:0.1f)
;if(!ȉ){Ȋ=ȏ.Center;ȋ=Vector2.One;ȍ="Textures\\FactionLogo\\Builders\\BuilderIcon_1.dds";if(ȏ.Height<=256){Ȍ=48;}ȉ=true;}Ȋ
+=ȋ;if(Ȋ.X+Ȍ/2>ȏ.Right||Ȋ.X-Ȍ/2<ȏ.X){ȋ.X*=-1f;if(Ç.Count>0)ȍ=Ç.ElementAt(Ȏ.Next(0,Ç.Count)).Value.Ǵ();}if(Ȋ.Y+Ȍ/2>ȏ.Bottom
||Ȋ.Y-Ȍ/2<ȏ.Y+60){ȋ.Y*=-1f;if(Ç.Count>0)ȍ=Ç.ElementAt(Ȏ.Next(0,Ç.Count)).Value.Ǵ();}R.ȓ(ȑ,ǌ,Ȋ,new Vector2(Ȍ,Ȍ),ģ?
"Textures\\FactionLogo\\Traders\\TraderIcon_2.dds":ȍ,Color.White);ǌ.Dispose();Ð++;}static void Ÿ(string Ȗ){var ȗ=Ȗ.Split('\n');for(int Ļ=0;Ļ<ȗ.Length;Ļ++){Õ.Add(new
KeyValuePair<string,DateTime>(ȗ[Ļ],DateTime.UtcNow));if(Õ.Count>LL){Õ.RemoveAt(0);}}}static void ƈ(float Ș,string Ų,string Ŗ,string
ș,string Ț){Ÿ($"Moved {Ș}x{Ų}({Ŗ}) from {ș} to {Ț}");}static void ƈ(MyFixedPoint Ș,string Ų,string Ŗ,string ș,string Ț){ƈ
((float)Ș,Ų,Ŗ,ș,Ț);}public static void Ʃ(string ț){Û.Add(ț);}static void ƥ(){Û.Clear();}static void Ư(string Ǜ){ě.Add(Ǜ);
}static void Ĳ(){Ĝ.Clear();foreach(var Ǜ in ě){Ĝ.Add(Ǜ);}ě.Clear();}void Ǳ(IMyTerminalBlock Ȝ,out IMySlimBlock ȝ){ȝ=Ȝ.
CubeGrid.GetCubeBlock(Ȝ.Position);}void ȟ(){if(AT&&Runtime.LastRunTimeMs>ATMS){ħ=(int)(Math.Ceiling(Runtime.LastRunTimeMs/(ATMS*
0.9f))+1);}if(ħ!=0&&Ĩ<ħ){Ĩ++;return;}ħ=0;Ĩ=0;if(d!=null){bool Ȟ=false;try{Ȟ=d.MoveNext();}catch(Exception Ǥ){Ÿ(
$"Error during Main Loop step \"{Ò}:{Ô}\": {Ǥ.Message}");Ư($"Main Loop crashed during step \"{Ò}:{Ô}\" please check {LST[0]}");}if(Ȟ&&!å){ȕ();}else{å=false;d.Dispose();d=e();}
}}void Ƞ(){if(f!=null&&Ò!=10&&Ò!=11&&Ò!=12&&Ò!=1&&Ò!=3&&Ò!=4){bool Ȟ=false;try{Ȟ=f.MoveNext();}catch(Exception Ǥ){Ÿ(
$"Error during Display Loop \"{Ǆ}\"({Ò}:{Ô}): {Ǥ.Message}");Ư($"Display Loop crashed please check {LST[0]}");}if(!Ȟ){f.Dispose();f=g();}}}int ȡ(IMyCubeGrid t){if(t.IsStatic)
return 10;if(t.GridSizeEnum==MyCubeSize.Large)return 5;return 1;}int Ĵ(IMyProgrammableBlock Ȣ,out bool ȣ){var Ȥ=Ȣ.CustomData.
Split(new string[]{"@GSIM-GridPriotity="},StringSplitOptions.None);if(Ȥ.Length==2){Ȥ=Ȥ[1].Split(new string[]{"\n"},
StringSplitOptions.None);if(Ȥ.Length>1){var ȥ=0;int.TryParse(Ȥ[0],out ȥ);if(ȥ>=0){ȣ=true;return ȥ;}}}ȣ=false;if(Ȣ.CustomData.StartsWith(
"@GOAT-Inventory-Manager")){return ȡ(Ȣ.CubeGrid);}return-1;}int ĺ(IMyShipConnector ķ,List<IMyTerminalBlock>Ȧ){int ȧ=-1;bool ȣ=false;foreach(var ľ
in Ȧ){if(ľ.CubeGrid!=ķ.CubeGrid)continue;if(!(ľ is IMyProgrammableBlock))continue;ȧ=Ĵ(ľ as IMyProgrammableBlock,out ȣ);if(
ȣ)break;}return ȧ;}void J(){string Ȩ=Ć.Argument(1);if(Ȩ=="true"){ã=true;}if(Ȩ=="false"){ã=false;ä=null;}if(Ȩ=="toggle"){ã
=!ã;}if(ã)å=true;else ä=null;}void K(){string Ȩ=Ć.Argument(1);if(Ȩ=="true"){æ=true;}if(Ȩ=="false"){æ=false;}if(Ȩ==
"toggle"){æ=!æ;}if(æ){ğ=0;å=true;}else{ê=null;}}void L(){ë=true;Ÿ("Item Index Rebuild has been requested.");}void M(){Ġ=true;Ÿ(
"Removing of not craftable Blueprints has been requested.");}void N(){ȩ();}void O(){ȩ(true);}void P(){ì=true;Ÿ("Item Index Population has been requested.");}void Q(){í=true;Ÿ(
"Item Name Translation has been requested.");}void ȩ(bool Ȫ=false){if(Ć.ArgumentCount!=6)return;var ª=new V(
$"~{Ć.Argument(1)}~{Ć.Argument(2)}~{Ć.Argument(3)}~{Ć.Argument(4)}~{Ć.Argument(5)}~");if(!ª.Ň){Ÿ($"Item definition for \"{Ć.Argument(1)}\" could not be parsed");return;}if(Ȫ){Ħ.Add(ª);}else{ĥ.Add(ª);}Ÿ(
$"Item \"{ª.ŏ}\" import queued from external source.");}void ȫ(){if(ä!=null){if((DateTime.UtcNow-ä.Value).TotalSeconds>300){ã=false;ä=null;}}if(æ&&ğ>5&&ê==null){æ=false;}}
bool Ť(IMyInventory Ȭ,MyInventoryItem ª,string ȭ=""){foreach(var Ŕ in p){if(!Ŕ.Value.Contains(Ç[ª.Type].Ō))continue;Ã=Ŕ.Key.
Â;if(!Ȭ.CanTransferItemTo(Ã,ª.Type))continue;if(!Ã.CanItemsBeAdded(ª.Amount,ª.Type))continue;if(Ȭ.TransferItemTo(Ã,0,null
,true,ª.Amount)){if(ȭ!="")ƈ(ª.Amount,Ç[ª.Type].ŏ,Ç[ª.Type].Ō,ȭ,Ŕ.Key.Ŕ.CustomName);return true;}}return false;}string Œ(
Dictionary<string,V>Ȯ,string Ų){if(!Ȯ.ContainsKey(Ų))return Ų;int Ļ=1;while(Ȯ.ContainsKey($"{Ų}_{Ļ}")){Ļ++;}return$"{Ų}_{Ļ}";}
public void
 Main
(string ȯ,UpdateType Ȱ){if((Ȱ&(UpdateType.Trigger|UpdateType.Terminal))!=0&&Ć.TryParse(ȯ)){Action ȱ;string Ȳ=Ć.Argument(0
);if(Ȳ==null){Echo("No command specified");}else if(I.TryGetValue(Ȳ,out ȱ)){ȱ();}else{Echo($"Unknown command {Ȳ}");}}ȫ();
if((Ȱ&UpdateType.Update1)!=0){ȟ();}if((Ȱ&UpdateType.Update10)!=0){Ƞ();}}
}
public class à{public MyDefinitionId ť;public int ƞ;public float i;public V Ơ;public à(MyDefinitionId ť,int ƞ,float i,V
Ơ){this.ť=ť;this.ƞ=ƞ;this.i=i;this.Ơ=Ơ;}}public class V{public const string ȳ="MyObjectBuilder_";public const string Ű=
"MyObjectBuilder_BlueprintDefinition/";public const string ȴ="Ore";public const string ȵ="Ingot";const string ȶ="Component";const string ȷ="AmmoMagazine";
const string ȸ="OxygenContainerObject";const string ȹ="GasContainerObject";const string Ⱥ="PhysicalGunObject";const string Ȼ=
"PhysicalObject";const string ȼ="ConsumableItem";const string Ƚ="Datapad";const string Ⱦ="SeedItem";const string ȿ="Package";const
string ɀ="Position";const string Ɂ="AngleGrinder";const string ɂ="HandDrill";const string Ƀ="MealPack";const string Ʉ=
"FireworksBox";const string Ʌ="AutomaticRifle";static Dictionary<string,string>Ɇ=new Dictionary<string,string>(){{ȳ+ȴ,"Ores"},{ȳ+ȵ,
"Ingots"},{ȳ+ȶ,"Components"},{ȳ+ȷ,"Ammo"},{ȳ+ȸ,"Tools"},{ȳ+ȹ,"Tools"},{ȳ+Ⱥ,"Tools"},{ȳ+Ȼ,"Tools"},{ȳ+ȼ,"Consumables"},{ȳ+Ƚ,
"Misc"},{ȳ+Ⱦ,"Seeds"},{ȳ+ȿ,"Misc"},};static ɇ Ņ;public static Dictionary<string,ɇ>ń=>Ɉ;static Dictionary<string,ɇ>Ɉ=new
Dictionary<string,ɇ>{{ȳ+ȶ+"/BulletproofGlass",new ɇ("","",Ű+"BulletproofGlass")},{ȳ+ȶ+"/Canvas",new ɇ("","",Ű+ɀ+"0030_Canvas")},{ȳ
+ȶ+"/Computer",new ɇ("","",Ű+"Computer"+ȶ)},{ȳ+ȶ+"/Construction",new ɇ("ConstructionComp","",Ű+"Construction"+ȶ)},{ȳ+ȶ+
"/Detector",new ɇ("DetectorComponent","",Ű+"Detector"+ȶ)},{ȳ+ȶ+"/Display",new ɇ("","",Ű+"Display")},{ȳ+ȶ+"/EngineerPlushie",new ɇ()
},{ȳ+ȶ+"/Explosives",new ɇ("","",Ű+"Explosives"+ȶ)},{ȳ+ȶ+"/Girder",new ɇ("","",Ű+"Girder"+ȶ)},{ȳ+ȶ+"/GravityGenerator",
new ɇ("GravityGenComponent","",Ű+"GravityGenerator"+ȶ)},{ȳ+ȶ+"/InteriorPlate",new ɇ("","",Ű+"InteriorPlate")},{ȳ+ȶ+
"/LargeTube",new ɇ("","",Ű+"LargeTube")},{ȳ+ȶ+"/Medical",new ɇ("MedicalComponent","",Ű+"Medical"+ȶ)},{ȳ+ȶ+"/MetalGrid",new ɇ("","",Ű
+"MetalGrid")},{ȳ+ȶ+"/Motor",new ɇ("","",Ű+"Motor"+ȶ)},{ȳ+ȶ+"/PowerCell",new ɇ("","",Ű+"PowerCell")},{ȳ+ȶ+
"/PrototechCapacitor",new ɇ("","",Ű+"PrototechCapacitor")},{ȳ+ȶ+"/PrototechCircuitry",new ɇ("","",Ű+"PrototechCircuitry")},{ȳ+ȶ+
"/PrototechCoolingUnit",new ɇ("","",Ű+"PrototechCoolingUnit")},{ȳ+ȶ+"/PrototechFrame",new ɇ()},{ȳ+ȶ+"/PrototechMachinery",new ɇ("","",Ű+
"PrototechMachinery")},{ȳ+ȶ+"/PrototechPanel",new ɇ("","",Ű+"PrototechPanel")},{ȳ+ȶ+"/PrototechPropulsionUnit",new ɇ("","",Ű+
"PrototechPropulsionUnit")},{ȳ+ȶ+"/RadioCommunication",new ɇ("RadioCommComponent","",Ű+"RadioCommunication"+ȶ)},{ȳ+ȶ+"/Reactor",new ɇ(
"ReactorComponent","",Ű+"Reactor"+ȶ)},{ȳ+ȶ+"/SabiroidPlushie",new ɇ()},{ȳ+ȶ+"/SmallTube",new ɇ("","",Ű+"SmallTube")},{ȳ+ȶ+"/SolarCell",new
ɇ("","",Ű+"SolarCell")},{ȳ+ȶ+"/SteelPlate",new ɇ("","",Ű+"SteelPlate")},{ȳ+ȶ+"/Superconductor",new ɇ("","",Ű+
"Superconductor")},{ȳ+ȶ+"/Thrust",new ɇ("ThrustComponent","",Ű+"Thrust"+ȶ)},{ȳ+ȶ+"/ZoneChip",new ɇ()},{ȳ+Ⱦ+"/Fruit",new ɇ("FruitSeeds",
"",Ű+ɀ+"0010_Seeds_Fruit")},{ȳ+Ⱦ+"/Grain",new ɇ("GrainSeeds","",Ű+ɀ+"0020_Seeds_Grain")},{ȳ+Ⱦ+"/Mushrooms",new ɇ(
"MushroomSpores","",Ű+ɀ+"0040_Spores_Mushrooms")},{ȳ+Ⱦ+"/Vegetables",new ɇ("VegetableSeeds","",Ű+ɀ+"0030_Seeds_Vegetables")},{ȳ+ȼ+
"/ClangCola",new ɇ("","Foods","")},{ȳ+ȼ+"/CosmicCoffee",new ɇ("","Foods","")},{ȳ+ȼ+"/Fruit",new ɇ("","Ingredients","")},{ȳ+ȼ+
"/InsectMeatCooked",new ɇ("","Foods",Ű+ɀ+"0020_CookSpiderMeat")},{ȳ+ȼ+"/InsectMeatRaw",new ɇ("","Ingredients","")},{ȳ+ȼ+"/MammalMeatCooked"
,new ɇ("","Foods",Ű+ɀ+"0010_CookMammalMeat")},{ȳ+ȼ+"/MammalMeatRaw",new ɇ("","Ingredients","")},{ȳ+ȼ+"/"+Ƀ+"_Burrito",new
ɇ("Burrito","Foods",Ű+ɀ+"0170_"+Ƀ+"_Burrito")},{ȳ+ȼ+"/"+Ƀ+"_Chili",new ɇ("Chili","Foods",Ű+ɀ+"0070_"+Ƀ+"_Chili")},{ȳ+ȼ+
"/"+Ƀ+"_Curry",new ɇ("Curry","Foods",Ű+ɀ+"0120_"+Ƀ+"_Curry")},{ȳ+ȼ+"/"+Ƀ+"_Dumplings",new ɇ("Dumplings","Foods",Ű+ɀ+"0140_"
+Ƀ+"_Dumplings")},{ȳ+ȼ+"/"+Ƀ+"_Flatbread",new ɇ("Flatbread","Foods",Ű+ɀ+"0080_"+Ƀ+"_Flatbread")},{ȳ+ȼ+"/"+Ƀ+
"_FrontierStew",new ɇ("FrontierStew","Foods",Ű+ɀ+"0180_"+Ƀ+"_FrontierStew")},{ȳ+ȼ+"/"+Ƀ+"_FruitBar",new ɇ("FruitBar","Foods",Ű+ɀ+
"0040_"+Ƀ+"_FruitBar")},{ȳ+ȼ+"/"+Ƀ+"_FruitPastry",new ɇ("FruitPastry","Foods",Ű+ɀ+"0100_"+Ƀ+"_FruitPastry")},{ȳ+ȼ+"/"+Ƀ+
"_GardenSlaw",new ɇ("GardenSlaw","Foods",Ű+ɀ+"0050_"+Ƀ+"_GardenSlaw")},{ȳ+ȼ+"/"+Ƀ+"_GreenPellets",new ɇ("GreenPellets","Foods",Ű+ɀ+
"0130_"+Ƀ+"_GreenPellets")},{ȳ+ȼ+"/"+Ƀ+"_KelpCrisp",new ɇ("KelpCrisp","Foods",Ű+ɀ+"0030_"+Ƀ+"_KelpCrisp")},{ȳ+ȼ+"/"+Ƀ+
"_Lasagna",new ɇ("Lasagna","Foods",Ű+ɀ+"0160_"+Ƀ+"_Lasagna")},{ȳ+ȼ+"/"+Ƀ+"_Ramen",new ɇ("Ramen","Foods",Ű+ɀ+"0090_"+Ƀ+"_Ramen")},{
ȳ+ȼ+"/"+Ƀ+"_RedPellets",new ɇ("RedPellets","Foods",Ű+ɀ+"0060_"+Ƀ+"_RedPellets")},{ȳ+ȼ+"/"+Ƀ+"_SearedSabiroid",new ɇ(
"SearedSabiroid","Foods",Ű+ɀ+"0190_"+Ƀ+"_SearedSabiroid")},{ȳ+ȼ+"/"+Ƀ+"_Spaghetti",new ɇ("Spaghetti","Foods",Ű+ɀ+"0150_"+Ƀ+"_Spaghetti")
},{ȳ+ȼ+"/"+Ƀ+"_SteakDinner",new ɇ("SteakDinner","Foods",Ű+ɀ+"0200_"+Ƀ+"_SteakDinner")},{ȳ+ȼ+"/"+Ƀ+"_VeggieBurger",new ɇ(
"VeggieBurger","Foods",Ű+ɀ+"0110_"+Ƀ+"_VeggieBurger")},{ȳ+ȼ+"/Medkit",new ɇ("","",Ű+ɀ+"0021_Medkit")},{ȳ+ȼ+"/Mushrooms",new ɇ("",
"Ingredients","")},{ȳ+ȼ+"/Powerkit",new ɇ("","",Ű+ɀ+"0022_Powerkit")},{ȳ+ȼ+"/Vegetables",new ɇ("","Ingredients","")},{ȳ+ȷ+
"/AutocannonClip",new ɇ("AutocannonMagazine","",Ű+ɀ+"0090_AutocannonClip")},{ȳ+ȷ+"/LargeCalibreAmmo",new ɇ("ArtilleryShell","",Ű+ɀ+
"0120_LargeCalibreAmmo")},{ȳ+ȷ+"/MediumCalibreAmmo",new ɇ("AssaultCannonShell","",Ű+ɀ+"0110_MediumCalibreAmmo")},{ȳ+ȷ+"/Missile200mm",new ɇ(
"Missile","",Ű+ɀ+"0100_Missile200mm")},{ȳ+ȷ+"/NATO_25x184mm",new ɇ("GatlingAmmo","",Ű+ɀ+"0080_NATO_25x184mmMagazine")},{ȳ+ȷ+
"/LargeRailgunAmmo",new ɇ("LargeRailgunSabot","",Ű+ɀ+"0140_LargeRailgunAmmo")},{ȳ+ȷ+"/SmallRailgunAmmo",new ɇ("SmallRailgunSabot","",Ű+ɀ+
"0130_SmallRailgunAmmo")},{ȳ+ȷ+"/SemiAutoPistolMagazine",new ɇ("PistolMagazine-S-10","",Ű+ɀ+"0010_SemiAutoPistolMagazine")},{ȳ+ȷ+
"/ElitePistolMagazine",new ɇ("PistolMagazine-S-10E","",Ű+ɀ+"0030_ElitePistolMagazine")},{ȳ+ȷ+"/FullAutoPistolMagazine",new ɇ(
"PistolMagazine-S-10A","",Ű+ɀ+"0020_FullAutoPistolMagazine")},{ȳ+ȷ+"/"+Ʌ+"Gun_Mag_20rd",new ɇ("RifleMagazine-MR-20","",Ű+ɀ+"0040_"+Ʌ+
"Gun_Mag_20rd")},{ȳ+ȷ+"/RapidFire"+Ʌ+"Gun_Mag_50rd",new ɇ("RifleMagazine-MR-50A","",Ű+ɀ+"0050_RapidFire"+Ʌ+"Gun_Mag_50rd")},{ȳ+ȷ+
"/Precise"+Ʌ+"Gun_Mag_5rd",new ɇ("RifleMagazine-MR-8P","",Ű+ɀ+"0060_Precise"+Ʌ+"Gun_Mag_5rd")},{ȳ+ȷ+"/Ultimate"+Ʌ+"Gun_Mag_30rd",
new ɇ("RifleMagazine-MR-30E","",Ű+ɀ+"0070_Ultimate"+Ʌ+"Gun_Mag_30rd")},{ȳ+ȷ+"/FlareClip",new ɇ("FlareGunClip","",Ű+ɀ+
"0051_FlareGunMagazine")},{ȳ+ȷ+"/"+Ʉ+"Blue",new ɇ("","",Ű+ɀ+"0060_"+Ʉ+"Blue")},{ȳ+ȷ+"/"+Ʉ+"Green",new ɇ("","",Ű+ɀ+"0061_"+Ʉ+"Green")},{ȳ+ȷ+"/"+
Ʉ+"Pink",new ɇ("","",Ű+ɀ+"0064_"+Ʉ+"Pink")},{ȳ+ȷ+"/"+Ʉ+"Rainbow",new ɇ("","",Ű+ɀ+"0065_"+Ʉ+"Rainbow")},{ȳ+ȷ+"/"+Ʉ+"Red",
new ɇ("","",Ű+ɀ+"0062_"+Ʉ+"Red")},{ȳ+ȷ+"/"+Ʉ+"Yellow",new ɇ("","",Ű+ɀ+"0063_"+Ʉ+"Yellow")},{ȳ+Ⱥ+
"/AdvancedHandHeldLauncherItem",new ɇ("RocketLauncher-PRO-1","",Ű+ɀ+"0090_AdvancedHandHeldLauncher")},{ȳ+Ⱥ+"/BasicHandHeldLauncherItem",new ɇ(
"RocketLauncher-PO-1","",Ű+ɀ+"0080_BasicHandHeldLauncher")},{ȳ+Ⱥ+"/"+Ʌ+"Item",new ɇ("Rifle-MR-20","",Ű+ɀ+"0040_"+Ʌ)},{ȳ+Ⱥ+"/RapidFire"+Ʌ+
"Item",new ɇ("Rifle-MR-50A","",Ű+ɀ+"0050_RapidFire"+Ʌ)},{ȳ+Ⱥ+"/Precise"+Ʌ+"Item",new ɇ("Rifle-MR-8P","",Ű+ɀ+"0060_Precise"+Ʌ)}
,{ȳ+Ⱥ+"/Ultimate"+Ʌ+"Item",new ɇ("Rifle-MR-30E","",Ű+ɀ+"0070_Ultimate"+Ʌ)},{ȳ+Ⱥ+"/SemiAutoPistolItem",new ɇ("Pistol-S-10"
,"",Ű+ɀ+"0010_SemiAutoPistol")},{ȳ+Ⱥ+"/FullAutoPistolItem",new ɇ("Pistol-S-20A","",Ű+ɀ+"0020_FullAutoPistol")},{ȳ+Ⱥ+
"/ElitePistolItem",new ɇ("Pistol-S-10E","",Ű+ɀ+"0030_EliteAutoPistol")},{ȳ+Ⱥ+"/FlareGunItem",new ɇ("FlareGun","",Ű+ɀ+"0050_FlareGun")},{ȳ+
ȸ+"/OxygenBottle",new ɇ("","",Ű+ɀ+"0010_OxygenBottle")},{ȳ+ȹ+"/HydrogenBottle",new ɇ("","",Ű+ɀ+"0020_HydrogenBottle")},{ȳ
+Ⱥ+"/"+Ɂ+"Item",new ɇ(Ɂ+"0","",Ű+ɀ+"0010_"+Ɂ)},{ȳ+Ⱥ+"/"+Ɂ+"2Item",new ɇ(Ɂ+"1","",Ű+ɀ+"0020_"+Ɂ+"2")},{ȳ+Ⱥ+"/"+Ɂ+"3Item",
new ɇ(Ɂ+"2","",Ű+ɀ+"0030_"+Ɂ+"3")},{ȳ+Ⱥ+"/"+Ɂ+"4Item",new ɇ(Ɂ+"3","",Ű+ɀ+"0040_"+Ɂ+"4")},{ȳ+Ⱥ+"/"+ɂ+"Item",new ɇ(ɂ+"0","",Ű
+ɀ+"0050_"+ɂ)},{ȳ+Ⱥ+"/"+ɂ+"2Item",new ɇ(ɂ+"1","",Ű+ɀ+"0060_"+ɂ+"2")},{ȳ+Ⱥ+"/"+ɂ+"3Item",new ɇ(ɂ+"2","",Ű+ɀ+"0070_"+ɂ+"3")
},{ȳ+Ⱥ+"/"+ɂ+"4Item",new ɇ(ɂ+"3","",Ű+ɀ+"0080_"+ɂ+"4")},{ȳ+Ⱥ+"/WelderItem",new ɇ("Welder0","",Ű+ɀ+"0090_Welder")},{ȳ+Ⱥ+
"/Welder2Item",new ɇ("Welder1","",Ű+ɀ+"0100_Welder2")},{ȳ+Ⱥ+"/Welder3Item",new ɇ("Welder2","",Ű+ɀ+"0110_Welder3")},{ȳ+Ⱥ+"/Welder4Item"
,new ɇ("Welder3","",Ű+ɀ+"0120_Welder4")},{ȳ+Ȼ+"/Algae",new ɇ("","Ingredients","")},{ȳ+Ȼ+"/Grain",new ɇ("","Ingredients",
"")},{ȳ+Ȼ+"/SpaceCredit",new ɇ("","Consumables","")},{ȳ+ȴ+"/Cobalt",new ɇ()},{ȳ+ȴ+"/Gold",new ɇ()},{ȳ+ȴ+"/Ice",new ɇ("Ice"
,"","")},{ȳ+ȴ+"/Iron",new ɇ()},{ȳ+ȴ+"/Magnesium",new ɇ()},{ȳ+ȴ+"/Nickel",new ɇ()},{ȳ+ȴ+"/Platinum",new ɇ()},{ȳ+ȴ+"/Scrap"
,new ɇ()},{ȳ+ȴ+"/Silicon",new ɇ()},{ȳ+ȴ+"/Silver",new ɇ()},{ȳ+ȴ+"/Stone",new ɇ("Stone","","")},{ȳ+ȴ+"/Uranium",new ɇ()},{
ȳ+ȵ+"/Cobalt",new ɇ()},{ȳ+ȵ+"/Gold",new ɇ()},{ȳ+ȵ+"/Stone",new ɇ("Gravel","","")},{ȳ+ȵ+"/Iron",new ɇ()},{ȳ+ȵ+"/Magnesium"
,new ɇ()},{ȳ+ȵ+"/Nickel",new ɇ()},{ȳ+ȵ+"/Platinum",new ɇ()},{ȳ+ȵ+"/PrototechScrap",new ɇ()},{ȳ+ȵ+"/Silicon",new ɇ()},{ȳ+ȵ
+"/Silver",new ɇ()},{ȳ+ȵ+"/Uranium",new ɇ()},};public static bool W=true;public static bool c=false;public MyItemType ň;
public string ŏ;public string ɉ;public string Ɨ;public string Ō;public MyDefinitionId?ť=null;public bool ƣ=false;public bool Ň
=false;public string ő="";public bool Ɋ=false;public float ſ{get{return ɋ;}set{Ɍ=value;}}float ɋ=0;float Ɍ=0;public float
Ə{get{return ɍ;}set{Ɏ=value;}}float ɍ=0;float Ɏ=0;public float Ɛ=0;public float Ǝ{get{return ɏ;}set{ɐ=value;}}float ɏ=0;
float ɐ=0;public float ǻ=0;[Flags]public enum ɔ{ɑ=0,ɒ=1,ɓ=2,}public ɔ ɕ=ɔ.ɑ;public bool Ǭ=false;public int ɖ=1;public int i=0
;string[]ɗ;int ɘ;int ə;public V(MyItemType ŵ){ň=ŵ;ŏ=ŵ.SubtypeId;switch(ŵ.TypeId){case"MyObjectBuilder_Ore":if(!ŵ.
SubtypeId.Contains("Scrap"))ŏ+="Ore";break;case"MyObjectBuilder_Ingot":ŏ+="Ingot";break;case"MyObjectBuilder_SeedItem":ŏ+="Seed";
break;}ɉ=ŵ.TypeId;Ɨ=ŵ.SubtypeId;Ň=true;if(Ɇ.ContainsKey(ɉ)){Ō=Ɇ[ɉ];}else{Ō="Misc";}if(Ɨ.Contains("MealPack_")){ŏ=ŏ.Replace(
"MealPack_","");Ō="Foods";}if(Ɉ.ContainsKey(ɚ())){Ņ=Ɉ[ɚ()];Ɋ=Ɉ[ɚ()].ɛ;if(Ņ.ŏ!="")ŏ=Ņ.ŏ;if(Ņ.Ō!="")Ō=Ņ.Ō;if(Ņ.ɜ!=""&&W)ť=ť=
MyDefinitionId.Parse(Ņ.ɜ);if(Ņ.ɖ!=1)ɖ=Ņ.ɖ;}ɝ();}public V(string ɞ,string ɟ,string Ų,string Ŗ){ɉ=ɞ;Ɨ=ɟ;ŏ=Ų;Ō=Ŗ;if(Ɉ.ContainsKey(ɚ()))Ɋ=
Ɉ[ɚ()].ɛ;ɝ();}public V(string ɗ){this.ɗ=ɗ.Split('~');if(this.ɗ.Length>=7){try{ň=MyItemType.Parse(
$"{this.ɗ[3]}/{this.ɗ[4]}");ŏ=this.ɗ[1];ɉ=ň.TypeId;Ɨ=ň.SubtypeId;Ō=this.ɗ[2];if(this.ɗ[5]!="")ť=MyDefinitionId.Parse(this.ɗ[5]);Ň=true;if(Ɉ.
ContainsKey(ɚ()))Ɋ=Ɉ[ɚ()].ɛ;ɝ();}catch{}}}public override string ToString(){return$"~{ŏ}~{Ō}~{ɉ}~{Ɨ}~{ť.ToString()}~";}public void
N(int ɠ){Ɍ+=ɠ;}public void Ɣ(int ɠ){Ɏ+=ɠ;}public void Ƙ(int ɠ){ɐ+=ɠ;}public void ƚ(){ǻ=Ɍ-ɋ;ɋ=Ɍ;ɏ=ɐ;ɍ=Ɏ;}public string ɚ()
{if(ɉ=="gas"){switch(Ɨ){case"oxygen":return"IconOxygen";case"hydrogen":return"IconHydrogen";}}if(ɉ=="energy"&&Ɨ==
"electricity")return"IconEnergy";return ɉ+"/"+Ɨ;}public string Ǵ(){string ɡ=ɚ();if(c&&Ɋ&&ɡ.StartsWith("MyObjectBuilder_")){return ɡ.
Replace("MyObjectBuilder_","ColorfulIcons_");}return ɡ;}public void ŋ(string ɗ){this.ɗ=ɗ.Split('~');if(this.ɗ.Length>=7){ŏ=this
.ɗ[1];Ō=this.ɗ[2];ɝ();}}public string ǣ(){return$"{ŏ}~~~~{X.ɢ(ɋ)}/{X.ɢ(Ǝ)}{(ɕ.HasFlag(ɔ.ɒ)?"A":"")}{(ɕ.HasFlag(ɔ.ɓ)?"D":"")}{(Ǭ?"H":"")}{(ɖ!=1?$"[Y:{ɖ}]":"")}{(i!=0?$"[P:{i}]":"")}"
;}public string ɣ(){return$"{X.ɢ(ɋ)}/{X.ɢ(Ǝ)} {(ɕ.HasFlag(ɔ.ɒ)?"A":"")}{(ɕ.HasFlag(ɔ.ɓ)?"D":"")}";}public int Ɯ(){ɘ=0;if(
ɕ.HasFlag(ɔ.ɓ)&&ɕ.HasFlag(ɔ.ɒ)){ɘ=(int)Math.Round(Ǝ-ſ-Ɛ*ɖ);}else if(ɕ.HasFlag(ɔ.ɓ)){ɘ=(int)Math.Round(Math.Min(Ǝ-ſ-Ɛ*ɖ,0)
);}else{ɘ=(int)Math.Round(Math.Max(Ǝ-ſ-Math.Max(Ɛ,0)*ɖ,0));}return ɘ/ɖ;}public void Ǣ(string ɤ){var Ū=ɤ.Split(new[]{'~',
'/'});var ɥ=Ū[Ū.Length-1];var Ȥ=ɥ.Split(new[]{'Y','<','['});if(Ȥ[0].Contains("A")){ɕ|=ɔ.ɒ;}else{ɕ&=~ɔ.ɒ;}if(Ȥ[0].Contains(
"D")){ɕ|=ɔ.ɓ;}else{ɕ&=~ɔ.ɓ;}Ǭ=Ȥ[0].Contains("H");string Ŵ;if(ɥ.ŭ("Y<",">",out Ŵ)){ɖ=int.Parse(Ŵ);}else if(ɥ.ŭ("[Y:","]",out
Ŵ)){ɖ=int.Parse(Ŵ);}string ȧ;if(ɥ.ŭ("[P:","]",out ȧ)){i=int.Parse(ȧ);}if(int.TryParse(Ȥ[0].Replace("A","").Replace("D",""
).Replace("H",""),out ə)){ɏ=ə;}ɐ=ɏ;}public bool ŉ(){return ɉ==ȳ+ȴ&&!Ɨ.ToLower().Contains("scrap");}public bool ɦ(){return
ɉ==ȳ+ȴ&&Ɨ.ToLower().Contains("ice");}public bool Ƥ(){return ɉ==ȳ+ȴ&&Ɨ.ToLower().Contains("scrap");}public bool ɧ(){return
ɉ==ȳ+ȴ&&Ɨ.ToLower().Contains("stone");}public bool ư(){return ɉ==ȳ+ȸ;}public bool Ʊ(){return ɉ==ȳ+ȹ;}public float Ǻ(){if(
ɏ==0f)return 1f;return Math.Max(ɋ/(float)ɏ,0f);}void ɝ(){ő=$"{Ō}~{ŏ}";}public static void Ŷ(string ɡ,string Ų="",string Ŗ
="",string Ŭ="",bool ŷ=true,int Ŵ=1){if(Ɉ.ContainsKey(ɡ)&&ŷ){if(Ų!="")Ɉ[ɡ].ŏ=Ų;if(Ŗ!="")Ɉ[ɡ].Ō=Ŗ;if(Ŭ!="")Ɉ[ɡ].ɜ=Ŭ;Ɉ[ɡ].ɛ
=false;Ɉ[ɡ].Ŏ=Ų;}else if(Ɉ.ContainsKey(ɡ)){Ɉ[ɡ].Ŏ=Ų;}else{Ɉ.Add(ɡ,new ɇ(Ų,Ŗ,Ŭ,false){Ŏ=Ų,ɖ=Ŵ});}}}public class ɇ{public
string ŏ;public string Ō;public string ɜ;public bool ɛ=true;public string Ŏ="";public int ɖ=1;public ɇ(bool ɨ=true){ŏ="";Ō="";
ɜ="";ɛ=ɨ;}public ɇ(string Ų,string Ŗ,string Ŭ,bool ɨ=true){ŏ=Ų;Ō=Ŗ;ɜ=Ŭ;ɛ=ɨ;}}public static class ǜ{static List<ɩ>z=new
List<ɩ>();static int ɪ=0;static int ɫ=0;static int ɬ=0;static Random ɭ=new Random();static ǜ(){N(10,$"Want to store a specific amount of Items in a Container? Use the \"{Program.SCT[0]}\" tag and set your values in the custom data!"
);N(10,$"Want to exclude Containers from sorting? Tag them with the \"{Program.ITS[0]}\" tag!");N(10,$"Want to keep more Uranium inside your Reactor? Tag them with \"{Program.SCT[0]}\" and adjust the amount of Uranium Ingots in the Custom Data!"
);N(10,"If you want the Script to Disassemble Items, add the Disassembling modifier to the wanted amount and have at least 1 Assembler set to disassemble."
);N(10,"Stored Bottles will be refilled automatically as long as you have accessible and filled Gas Tanks on your Grid!")
;N(10,"Did you know that you can add new tags to existing ones? Inside the Script Settings you can edit and or Add Tags!"
);N(10,$"Don't want the Script to Count Items in a container for the AutoCrafting Panel? add the \"{Program.HTS[0]}\" tag to its name!"
);N(10,"Did you know that the script knows pretty much all Vanilla Items? It just needs to see the Item Once and it will then add it to the AutoCrafting Panel."
);N(10,"You can change the Display Name of Items inside the Custom Data of the PB running this Script.");N(10,"Need to Teach the Script a modded Item? Just run the PB with \"learning toggle\" and que 1000 of that Item in a Assembler that has \"is Learning\" in its name."
);N(10,$"Don't want the Script to Handle a Machine? just add \"{Program.MTS[0]}\" to the name of that Machine.");N(10,
"Do you Enjoy the Script? Please leave us a Like on our Workshop Page, it helps us a lot!");N(1,"Another Cargo Container needs your Help! I'll mark it on your map.\nA Fallout 4 Reference");N(1,
"MyObjectBuilder_ConsumableItem/Potatoes ?!?\n Apparently the Null Reference Item for Space Engineers");N(1,"A Gold Plated Plate? A PLATINUM PLATED PLATE?!?\nPerry the Platapus Reference");N(1,"Inventory Full - And that voice in my head goes\nInventory Full - Making me lose my mind\nInventory Full - Just like a broken record\nInventory Full - With all the rock and stone I've mined\nSong by Black5heep"
);}public static void N(int ɮ,string ț){z.Add(new ɩ(ɮ,ț));ɪ+=ɮ;}public static string ǝ(){ɫ=ɭ.Next(0,ɪ);ɬ=0;foreach(ɩ ª in
z){if(ɬ<ɫ){ɬ+=ª.ɮ;continue;}return ª.ț;}return z.Last().ț;}}public class ɩ{public int ɮ;public string ț;public ɩ(int ɮ,
string ț){this.ɮ=ɮ;this.ț=ț;}}public class m{public IMyTerminalBlock Ŕ;public IMyInventory Â;public int i=0;public List<string
>ŗ=new List<string>();int ɯ;int ɰ;string þ;public m(IMyTerminalBlock Ŕ){this.Ŕ=Ŕ;Â=Ŕ.GetInventory();ɱ();}public void ɱ(){
ɯ=Ŕ.CustomName.IndexOf("[P:");if(ɯ<0)return;þ=Ŕ.CustomName.Substring(ɯ+3);ɰ=þ.IndexOf(']');if(ɰ<0)return;int.TryParse(þ.
Substring(0,ɰ),out i);}}public static class ʀ{public static bool Ĺ(this string ɲ,string[]ɳ){if(ɳ.Length==1)return ɲ.Contains(ɳ[0]
);foreach(string ɴ in ɳ){if(ɲ.Contains(ɴ))return true;}return false;}public static MyIniValue ǎ(this MyIni ɵ,string Ų){
return ɵ.Get("config",Ų);}public static void Ǟ(this MyIni ɵ,string Ų,bool ɶ){ɵ.Set("config",Ų,ɶ);}public static void Ǟ(this
MyIni ɵ,string Ų,int ɶ){ɵ.Set("config",Ų,ɶ);}public static void Ǟ(this MyIni ɵ,string Ų,string ɶ){ɵ.Set("config",Ų,ɶ);}public
static void Ǟ(this MyIni ɵ,string Ų,float ɶ){ɵ.Set("config",Ų,ɶ);}public static bool ɷ(this MyIni ɵ,string Ų){return ɵ.
ContainsKey("config",Ų);}public static string ǟ(this MyIni ɵ){return ɵ.ToString().Replace("[config]\n","");}public static bool ŭ(
this string ɸ,string ĳ,string ɹ,out string Ũ){Ũ="";int ɺ=0;if(ĳ!=""){ɺ=ɸ.IndexOf(ĳ,StringComparison.Ordinal);if(ɺ==-1)return
false;}if(ɹ!=""){int ɻ=ɸ.Substring(ɺ+ĳ.Length).IndexOf(ɹ,StringComparison.Ordinal);if(ɻ==-1)return false;Ũ=ɸ.Substring(ɺ+ĳ.
Length,ɻ);}else{Ũ=ɸ.Substring(ɺ+ĳ.Length);}return true;}public static string ű(this string ɸ){int ɺ=ɸ.IndexOfAny(new char[]{
'\n','\r'});if(ɺ==-1)return ɸ;return ɸ.Substring(0,ɺ);}public static MyFixedPoint ɼ(this IMyInventory Â){return Â.MaxVolume-
Â.CurrentVolume;}public static MyFixedPoint ɿ(this MyFixedPoint ɽ,MyFixedPoint ɾ){return(MyFixedPoint)((float)ɽ/(float)ɾ)
;}public static MyFixedPoint ɿ(this MyFixedPoint ɽ,float ɾ){return(MyFixedPoint)((float)ɽ/ɾ);}}public class ʄ{public V ʁ;
public List<ʂ>ɖ=new List<ʂ>();float ʃ;public ʄ(V ƭ){ʁ=ƭ;}public bool ʅ(){return ɖ.Count==0;}public string ž(){List<string>Ŵ=
new List<string>();foreach(var Ƨ in ɖ){Ŵ.Add($"{Ƨ.Ơ.ň.TypeId}/{Ƨ.Ơ.ň.SubtypeId}:{Ƨ.ƞ}");}return string.Join(",",Ŵ);}public
float ʆ(Dictionary<MyItemType,V>Ç){ʃ=1000000000f;foreach(var Ƨ in ɖ){if(!Ç.ContainsKey(Ƨ.Ơ.ň))continue;if(Ç[Ƨ.Ơ.ň].ſ/Ƨ.ƞ<ʃ){ʃ
=Ç[Ƨ.Ơ.ň].ſ/Ƨ.ƞ;}}return ʃ;}}public class ç{Dictionary<V,ʄ>ʇ=new Dictionary<V,ʄ>();MyItemType ʈ;V ʉ;List<KeyValuePair<V,
float>>ʊ=new List<KeyValuePair<V,float>>();List<V>Ĕ=new List<V>();Dictionary<string,List<ʋ>>ʌ=new Dictionary<string,List<ʋ>>{
{V.ȳ+V.ȴ+"/Stone",new List<ʋ>{new ʋ(V.ȳ+V.ȵ+"/Stone",1.4f),new ʋ(V.ȳ+V.ȵ+"/Iron",3f),new ʋ(V.ȳ+V.ȵ+"/Nickel",0.24f),new ʋ
(V.ȳ+V.ȵ+"/Silicon",0.4f)}},{V.ȳ+V.ȴ+"/Iron",new List<ʋ>{new ʋ(V.ȳ+V.ȵ+"/Iron",70f)}},{V.ȳ+V.ȴ+"/Nickel",new List<ʋ>{new
ʋ(V.ȳ+V.ȵ+"/Nickel",40f)}},{V.ȳ+V.ȴ+"/Cobalt",new List<ʋ>{new ʋ(V.ȳ+V.ȵ+"/Cobalt",30f)}},{V.ȳ+V.ȴ+"/Silicon",new List<ʋ>{
new ʋ(V.ȳ+V.ȵ+"/Silicon",70f)}},{V.ȳ+V.ȴ+"/Silver",new List<ʋ>{new ʋ(V.ȳ+V.ȵ+"/Silver",10f)}},{V.ȳ+V.ȴ+"/Gold",new List<ʋ>{
new ʋ(V.ȳ+V.ȵ+"/Gold",1f)}},{V.ȳ+V.ȴ+"/Platinum",new List<ʋ>{new ʋ(V.ȳ+V.ȵ+"/Platinum",0.5f)}},{V.ȳ+V.ȴ+"/Magnesium",new
List<ʋ>{new ʋ(V.ȳ+V.ȵ+"/Magnesium",0.7f)}},{V.ȳ+V.ȴ+"/Uranium",new List<ʋ>{new ʋ(V.ȳ+V.ȵ+"/Uranium",1f)}},};string[]ɗ;string
[]ʍ;string[]ʎ;MyIni j=new MyIni();public bool ź(V ƭ){return ʇ.ContainsKey(ƭ);}public void Ŋ(V ƭ,Dictionary<MyItemType,V>Ç
){ʇ.Add(ƭ,new ʄ(ƭ));if(!ʌ.ContainsKey(ƭ.ɚ()))return;foreach(var Ŵ in ʌ[ƭ.ɚ()]){ʈ=MyItemType.Parse(Ŵ.ɕ);if(!Ç.ContainsKey(
ʈ))Ç.Add(ʈ,new V(ʈ));ʇ[ƭ].ɖ.Add(new ʂ(Ç[ʈ],Ŵ.ƞ));if(Ç[ʈ].ƣ==false)Ç[ʈ].ƣ=true;}}public bool ʏ(){foreach(var ƭ in ʇ){if(ƭ.
Value.ʅ()){return true;}}return false;}public bool Ż(V ƭ){return ʇ.ContainsKey(ƭ)&&ʇ[ƭ].ʅ();}public void Ž(V ƭ,Dictionary<
MyItemType,V>Ç,List<MyInventoryItem>z,int Ŵ){foreach(var ª in z){if(!Ç.ContainsKey(ª.Type))Ç.Add(ª.Type,new V(ª.Type));ʇ[ƭ].ɖ.Add(
new ʂ(Ç[ª.Type],(float)ª.Amount/(Ŵ/100f)));Ç[ª.Type].ƣ=true;}}public string ž(V Ç){return ʇ[Ç].ž();}public override string
ToString(){List<string>ǯ=new List<string>();foreach(var Ƨ in ʇ){ǯ.Add($"~{Ƨ.Key.ŏ}~{Ƨ.Key.ɉ}~{Ƨ.Key.Ɨ}~{Ƨ.Value.ž()}~");}return
string.Join("\n",ǯ);}public void ō(string ʐ,Dictionary<MyItemType,V>Ç){ɗ=ʐ.Split('~');if(ɗ.Length<6)return;try{MyItemType Ƌ=
MyItemType.Parse($"{ɗ[2]}/{ɗ[3]}");if(!Ç.ContainsKey(Ƌ))Ç.Add(Ƌ,new V(Ƌ));if(!ʇ.ContainsKey(Ç[Ƌ]))ʇ.Add(Ç[Ƌ],new ʄ(Ç[Ƌ]));if(!ʇ[Ç[
Ƌ]].ʅ())return;ʍ=ɗ[4].Split(',');foreach(var Ŵ in ʍ){if(Ŵ=="")continue;ʎ=Ŵ.Split(':');if(ʎ.Length!=2)continue;MyItemType
ʑ=MyItemType.Parse(ʎ[0]);if(!Ç.ContainsKey(ʑ))Ç.Add(ʑ,new V(ʑ));Ç[ʑ].ƣ=true;ʇ[Ç[Ƌ]].ɖ.Add(new ʂ(Ç[ʑ],float.Parse(ʎ[1])));
}}catch(Exception Ǥ){}}public V ƨ(V ʒ,out float ʓ,Dictionary<MyItemType,V>Ç){ʉ=null;ʓ=0;foreach(var ƭ in ʇ){foreach(var Ŵ
in ƭ.Value.ɖ){if(Ŵ.Ơ==ʒ&&Ŵ.ƞ>ʓ&&Ç[ƭ.Key.ň].Ə>0){ʓ=Ŵ.ƞ;ʉ=ƭ.Key;}}}return ʉ;}public void ƕ(MyInventoryItem ª,Dictionary<
MyItemType,V>Ç,IMyRefinery ł){j.Clear();X.Ǌ(ł.CustomData,ref j);int ʔ=j.ǎ("yield").ToInt32(100);foreach(var ů in ʇ){if(ů.Key.ň!=ª.
Type)continue;foreach(var Ŵ in ů.Value.ɖ){Ç[Ŵ.Ơ.ň].Ɛ+=((float)ª.Amount/100f)*Ŵ.ƞ*(ʔ/100f);}}}public List<V>ƪ(Dictionary<
MyItemType,V>Ç){ʉ=null;ʊ.Clear();Ĕ.Clear();foreach(var ª in Ç){if(ª.Value.Ə<=0f)continue;if(ª.Value.ŉ()&&ʇ.ContainsKey(ª.Value)&&!
ª.Value.ɦ()&&!ª.Value.ɧ()){ʊ.Add(new KeyValuePair<V,float>(ª.Value,ʇ[ª.Value].ʆ(Ç)));}else if(ª.Value.ɧ()){ʊ.Add(new
KeyValuePair<V,float>(ª.Value,0f));}else if(ª.Value.ɦ()){}else if(ª.Value.Ƥ()){ʊ.Add(new KeyValuePair<V,float>(ª.Value,0.001f));}
else if(ª.Value.ŉ()){ʊ.Add(new KeyValuePair<V,float>(ª.Value,1000000000f));}}ʊ=ʊ.OrderBy(ş=>ş.Value).ToList();foreach(var Ŵ
in ʊ){Ĕ.Add(Ŵ.Key);Program.Ʃ($"{Ŵ.Key.ŏ}: {Ŵ.Value}");}return Ĕ;}public void Ń(){Ĕ.Clear();ʇ.Clear();ʊ.Clear();}}public
class ʂ{public V Ơ;public float ƞ;public ʂ(V ª,float Ș){Ơ=ª;ƞ=Ș;}}public class ʋ{public string ɕ;public float ƞ;public ʋ(
string Ƌ,float Ș){ɕ=Ƌ;ƞ=Ș;}}public static class R{public static bool S;public static Color U;public static Color T;public
static float ǳ=1f;public static void Ǡ(){ǳ-=0.01f;if(ǳ<0.98f){ǳ=1f;}}public static void ǒ(IMyTextSurface ȑ,MySpriteDrawFrame ǌ
,Vector2 Ǎ,string ɸ,float ƶ=1f){var ǰ=new MySprite(){Type=SpriteType.TEXT,Data=ɸ,Position=Ǎ,RotationOrScale=ƶ,Color=ȑ.
ScriptForegroundColor.Shade(ǳ),Alignment=TextAlignment.LEFT,FontId="White"};ǌ.Add(ǰ);}public static void Ȓ(IMyTextSurface ȑ,MySpriteDrawFrame
ǌ,Vector2 Ǎ,string ɸ,float ƶ=1f){var ǰ=new MySprite(){Type=SpriteType.TEXT,Data=ɸ,Position=Ǎ,RotationOrScale=ƶ,Color=ȑ.
ScriptForegroundColor.Shade(ǳ),Alignment=TextAlignment.CENTER,FontId="White"};ǌ.Add(ǰ);}public static int ǖ(IMyTextSurface ȑ,
MySpriteDrawFrame ǌ,Vector2 Ǎ,string ɸ,float ƶ=1f,int Ʒ=16){List<string>ʕ=new List<string>();string[]ʖ=ɸ.Split(' ');string þ="";int Ļ=0;
foreach(string ʗ in ʖ){if(Ļ==0){þ=ʗ;Ļ++;continue;}if(ʗ.EndsWith("\n")){ʕ.Add($"{þ} {ʗ.TrimEnd('\n')}");þ="";Ļ=0;}else if(ȑ.
MeasureStringInPixels(new StringBuilder($"{þ} {ʗ}"),"White",ƶ).X+(2*Ʒ)>ȑ.SurfaceSize.X){ʕ.Add($"{þ}");þ=ʗ;}else{þ=$"{þ} {ʗ}";}}ʕ.Add($"{þ}");
ǒ(ȑ,ǌ,Ǎ,string.Join("\n",ʕ),ƶ);return ʕ.Count;}public static void ǔ(IMyTextSurface ȑ,MySpriteDrawFrame ǌ,Vector2 Ǎ,string
ɸ,float ƶ=1f,int Ʒ=16){var ǰ=new MySprite(){Type=SpriteType.TEXT,Data=ɸ,Position=Ǎ,RotationOrScale=ƶ,Color=ȑ.
ScriptForegroundColor.Shade(ǳ),Alignment=TextAlignment.LEFT,FontId="White"};ǌ.Add(ǰ);ǰ=new MySprite(){Type=SpriteType.TEXTURE,Data=
"SquareSimple",Position=Ǎ+new Vector2(0,30*ƶ),Size=new Vector2(ȑ.SurfaceSize.X-(Ʒ*2),2*ƶ),Color=ȑ.ScriptForegroundColor.Shade(ǳ),
Alignment=TextAlignment.LEFT};ǌ.Add(ǰ);}public static void Ƿ(IMyTextSurface ȑ,MySpriteDrawFrame ǌ,Vector2 Ǎ,V ª,float ƶ=1f,int Ʒ=
16,RectangleF?ô=null){ʘ(ȑ,ǌ,Ǎ,ª.ŏ,ª.Ǵ(),ª.Ǝ!=0?$"{X.ʙ(ª.ſ)}/{X.ʙ(ª.Ǝ)}":$"{X.ʙ(ª.ſ)}",ƶ,Ʒ,ô);}public static void Ǒ(
IMyTextSurface ȑ,MySpriteDrawFrame ǌ,Vector2 Ǎ,string ʚ,float ƶ=1f,int Ʒ=16,float Ǖ=0f,bool ʛ=false){var ǰ=new MySprite(){Type=
SpriteType.TEXTURE,Data=ʚ,Position=Ǎ+new Vector2(16,16)*ƶ,Size=new Vector2(32,32)*ƶ,Color=ʛ?ȑ.ScriptForegroundColor.Shade(ǳ):Color
.White.Shade(ǳ),Alignment=TextAlignment.CENTER,RotationOrScale=Ǖ};ǌ.Add(ǰ);}public static void ȓ(IMyTextSurface ȑ,
MySpriteDrawFrame ǌ,Vector2 Ǎ,Vector2 ʜ,string ʚ,Color ʝ,float Ǖ=0f,float Ȕ=1f){var ǰ=new MySprite(){Type=SpriteType.TEXTURE,Data=ʚ,
Position=Ǎ,Size=ʜ,Color=ʝ==Color.Black?ȑ.ScriptForegroundColor.Shade(ǳ).Alpha(Ȕ):ʝ.Shade(ǳ).Alpha(Ȕ),Alignment=TextAlignment.
CENTER,RotationOrScale=Ǖ};ǌ.Add(ǰ);}public static void Ǹ(IMyTextSurface ȑ,MySpriteDrawFrame ǌ,Vector2 Ǎ,V ª,float ƶ=1f,int Ʒ=
16,RectangleF?ô=null){ʘ(ȑ,ǌ,Ǎ,"",ª.Ǵ(),ª.Ǝ!=0?$"{X.ʙ(ª.ſ)}/{X.ʙ(ª.Ǝ)}":$"{X.ʙ(ª.ſ)}",ƶ,Ʒ,ô);}public static void ǭ(
IMyTextSurface ȑ,MySpriteDrawFrame ǌ,Vector2 Ǎ,V ª,float ƶ=1f,int Ʒ=16,RectangleF?ô=null,bool ʛ=false){ʘ(ȑ,ǌ,Ǎ,ª.ŏ,ª.Ǵ(),ª.ɣ(),ƶ,Ʒ,ô,ʛ
);if(ª.Ɛ!=0){Ǎ.X+=16*ƶ;Ǎ.Y+=12*ƶ;Ǒ(ȑ,ǌ,Ǎ,"Construction",ƶ/1.5f,Ʒ);}}public static void ʘ(IMyTextSurface ȑ,
MySpriteDrawFrame ǌ,Vector2 Ǎ,string ɸ,string ʚ,string ɶ,float ƶ=1f,int Ʒ=16,RectangleF?ô=null,bool ʛ=false){var ǰ=new MySprite(){Type=
SpriteType.TEXTURE,Data=ʚ,Position=Ǎ+new Vector2(16,16)*ƶ,Size=new Vector2(32,32)*ƶ,Color=ʛ?ȑ.ScriptForegroundColor.Shade(ǳ):Color
.White.Shade(ǳ),Alignment=TextAlignment.CENTER};ǌ.Add(ǰ);Ǎ+=new Vector2(40*ƶ,0);ǰ=new MySprite(){Type=SpriteType.TEXT,
Data=ɸ,Position=Ǎ,RotationOrScale=ƶ,Color=ȑ.ScriptForegroundColor.Shade(ǳ),Alignment=TextAlignment.LEFT,FontId="White"};ǌ.
Add(ǰ);if(ô!=null){Ǎ=new Vector2(ô.Value.Right-Ʒ,Ǎ.Y);}else{Ǎ=new Vector2(ȑ.SurfaceSize.X-Ʒ,Ǎ.Y);}ǰ=new MySprite(){Type=
SpriteType.TEXT,Data=ɶ,Position=Ǎ,RotationOrScale=ƶ,Color=ȑ.ScriptForegroundColor.Shade(ǳ),Alignment=TextAlignment.RIGHT,FontId=
"White"};ǌ.Add(ǰ);}public static void ǹ(IMyTextSurface ȑ,MySpriteDrawFrame ǌ,Vector2 Ǎ,string ʚ,float ɶ,float ƶ=1f,int Ʒ=16,
bool ʛ=false,int ʝ=-1){var ǰ=new MySprite(){Type=SpriteType.TEXTURE,Data=ʚ,Position=Ǎ+new Vector2(16,16)*ƶ,Size=new Vector2(
32,32)*ƶ,Color=ʛ?ȑ.ScriptForegroundColor.Shade(ǳ):Color.White.Shade(ǳ),Alignment=TextAlignment.CENTER};ǌ.Add(ǰ);Ǎ+=new
Vector2(40*ƶ,0);ǰ=new MySprite(){Type=SpriteType.TEXTURE,Data="SquareSimple",Position=Ǎ+new Vector2(0,16)*ƶ,Size=new Vector2(ȑ.
SurfaceSize.X-(Ʒ*2)-(40*ƶ),20*ƶ),Color=ȑ.ScriptForegroundColor.Shade(0.25f).Shade(ǳ),Alignment=TextAlignment.LEFT};ǌ.Add(ǰ);ǰ=new
MySprite(){Type=SpriteType.TEXTURE,Data="SquareSimple",Position=Ǎ+new Vector2(0,16)*ƶ,Size=new Vector2((ȑ.SurfaceSize.X-(Ʒ*2)-(
40*ƶ))*ɶ,20*ƶ),Color=ǲ(ɶ,ʝ,ȑ.ScriptForegroundColor.Shade(ǳ)),Alignment=TextAlignment.LEFT};ǌ.Add(ǰ);}public static void Ǚ(
IMyTextSurface ȑ,MySpriteDrawFrame ǌ,Vector2 Ǎ,float ɶ,float ƶ=1f,int Ʒ=16,float ǚ=1f,float ʞ=0f,int ʝ=-1){var ǰ=new MySprite(){Type=
SpriteType.TEXTURE,Data="SquareSimple",Position=Ǎ+(ʞ==0f?new Vector2(0,16)*ƶ:new Vector2(0,ʞ/2f)),Size=new Vector2(ȑ.SurfaceSize.X
*ǚ-(Ʒ*2),ʞ==0?20*ƶ:ʞ),Color=ȑ.ScriptForegroundColor.Shade(0.25f).Shade(ǳ),Alignment=TextAlignment.LEFT};ǌ.Add(ǰ);ǰ=new
MySprite(){Type=SpriteType.TEXTURE,Data="SquareSimple",Position=Ǎ+(ʞ==0f?new Vector2(0,16)*ƶ:new Vector2(0,ʞ/2f)),Size=new
Vector2((ȑ.SurfaceSize.X*ǚ-(Ʒ*2))*ɶ,ʞ==0?20*ƶ:ʞ),Color=ǲ(ɶ,ʝ,ȑ.ScriptForegroundColor.Shade(ǳ)),Alignment=TextAlignment.LEFT};ǌ.
Add(ǰ);}public static void Ǽ(IMyTextSurface ȑ,MySpriteDrawFrame ǌ,Vector2 Ǎ,V ª,float ƶ=1f,int Ʒ=16){var ǰ=new MySprite(){
Type=SpriteType.TEXTURE,Data=ª.Ǵ(),Position=Ǎ+new Vector2(16,16)*ƶ,Size=new Vector2(32,32)*ƶ,Color=ȑ.ScriptForegroundColor.
Shade(ǳ),Alignment=TextAlignment.CENTER};ǌ.Add(ǰ);Ǎ+=new Vector2(40*ƶ,0);ǰ=new MySprite(){Type=SpriteType.TEXT,Data=ª.ŏ,
Position=Ǎ,RotationOrScale=ƶ,Color=ȑ.ScriptForegroundColor.Shade(ǳ),Alignment=TextAlignment.LEFT,FontId="White"};ǌ.Add(ǰ);Ǎ=new
Vector2(ȑ.SurfaceSize.X-Ʒ-32*ƶ,Ǎ.Y);ǰ=new MySprite(){Type=SpriteType.TEXTURE,Data="Triangle",Position=Ǎ+new Vector2(16,16)*ƶ,
Size=new Vector2(32,16)*ƶ,Color=ǲ(ª.ǻ>0?1f:0f,1).Shade(ǳ),RotationOrScale=ª.ǻ>0?0f:(float)Math.PI,Alignment=TextAlignment.
CENTER};ǌ.Add(ǰ);Ǎ=new Vector2(ȑ.SurfaceSize.X-Ʒ-40*ƶ,Ǎ.Y);ǰ=new MySprite(){Type=SpriteType.TEXT,Data=X.ʙ(ª.ǻ),Position=Ǎ,
RotationOrScale=ƶ,Color=ǲ(ª.ǻ>0?1f:0f,1).Shade(ǳ),Alignment=TextAlignment.RIGHT,FontId="White"};ǌ.Add(ǰ);}public static Color ǲ(float ɶ
,int ʟ=0,Color?ʝ=null){switch(ʟ){default:if(S)return Color.Lerp(U,T,ɶ);return Color.Lerp(Color.Green,Color.Red,ɶ);case 1:
if(S)return Color.Lerp(U,T,1f-ɶ);return Color.Lerp(Color.Green,Color.Red,1f-ɶ);case 2:if(S)return Color.Lerp(ʝ.Value,T,ɶ);
return Color.Lerp(ʝ.Value,Color.Red,ɶ);case 3:if(S)return Color.Lerp(U,ʝ.Value,1f-ɶ);return Color.Lerp(Color.Green,ʝ.Value,1f-
ɶ);case-1:return ʝ.Value;}}public static float Ǘ(RectangleF ô,float ʠ){return ô.Position.X+ʠ;}public static void Ǿ(
IMyTextSurface ʡ,MySpriteDrawFrame ǌ,string ʢ,int ʣ,float Ȕ,RectangleF ô,Color ʝ,bool ʤ=true){var ǰ=new MySprite();int ʥ=ʣ;int ʦ=ʣ;if(
ʤ){if(ô.Width>=ô.Height*2){ʥ*=2;}else if(ô.Height>=ô.Width*2){ʦ*=2;}}for(int Ő=0;Ő<ʥ;Ő++){for(int ʧ=0;ʧ<ʦ;ʧ++){ǰ=new
MySprite(){Type=SpriteType.TEXTURE,Data=ʢ,Position=ô.Position+new Vector2(ô.Width/ʥ,ô.Height/ʦ)*0.5f+new Vector2(ô.Width/ʥ*Ő,ô.
Height/ʦ*ʧ),Size=new Vector2(ô.Width/ʥ,ô.Height/ʦ),Color=ʝ==Color.Black?ʡ.ScriptForegroundColor.Shade(ǳ).Alpha(Ȕ):ʝ.Shade(ǳ).
Alpha(Ȕ),Alignment=TextAlignment.CENTER};ǌ.Add(ǰ);}}}}public class Į{public static int ʨ=200;public int i=0;public bool ƀ=
true;public static Dictionary<IMyTerminalBlock,Į>ƫ=new Dictionary<IMyTerminalBlock,Į>();public static List<Į>Ɓ=new List<Į>()
;public static Dictionary<string,Į>ǿ=new Dictionary<string,Į>();public string ŏ=>Ų!=string.Empty?Ų:ʩ.CustomName;string Ų=
string.Empty;Dictionary<MyItemType,ʪ>Ƭ=new Dictionary<MyItemType,ʪ>();Dictionary<string,ʪ>ʫ=new Dictionary<string,ʪ>();List<
string>ʬ=new List<string>();ʪ ʭ;MyInventoryItem?µ;int ɯ;int ɰ;string þ;int ʮ=0;int ʯ=0;int ʰ=0;bool ʱ;string ʲ="";string ʳ="";
IMyTerminalBlock ʩ;List<IMyTerminalBlock>ʴ=new List<IMyTerminalBlock>();List<IMyInventory>ʵ=new List<IMyInventory>();public bool Ȇ=true;
IMyTextSurface ȇ;string ɤ;MyFixedPoint ʶ;int ʷ=0;public int ř=5;MyFixedPoint ʸ;bool ʹ;MyFixedPoint º;List<MyFixedPoint>ʺ=new List<
MyFixedPoint>();MyFixedPoint ʻ;MyFixedPoint ʼ;MyFixedPoint ʽ;MyFixedPoint ʾ;List<MyFixedPoint>ʿ=new List<MyFixedPoint>();
MyFixedPoint ˀ;int ˁ=0;string[]ˆ;IMyGridTerminalSystem ˇ;List<IMyTerminalBlock>t;IMyTerminalBlock ˈ;Vector3 ǁ;float ˉ;float Ā;bool ˊ
=false;string[]ˋ;public Į(IMyTerminalBlock Ŕ,IMyGridTerminalSystem ˌ=null,List<IMyTerminalBlock>t=null){ʩ=Ŕ;if(Ŕ is
IMyTextSurface){Ȇ=false;ˇ=ˌ;this.t=t;ȇ=Ŕ as IMyTextSurface;if(ʩ.CustomData=="")ʩ.CustomData=
"Margin=0\nSize=0.7\nStockStats\nStockLevel,Color=2\nStockVolume,Color=1";}else{Ŝ(Ŕ);}Ɓ.Add(this);ɱ();}public Į(IMyTerminalBlock Ŕ,V ª,int Ș,int ȧ){ʩ=Ŕ;Ŝ(Ŕ);ʭ=new ʪ(ª,Ș,ʪ.ˍ.ˎ);Ƭ.Add(ª.ň,ʭ);i=ȧ;
ƀ=false;Ɓ.Add(this);}public Į(int ȧ,string Ų,int ʷ=1){i=ȧ;ƀ=false;this.Ų=Ų;this.ʷ=ʷ;Ɓ.Add(this);ǿ.Add(this.Ų,this);}
public void Ŝ(IMyTerminalBlock Ŕ,int ˏ=0){if(!ƫ.ContainsKey(Ŕ))ƫ.Add(Ŕ,this);ʴ.Add(Ŕ);ʵ.Add(Ŕ.GetInventory(ˏ));}public void N(
V ª,int Ș){if(Ș==-1){ʭ=new ʪ(ª,0,ʪ.ˍ.ː);}else{ʭ=new ʪ(ª,Ș,ʪ.ˍ.ˎ);}if(Ƭ.ContainsKey(ª.ň))Ƭ[ª.ň].ˑ+=Ș;else Ƭ.Add(ª.ň,ʭ);}
public static void œ(){ƫ.Clear();Ɓ.Clear();ǿ.Clear();}public static bool ś(IMyTerminalBlock Ŕ){return ƫ.ContainsKey(Ŕ);}public
static Į ŕ(IMyTerminalBlock Ŕ){return ƫ[Ŕ];}public static void Ş(){ƫ=ƫ.OrderByDescending(ş=>ş.Value.i).ToDictionary(Š=>Š.Key,Š
=>Š.Value);}public MyFixedPoint ƅ(MyItemType ª,MyFixedPoint Ș,ref bool Ƅ){if(!Ƭ.ContainsKey(ª))return 0;if(Ƭ[ª].ˠ==ʪ.ˍ.ː){
Ƅ=true;}else{Ƅ=false;}if(Ƭ[ª].ˑ==0&&Ƭ[ª].ˠ!=ʪ.ˍ.ː)return 0;if(Ƭ[ª].ˠ==ʪ.ˍ.ˡ)return 0;if(Ƭ[ª].ˠ==ʪ.ˍ.ˎ||Ƭ[ª].ˠ==ʪ.ˍ.ˢ){º=Ƭ
[ª].ˑ;foreach(var Â in ʵ){µ=null;µ=Â.FindItem(ª);if(µ!=null)º-=µ.Value.Amount;}Ș=MyFixedPoint.Min(Ș,º);}Ș=MyFixedPoint.
Min(Ș,ˣ().ɿ(ª.GetItemInfo().Volume));return Ș;}public MyFixedPoint ˣ(){ʶ=MyFixedPoint.Zero;foreach(var Â in ʵ){ʶ+=Â.ɼ();}
return ʶ;}public MyFixedPoint ˤ(){ʶ=MyFixedPoint.Zero;foreach(var Â in ʵ){ʶ+=Â.MaxVolume;}return ʶ;}public float Ͳ(){float ˬ=0
;float ˮ=0;Ͱ();foreach(var Ƨ in Ƭ){if(Ƨ.Value.ˠ==ʪ.ˍ.ː||Ƨ.Value.ˑ==0)continue;ˬ+=Ƨ.Value.ˑ;ˮ+=Ƨ.Value.ͱ;}if(ˬ<=0f)return
1f-(float)ˣ().ɿ(ˤ());return ˮ/ˬ;}void Ͱ(){if(ˊ)return;List<MyInventoryItem>z=new List<MyInventoryItem>();foreach(var Â in
ʵ){z.Clear();Â.GetItems(z);foreach(var ª in z){if(!Ƭ.ContainsKey(ª.Type))continue;Ƭ[ª.Type].ͱ+=(float)ª.Amount;}}ˊ=true;}
public MyFixedPoint Ɔ(MyItemType ª,MyFixedPoint Ș,int ȧ=0){if(!Ƭ.ContainsKey(ª))return Ș;if(ȧ>i)return Ș;if(Ƭ[ª].ˑ==0&&Ƭ[ª].ˠ
!=ʪ.ˍ.ː)return Ș;if(Ƭ[ª].ˠ==ʪ.ˍ.ˡ||Ƭ[ª].ˠ==ʪ.ˍ.ˎ){ʸ=0;foreach(var Â in ʵ){µ=Â.FindItem(ª);if(µ.HasValue)ʸ+=µ.Value.Amount;
}return Math.Min(Math.Max((int)ʸ-Ƭ[ª].ˑ,0),(int)Ș);}return 0;}void ͺ(List<V>ͳ){ʬ.Clear();þ="";ʲ="";ʳ="";ʬ.Add(
"@GOAT-Stock Definitions START");if(!Ȇ){ʬ.Add($"~~~~~~~~ Container List ~~~~~~~~");foreach(var Ŕ in ʴ){ʬ.Add(ʹ(Ŕ));}ʬ.Add($"~~~~~~~~ Settings ~~~~~~~~"
);ʬ.Add($"method={(ʷ==0?"Priority":"Balance")}");ʬ.Add($"rebalancePercentage={ř}");ʬ.Add($"name={ŏ}");ʬ.Add(
$"~~~~~~~~ Modifier Explanation ~~~~~~~~");}ʬ.Add("~ ItemName=100 >>> Adds 100, removes excess.");ʬ.Add("~ ItemName=100M >>> Adds 100, Ignoring excess.");ʬ.Add(
"~ ItemName=100L >>> Doesn't Add Item, removes above 100.");ʬ.Add("~ ItemName=All >>> Adds Item until full, Ignores excess.");ʬ.Add(
"~ ItemName=100P >>> Pins the Item to the Top of the List");ʬ.Add("~ Note: P can be used after any Modifier, Example =100MP");ʬ.Add($"~~~~~~~~ Pinned Items ~~~~~~~~");foreach(V Ç
in ͳ){if(!Ƭ.ContainsKey(Ç.ň))continue;if(!Ƭ[Ç.ň].Ͷ)continue;ʬ.Add(Ƭ[Ç.ň].ToString());}foreach(V Ç in ͳ){if(!Ƭ.ContainsKey(
Ç.ň))continue;if(Ƭ[Ç.ň].Ͷ)continue;if(þ!=Ç.Ō){ʬ.Add($"~~~~~~~~ {Ç.Ō} ~~~~~~~~");þ=Ç.Ō;}ʬ.Add(Ƭ[Ç.ň].ToString());}ʬ.Add(
$"~~~~~~~~ Currently Unknown Items ~~~~~~~~");foreach(var ͷ in ʫ){ʬ.Add(ͷ.Value.ToString());}ʬ.Add($"@GOAT-Stock Definitions END");if(Ȇ){ɯ=ʩ.CustomData.IndexOf(
"@GOAT-Stock Definitions");if(ɯ>=0){ʲ=ʩ.CustomData.Substring(0,ɯ);}else{ʲ=ʩ.CustomData;}ɰ=ʩ.CustomData.IndexOf("@GOAT-Stock Definitions END");if(
ɰ>0&&ɰ+27<ʩ.CustomData.Length){ʳ=ʩ.CustomData.Substring(ɰ+27);}if(!ʲ.EndsWith("\n"))ʲ+="\n";ʩ.CustomData=ʲ+string.Join(
"\n",ʬ)+ʳ;}else{ȇ.WriteText(string.Join("\n",ʬ));}}public void ͻ(){ʮ=0;}public bool ŋ(List<V>ͳ,Dictionary<string,V>Ê){ʰ=0;ʯ=
0;ʱ=false;ˁ=0;if(Ȇ){ɤ=ʩ.CustomData;}else{ɤ=ȇ.GetText();}if(ɤ!=""){ʬ=ɤ.Split('\n').ToList();foreach(var ͼ in ʬ){if(!ͼ.
StartsWith("@GOAT-Stock Definitions")&&!ʱ)continue;ʱ=true;if(ʯ<ʮ){ʯ++;continue;}if(ʯ==ʮ){ʯ+=999999;}if(ͼ.StartsWith(
"~~~~~~~~ Cont")){ˁ=1;ʮ++;continue;}if(ͼ.StartsWith("~~~~~~~~ Set")){ˁ=2;ʮ++;continue;}if(ͼ.StartsWith("~~~~~~~~ Mod")){ˁ=0;ʮ++;
continue;}if(ˁ==1){ͽ(ͼ);ʮ++;continue;}if(ˁ==2){ˆ=ͼ.Split('=');if(ˆ.Length==2){switch(ˆ[0]){case"method":ʷ=ˆ[1].StartsWith("Prio"
)?0:1;break;case"rebalancePercentage":ř=int.Parse(ˆ[1]);break;case"name":Ų=ˆ[1];if(!ǿ.ContainsKey(Ų))ǿ.Add(Ų,this);break;
}}ʮ++;continue;}if(ͼ.StartsWith("~")||ͼ.StartsWith("@")){ʰ++;ʮ++;continue;}ʭ=new ʪ(ͼ,Ê);if(ʭ.Ň&&!Ƭ.ContainsKey(ʭ.ŵ.ň)){Ƭ.
Add(ʭ.ŵ.ň,ʭ);}else if(ʭ.Ň){Ƭ[ʭ.ŵ.ň].ˑ=ʭ.ˑ;Ƭ[ʭ.ŵ.ň].ˠ=ʭ.ˠ;Ƭ[ʭ.ŵ.ň].Ͷ=ʭ.Ͷ;}else if(!string.IsNullOrEmpty(ʭ.Ά)&&!ʫ.ContainsKey
(ʭ.Ά)){ʫ.Add(ʭ.Ά,ʭ);}else if(!string.IsNullOrEmpty(ʭ.Ά)){ʫ[ʭ.Ά].ˑ=ʭ.ˑ;ʫ[ʭ.Ά].ˠ=ʭ.ˠ;ʫ[ʭ.Ά].Ͷ=ʭ.Ͷ;}ʰ++;ʮ++;if(ʰ>=ʨ){return
false;}if(ͼ.StartsWith("@GOAT-Stock Definitions END"))break;}}foreach(var Ç in ͳ){if(!Ƭ.ContainsKey(Ç.ň)){Ƭ.Add(Ç.ň,new ʪ(Ç,0
,ʪ.ˍ.ˎ));}}ͺ(ͳ);return true;}public void ɱ(){ɯ=ʩ.CustomName.IndexOf("[P:");if(ɯ<0)return;þ=ʩ.CustomName.Substring(ɯ+3);ɰ=
þ.IndexOf(']');if(ɰ<0)return;int.TryParse(þ.Substring(0,ɰ),out i);}public bool ƃ(IMyInventory ș,MyItemType ŵ){foreach(var
Â in ʵ){if(!ș.CanTransferItemTo(Â,ŵ))return false;}return true;}void Έ(){ʺ.Clear();ʼ=0;ʽ=0;foreach(var Â in ʵ){ʻ=Â.ɼ();if
(ʻ>ʼ)ʼ=ʻ;ʺ.Add(ʻ);}for(int Ļ=0;Ļ<ʺ.Count;Ļ++){ʺ[Ļ]=ʺ[Ļ].ɿ(ʼ);ʽ+=ʺ[Ļ];}}public bool Ɗ(){if(ʷ==0)return false;if(ʵ.Count<=0
)return false;float Ή=1f;float Ί=0f;foreach(var Â in ʵ){if(Â.VolumeFillFactor>Ί)Ί=Â.VolumeFillFactor;if(Â.
VolumeFillFactor<Ή)Ή=Â.VolumeFillFactor;}if((Ί-Ή)*100f<=ř)return false;X.ƌ(ʵ);return true;}public bool Ƈ(IMyInventory ș,int ǯ,
MyFixedPoint?ˑ=null){µ=ș.GetItemAt(ǯ);if(!µ.HasValue)return false;if(ˑ.HasValue)ʸ=ˑ.Value;else ʸ=µ.Value.Amount;if(ʷ==1){ʹ=false;Έ()
;ʾ=ʸ.ɿ(ʽ);ʿ.Clear();foreach(var Ό in ʺ){ʿ.Add(Ό*ʾ);}if(!µ.Value.Type.GetItemInfo().UsesFractions){ˀ=0;for(int Ļ=0;Ļ<ʿ.
Count;Ļ++){ˀ+=ʿ[Ļ]-MyFixedPoint.Floor(ʿ[Ļ]);ʿ[Ļ]=MyFixedPoint.Floor(ʿ[Ļ]);}for(int Ļ=0;Ļ<ʺ.Count;Ļ++){if(ʺ[Ļ]==1){ʿ[Ļ]=
MyFixedPoint.Ceiling(ʿ[Ļ]+ˀ);break;}}}for(int Ļ=0;Ļ<ʵ.Count;Ļ++){if(ʵ[Ļ].ɼ()<µ.Value.Type.GetItemInfo().Volume*ʿ[Ļ])return false;}ʹ=
true;for(int Ļ=0;Ļ<ʵ.Count;Ļ++){ș.TransferItemTo(ʵ[Ļ],ǯ,null,true,ʿ[Ļ]);}}else{if(ˑ.HasValue&&µ.Value.Type.GetItemInfo().
Volume*ˑ>ˣ())return false;if(!ˑ.HasValue){foreach(var Â in ʵ){ʹ|=ș.TransferItemTo(Â,ǯ,null,true);}}else{ʹ=true;foreach(var Â
in ʵ){º=MyFixedPoint.Min(ˑ.Value,(MyFixedPoint)((float)Â.ɼ()/µ.Value.Type.GetItemInfo().Volume));if(ș.TransferItemTo(Â,ǯ,
null,true,º)){ˑ-=º;}else{ʹ=false;}}}}return ʹ;}void ͽ(string Ύ){if(Ύ.StartsWith("ID:")){ˆ=Ύ.Substring(3).Split('|');ˈ=ˇ.
GetBlockWithId(long.Parse(ˆ[0]));if(ˈ!=null){Ŝ(ˈ);}else{ǁ=Vector3.Zero;ǁ.X=float.Parse(ˆ[1]);ǁ.Y=float.Parse(ˆ[2]);ǁ.Z=float.Parse(ˆ[3
]);ˉ=999999f;foreach(var Ŕ in t){Ā=Vector3.RectangularDistance(ʩ.Position+ǁ,Ŕ.Position);if(Ā<ˉ){ˈ=Ŕ;ˉ=Ā;}}if(ˉ<999999f){Ŝ
(ˈ);ƫ.Add(ˈ,this);}}}else if(Ύ.StartsWith("G:")){var Ώ=ˇ.GetBlockGroupWithName(Ύ.Substring(2));var Ȧ=new List<
IMyTerminalBlock>();Ώ.GetBlocks(Ȧ);foreach(var ľ in Ȧ){Ŝ(ľ);}}else{ˈ=ˇ.GetBlockWithName(Ύ);if(ˈ!=null){Ŝ(ˈ);}}}string ʹ(IMyTerminalBlock
Ŕ){return$"ID:{Ŕ.EntityId.ToString()}|{ʩ.Position.X-Ŕ.Position.X}|{ʩ.Position.Y-Ŕ.Position.Y}|{ʩ.Position.Z-Ŕ.Position.Z}|{Ŕ.CustomName}"
;}public void Ȉ(Dictionary<MyItemType,V>Ç){if(ȇ==null)return;RectangleF ô=new RectangleF((ȇ.TextureSize-ȇ.SurfaceSize)/2f
,ȇ.SurfaceSize);var ǌ=ȇ.DrawFrame();var Ǎ=ô.Position;ˋ=ʩ.CustomData.Split('\n');int Ʒ=16;int Ƹ=0;float ƶ=1f;MyIni j=new
MyIni();foreach(var Ƨ in ˋ){if(Ƨ.StartsWith("@")||Ƨ.StartsWith("~"))continue;Ƹ++;X.ǵ(Ƨ,ref j);if(Ƹ==1){Ʒ=j.ǎ("Margin").
ToInt32(16);Ǎ.X+=Ʒ;Ǎ.Y+=Ʒ;}if(Ƨ.StartsWith("Size=")){ƶ=j.ǎ("Size").ToSingle(1f);continue;}if(Ƨ.StartsWith("StockVolumeFull")){Ȅ
(ȇ,ô,ǌ,Ǎ,Ʒ,j.ǎ("Color").ToInt32(0)-1);Ǎ=new Vector2(R.Ǘ(ô,Ʒ),Ǎ.Y+30*ƶ);}else if(Ƨ.StartsWith("StockVolume")){ȃ(ȇ,ǌ,Ǎ,ƶ,Ʒ,
j.ǎ("Color").ToInt32(0)-1);Ǎ=new Vector2(R.Ǘ(ô,Ʒ),Ǎ.Y+30*ƶ);}if(Ƨ.StartsWith("StockLevelFull")){Ȃ(ȇ,ô,ǌ,Ǎ,Ʒ,j.ǎ("Color").
ToInt32(0)-1);Ǎ=new Vector2(R.Ǘ(ô,Ʒ),Ǎ.Y+30*ƶ);}else if(Ƨ.StartsWith("StockLevel")){ȁ(ȇ,ǌ,Ǎ,ƶ,Ʒ,j.ǎ("Color").ToInt32(0)-1);Ǎ=
new Vector2(R.Ǘ(ô,Ʒ),Ǎ.Y+30*ƶ);}if(Ƨ.StartsWith("StockList")){ȅ(ȇ,ô,ǌ,Ǎ,ƶ,Ʒ,Ç);}if(Ƨ.StartsWith("StockStats")){Ȁ(ȇ,ǌ,Ǎ,ƶ);Ǎ
=new Vector2(R.Ǘ(ô,Ʒ),Ǎ.Y+30*ƶ);}}ǌ.Dispose();}public void Ȁ(IMyTextSurface ȇ,MySpriteDrawFrame ǌ,Vector2 Ǎ,float ƶ=1f){R
.ǒ(ȇ,ǌ,Ǎ,$"Managed Containers: {ʵ.Count} | LastUpdate: {DateTime.UtcNow.AddHours(X.Y):HH:mm:ss}",ƶ);}public void ȅ(
IMyTextSurface ȇ,RectangleF ô,MySpriteDrawFrame ǌ,Vector2 Ǎ,float ƶ,int Ʒ,Dictionary<MyItemType,V>Ç){Ͱ();foreach(var ΐ in Ƭ){if(ΐ.
Value.ˠ==ʪ.ˍ.ː||ΐ.Value.ˑ==0)continue;if(!Ç.ContainsKey(ΐ.Key))continue;R.ǹ(ȇ,ǌ,Ǎ,Ç[ΐ.Key].Ǵ(),ΐ.Value.ͱ/ΐ.Value.ˑ,ƶ,Ʒ);Ǎ=new
Vector2(R.Ǘ(ô,Ʒ),Ǎ.Y+30*ƶ);}}public void ȁ(IMyTextSurface ȇ,MySpriteDrawFrame ǌ,Vector2 Ǎ,float ƶ,int Ʒ,int ʝ){R.ǹ(ȇ,ǌ,Ǎ,
"Textures\\FactionLogo\\Builders\\BuilderIcon_15.dds",Ͳ(),ƶ,Ʒ,ʝ:ʝ);}public void ȃ(IMyTextSurface ȇ,MySpriteDrawFrame ǌ,Vector2 Ǎ,float ƶ,int Ʒ,int ʝ){R.ǹ(ȇ,ǌ,Ǎ,
"Textures\\FactionLogo\\Builders\\BuilderIcon_1.dds",1f-(float)ˣ().ɿ(ˤ()),ƶ,Ʒ,ʝ:ʝ);}public void Ȃ(IMyTextSurface ȇ,RectangleF ô,MySpriteDrawFrame ǌ,Vector2 Ǎ,int Ʒ,int ʝ){R
.Ǚ(ȇ,ǌ,Ǎ,Ͳ(),Ʒ:Ʒ,ʞ:ô.Height,ʝ:ʝ);}public void Ȅ(IMyTextSurface ȇ,RectangleF ô,MySpriteDrawFrame ǌ,Vector2 Ǎ,int Ʒ,int ʝ){
R.Ǚ(ȇ,ǌ,Ǎ,1f-(float)ˣ().ɿ(ˤ()),Ʒ:Ʒ,ʞ:ô.Height,ʝ:ʝ);}}public class ʪ{public V ŵ;public int ˑ;public bool Ň=false;string[]ɗ
;public string Ά;public float ͱ=0;public enum ˍ{ˎ=0,ˢ=1,ˡ=2,ː=3}public ˍ ˠ;public bool Ͷ=false;public ʪ(V ŵ,int ˑ,ˍ ˠ){
this.ŵ=ŵ;this.ˑ=ˑ;this.ˠ=ˠ;Ň=true;}public ʪ(string ɗ,Dictionary<string,V>ͳ){this.ɗ=ɗ.Split('=');if(this.ɗ.Length!=2)return;Ά
=this.ɗ[0];Α(this.ɗ[1]);if(ͳ.ContainsKey(this.ɗ[0])){ŵ=ͳ[this.ɗ[0]];Ň=true;}}public override string ToString(){return
$"{(Ň?ŵ.ŏ:Ά)}={Β()}";}string Β(){switch(ˠ){case ˍ.ˢ:return$"{ˑ}M{(Ͷ?"P":"")}";case ˍ.ˡ:return$"{ˑ}L{(Ͷ?"P":"")}";case ˍ.ː:return
$"All{(Ͷ?"P":"")}";default:return$"{ˑ.ToString()}{(Ͷ?"P":"")}";}}void Α(string Γ){if(Γ.Contains("P")){Ͷ=true;Γ=Γ.Replace("P","");}else{Ͷ=
false;}if(Γ.ToLower()=="all"){ˠ=ˍ.ː;}else if(Γ.ToLower().EndsWith("m")){ˠ=ˍ.ˢ;int.TryParse(Γ.Substring(0,Γ.Length-1),out ˑ);}
else if(Γ.ToLower().EndsWith("l")){ˠ=ˍ.ˡ;int.TryParse(Γ.Substring(0,Γ.Length-1),out ˑ);}else{int.TryParse(Γ,out ˑ);}}}public
static class ĵ{static IMyCubeGrid Δ;static Dictionary<IMyCubeGrid,Ε>Ζ=new Dictionary<IMyCubeGrid,Ε>();static string[]Η;static
string[]Θ;public static void Ķ(IMyCubeGrid t,string[]Η,string[]Θ){Δ=t;ĵ.Η=Η;ĵ.Θ=Θ;Ζ.Clear();Ζ.Add(Δ,new Ε{Ι=false,Κ=false,Λ=
true});}public static void ĸ(IMyShipConnector ķ,bool Μ=true){if(!Ζ.ContainsKey(ķ.CubeGrid))Ζ.Add(ķ.CubeGrid,new Ε());if(ķ.
IsConnected&&ķ.OtherConnector!=null){if(Μ)ĸ(ķ.OtherConnector,false);Ζ[ķ.CubeGrid].Ν.Add(new Ξ{t=ķ.OtherConnector.CubeGrid,ķ=ķ});}}
public static void ļ(IMyMechanicalConnectionBlock Ο){if(!Ζ.ContainsKey(Ο.CubeGrid))Ζ.Add(Ο.CubeGrid,new Ε());if(Ο.IsAttached&&
Ο.Top!=null){if(!Ζ[Ο.CubeGrid].Π.Contains(Ο.TopGrid))Ζ[Ο.CubeGrid].Π.Add(Ο.TopGrid);if(!Ζ.ContainsKey(Ο.TopGrid))Ζ.Add(Ο.
TopGrid,new Ε());Ζ[Ο.TopGrid].Π.Add(Ο.CubeGrid);}}public static void Ľ(){Ρ(Δ,Σ:true);}static void Ρ(IMyCubeGrid t,
IMyShipConnector ķ=null,IMyCubeGrid Τ=null,bool Σ=false){if(!Ζ[t].Υ&&(!Σ||Τ==null)){Ζ[t].Υ=true;if(Τ!=null&&ķ!=null){if(!ķ.CustomName.Ĺ(
Η)&&!ķ.OtherConnector.CustomName.Ĺ(Η)){Ζ[t].Κ=Ζ[Τ].Κ;}else{Ζ[t].Κ=true;}if(!ķ.CustomName.Ĺ(Θ)&&!ķ.OtherConnector.
CustomName.Ĺ(Θ)){Ζ[t].Ι=Ζ[Τ].Ι;}else{Ζ[t].Ι=true;}}else if(Τ!=null&&Ζ[t].Λ){Ζ[t].Κ=false;Ζ[t].Ι=false;}foreach(var Φ in Ζ[t].Ν){Ρ(
Φ.t,Φ.ķ,t);}}if(!Ζ[t].Χ&&Σ){Ζ[t].Χ=true;if(Τ!=null){Ζ[t].Λ=Ζ[Τ].Λ;}else{Ζ[t].Λ=true;}if(Ζ[t].Λ){Ζ[t].Κ=false;Ζ[t].Ι=false
;}foreach(var Φ in Ζ[t].Π){Ρ(Φ,null,t,true);foreach(var Ψ in Ζ[t].Ν){Ρ(Ψ.t,Ψ.ķ,t);}}}}public static bool ŀ(IMyCubeGrid t)
{if(Ζ.ContainsKey(t))return Ζ[t].Ι;return true;}public static bool Ł(IMyCubeGrid t){if(Ζ.ContainsKey(t))return Ζ[t].Κ;
return true;}public static bool Ŀ(IMyCubeGrid t){if(Ζ.ContainsKey(t))return Ζ[t].Λ;return false;}}public class Ε{public bool Λ
=false;public bool Ι=true;public bool Κ=true;public bool Υ=false;public bool Χ=false;public List<Ξ>Ν=new List<Ξ>();public
List<IMyCubeGrid>Π=new List<IMyCubeGrid>();}public class Ξ{public IMyCubeGrid t;public IMyShipConnector ķ;}public static
class X{public static double Y;public static string ʙ(float Ω){if(Math.Abs(Ω)>=1000000000)return ʙ(Ω/1000000000)+"G";if(Math.
Abs(Ω)>=1000000)return ʙ(Ω/1000000)+"M";if(Math.Abs(Ω)>=100000)return ʙ(Ω/1000)+"K";if(Math.Abs(Ω)>=10000)return(Ω/1000D).
ToString("0.#")+"K";return Ω.ToString("#,0");}public static string ɢ(float Ω){return Ω.ToString("N0").Replace(",","");}public
static bool Ǌ(string ɤ,ref MyIni ɵ){ɵ.Clear();return ɵ.TryParse($"[Config]\n{ɤ}");}public static bool ǵ(string ɤ,ref MyIni ɵ){
ɵ.Clear();var Ϊ=ɤ.Split(',');var Ϋ="[Config]\n";foreach(var Ƨ in Ϊ){if(Ƨ.Contains("="))Ϋ+=Ƨ+"\n";}return ɵ.TryParse(Ϋ);}
public static int Ź(IMyTerminalBlock ľ){int Ŵ=100;string[]Ȥ=ľ.DetailedInfo.Split(':');if(Ȥ.Length<6)return 100;string ά=Ȥ[5];
int ǯ=ά.IndexOf('%');if(ǯ==-1)return 100;ά=ά.Substring(1,ǯ-1);int.TryParse(ά,out Ŵ);return Ŵ;}public static Color ǽ(string
ʝ){if(ʝ.Length==6){return new Color(int.Parse(ʝ.Substring(0,2),System.Globalization.NumberStyles.HexNumber),int.Parse(ʝ.
Substring(2,2),System.Globalization.NumberStyles.HexNumber),int.Parse(ʝ.Substring(4,2),System.Globalization.NumberStyles.
HexNumber));}return Color.Black;}public static void ƌ(List<IMyInventory>ʵ){List<MyFixedPoint>έ=new List<MyFixedPoint>();
MyFixedPoint ή=MyFixedPoint.Zero;List<MyInventoryItem>z=new List<MyInventoryItem>();Dictionary<MyItemType,MyFixedPoint>Ǉ=new
Dictionary<MyItemType,MyFixedPoint>();Dictionary<int,Dictionary<MyItemType,MyFixedPoint>>Ç=new Dictionary<int,Dictionary<
MyItemType,MyFixedPoint>>();MyFixedPoint ί;MyFixedPoint ΰ;for(int Ļ=0;Ļ<ʵ.Count;Ļ++){έ.Add(ʵ[Ļ].MaxVolume);ή+=ʵ[Ļ].MaxVolume;Ç.Add
(Ļ,new Dictionary<MyItemType,MyFixedPoint>());z.Clear();ʵ[Ļ].GetItems(z);foreach(MyInventoryItem ª in z){if(Ǉ.ContainsKey
(ª.Type)){Ǉ[ª.Type]+=ª.Amount;}else{Ǉ.Add(ª.Type,ª.Amount);}if(Ç[Ļ].ContainsKey(ª.Type)){Ç[Ļ][ª.Type]+=ª.Amount;}else{Ç[Ļ
].Add(ª.Type,ª.Amount);}}}for(int Ļ=0;Ļ<ʵ.Count;Ļ++){έ[Ļ]=έ[Ļ].ɿ(ή);}for(int Ļ=0;Ļ<ʵ.Count;Ļ++){z.Clear();ʵ[Ļ].GetItems(z
);for(int Ǫ=0;Ǫ<z.Count;Ǫ++){if(!Ǉ.ContainsKey(z[Ǫ].Type))continue;ί=Ǉ[z[Ǫ].Type]*έ[Ļ];if(ί>z[Ǫ].Amount)continue;ΰ=z[Ǫ].
Amount-ί;for(int α=0;α<ʵ.Count;α++){if(Ļ==α)continue;ί=MyFixedPoint.Min(Ǉ[z[Ǫ].Type]*έ[α],ΰ);if(Ç[α].ContainsKey(z[Ǫ].Type))ί
-=Ç[α][z[Ǫ].Type];if(!z[Ǫ].Type.GetItemInfo().UsesFractions)ί=MyFixedPoint.Floor(ί);if(ί<=0)continue;if(ʵ[Ļ].
TransferItemTo(ʵ[α],Ǫ,null,true,ί)){ΰ-=ί;if(Ç[α].ContainsKey(z[Ǫ].Type)){Ç[α][z[Ǫ].Type]+=ί;}else{Ç[α].Add(z[Ǫ].Type,ί);}if(Ç[Ļ].
ContainsKey(z[Ǫ].Type))Ç[Ļ][z[Ǫ].Type]-=ί;}}}}}