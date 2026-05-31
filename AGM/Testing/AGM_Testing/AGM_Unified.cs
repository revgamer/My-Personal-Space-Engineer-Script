// =============================================================================
// AGM UNIFIED — AutoGrid Manager (All-in-One)
// Merges: Core + Power + Logistics + Production into a single PB script
// Author: RevGamer | Version: 1.0-unified | Testing build
//
// CustomData commands (on LCD blocks):
//   CoreDashboard | PowerDashboard | LogisticsDashboard | ProductionDashboard
//   ComponentStock | OreStock | IngotStock | AmmoStock | ToolStock | BottleStock
//   Autocrafting | FuelLifeSupport
// =============================================================================

using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRageMath;
using VRage.Game.GUI.TextPanel;
using VRage.Collections;

namespace Script
{
    public sealed class Program : MyGridProgram
    {
        private const string VERSION    = "1.0";
        private const string SCREEN_TAG = "[AGM-S]";
        private static readonly StringComparison SC = StringComparison.OrdinalIgnoreCase;

        private readonly Color COL_BG        = new Color(1,8,13);
        private readonly Color COL_PANEL     = new Color(2,18,28);
        private readonly Color COL_PANEL2    = new Color(3,58,78);
        private readonly Color COL_ACCENT    = new Color(38,239,255);
        private readonly Color COL_ACCENT2   = new Color(112,247,255);
        private readonly Color COL_TEXT      = new Color(126,246,255);
        private readonly Color COL_DIM       = new Color(44,177,195);
        private readonly Color COL_ROW_TEXT  = new Color(126,246,255);
        private readonly Color COL_ROW_DIM   = new Color(63,207,222);
        private readonly Color COL_OK        = new Color(97,255,214);
        private readonly Color COL_WARN      = new Color(255,202,34);
        private readonly Color COL_BAD       = new Color(255,79,66);
        private readonly Color COL_PROG_BG   = new Color(18,48,32);
        private readonly Color COL_PROG_FILL = new Color(255,204,36);

        private class StockEntry  { public string Category,Name,Icon; public double Amount; }
        private class CargoInfo   { public IMyTerminalBlock Block; public IMyInventory Inv; public string Type; public int Index; public bool Locked,Hidden,Manual; }
        private class SourceInfo  { public IMyTerminalBlock Block; public IMyInventory Inv; public string Type; }
        private class PowerProfile{ public string Name="Base",Batteries="",Reactors="",Solar="",Wind="",Hydrogen=""; public bool IncludeUngrouped=false; }
        private class PowerStats  { public int Batteries,Reactors,Solar,Wind,Hydrogen,Producers; public double Stored,Capacity,Input,Output,MaxOutput; }

        private readonly List<IMyTerminalBlock> _blocks  = new List<IMyTerminalBlock>();
        private readonly List<IMyTerminalBlock> _screens = new List<IMyTerminalBlock>();
        private readonly List<MyInventoryItem>  _invItems= new List<MyInventoryItem>();
        private readonly List<IMyAirVent>       _vents   = new List<IMyAirVent>();
        private readonly StringBuilder          _sb      = new StringBuilder();

        private int    _drawTick   = 0;
        private int    _tickCount  = 0;
        private bool   _booting     = true;
        private double _bootElapsed = 0.0;
        private int    _bootDots    = 0;
        private double _bootDotTimer= 0.0;

        private bool   _globalPause       = false;
        private bool   _includeDockedGrids = false;
        private string _noSortTag  = "[No Sorting]";
        private string _lockedTag  = "{Locked}";
        private string _manualTag  = "{Manual}";
        private string _hiddenTag  = "{Hidden}";

        private readonly List<PowerProfile>       _powerProfiles = new List<PowerProfile>();
        private readonly List<IMyTerminalBlock>   _groupBuf      = new List<IMyTerminalBlock>();
        private readonly HashSet<long>            _selectedIds   = new HashSet<long>();
        private bool   _powerEnabled = true;

        private readonly List<CargoInfo>  _cargos  = new List<CargoInfo>();
        private readonly List<SourceInfo> _sources = new List<SourceInfo>();
        private bool   _logisticsEnabled = true;
        private bool   _autoAssign       = true;
        private int    _maxMoves         = 2;
        private int    _srcIndex         = 0;
        private int    _lastMoves        = 0;
        private string _lastItem="",_lastFrom="",_lastTo="",_logWarning="";
        private string _logStatus = "boot";

        private string _fuelGenerators="",_fuelH2Tanks="",_fuelO2Tanks="";
        private bool   _fuelUngrouped = true;

        private readonly List<IMyAssembler>        _assemblers       = new List<IMyAssembler>();
        private readonly List<IMyRefinery>         _refineries       = new List<IMyRefinery>();
        private readonly List<MyProductionItem>    _queue            = new List<MyProductionItem>();
        private readonly List<string>              _refineryPriority = new List<string>();
        private readonly List<string>              _assemblerPriority= new List<string>();
        private readonly Dictionary<string,double> _compQuotas  = new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string,double> _compStock   = new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string,double> _compQueued  = new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase);
        private bool   _prodEnabled=true,_monitorOnly=true,_autocraftComps=true,_sortAsmQueue=true,_sortRefInput=true;
        private int    _maxQueuePerRun=2,_maxQueueAmount=500,_lastQueued=0,_lastAsmMoves=0,_lastRefMoves=0;
        private string _lastProdAction="",_prodWarning="",_prodStatus="boot";

        private readonly List<StockEntry>            _stockEntries = new List<StockEntry>();
        private readonly Dictionary<string,StockEntry> _stockByKey = new Dictionary<string,StockEntry>(StringComparer.OrdinalIgnoreCase);

        // =========================================================================
        // ENTRY POINTS
        // =========================================================================
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1 | UpdateFrequency.Update100;
            EnsureConfig();
            Reload();
        }

        public void Save() { }

        public void Main(string argument, UpdateType updateSource)
        {
            string arg = argument == null ? "" : argument.Trim();
            if (arg.Length > 0) HandleArgument(arg);

            if ((updateSource & UpdateType.Update100) != 0)
            { Reload(); RunLogistics(); RunProduction(); return; }

            double dt = Runtime.TimeSinceLastRun.TotalSeconds;
            if (dt <= 0) dt = 0.0167;
            TickPbAnim(dt);

            // heavy work only every 10 ticks
            if ((updateSource & UpdateType.Update1) != 0)
            {
                _tickCount++;
                if (_tickCount >= 10)
                {
                    _tickCount = 0;
                    if (!_booting)
                    {
                        int bi = (_drawTick) * 2;
                        _drawTick = (_drawTick + 1) % 10;
                        if (bi   < _screens.Count) DrawScreen(_screens[bi]);
                        if (bi+1 < _screens.Count) DrawScreen(_screens[bi+1]);
                    }
                }
            }

            if (_booting)
            {
                _bootElapsed += dt; _bootDotTimer += dt;
                if (_bootDotTimer >= 0.4) { _bootDotTimer=0; _bootDots=(_bootDots+1)%4; }
                double prog = Math.Min(1.0, _bootElapsed/4.0);
                DrawBootSurface(Me.GetSurface(0), prog);
                for (int i=0; i<_screens.Count; i++)
                { var prov=_screens[i] as IMyTextSurfaceProvider; if (prov!=null&&prov.SurfaceCount>0) DrawBootSurface(prov.GetSurface(0),prog); }
                if (_bootElapsed >= 4.0) _booting = false;
                return;
            }

            // PB screen animates every tick
            DrawPbScreen();
        }

        // =========================================================================
        // ARGUMENT HANDLER
        // =========================================================================
        private void HandleArgument(string arg)
        {
            if (arg.Equals("reboot",SC)||arg.Equals("boot",SC)) { _booting=true;_bootElapsed=0;_bootDots=0;Reload();return; }
            if (arg.Equals("reload",SC)||arg.Equals("rescan",SC)) { Reload(); return; }
            if (arg.Equals("pause",SC))  { _globalPause=true;  WriteCoreValue("global_pause","true");  return; }
            if (arg.Equals("resume",SC)) { _globalPause=false; WriteCoreValue("global_pause","false"); return; }
        }

        // =========================================================================
        // RELOAD / SCAN
        // =========================================================================
        private void Reload()
        {
            ReadConfig(); ScanBlocks(); BuildCargoAndSources(); BuildProductionLists();
            _stockByKey.Clear(); _stockEntries.Clear();
            Echo("AutoGrid Manager v1.0 | RevGamer");
            Echo("Power:      "+(_powerEnabled?"ONLINE":"OFF"));
            Echo("Logistics:  "+(_logisticsEnabled?"ONLINE":"OFF"));
            Echo("Production: "+(_prodEnabled?"ONLINE":"OFF"));
            Echo("Log: "  +_logStatus.ToUpperInvariant());
            Echo("Prod: " +_prodStatus.ToUpperInvariant());
            Echo("Screens: "+_screens.Count);
        }

        private void ScanBlocks()
        {
            _blocks.Clear(); _screens.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(_blocks, b => _includeDockedGrids||b.IsSameConstructAs(Me));
            for (int i=0; i<_blocks.Count; i++)
            {
                var b=_blocks[i]; if (b==Me) continue;
                if ((b.CustomName.IndexOf(SCREEN_TAG,SC)>=0||HasDashboardCmd(b)) && b is IMyTextSurfaceProvider)
                { var p=b as IMyTextSurfaceProvider; if (p.SurfaceCount>0) _screens.Add(b); }
            }
        }

        // =========================================================================
        // CONFIG
        // =========================================================================
        private void EnsureConfig()
        {
            if (!string.IsNullOrWhiteSpace(Me.CustomData)) return;
            Me.CustomData =
@"[Core]
enabled=true
power=true
logistics=true
production=true
global_pause=false
include_docked_grids=false

[Power:Base]
batteries=G:Base Batteries
reactors=
solar=
wind=
hydrogen=
include_ungrouped=false

[Logistics]
auto_assign=true
max_moves_per_run=2

[FuelLifeSupport]
o2h2_generators=
h2_tanks=
o2_tanks=
include_ungrouped=true

[Production]
monitor_only=true
autocraft_components=true
sort_assembler_queue=true
sort_refinery_input=true
max_queue_per_run=2
max_queue_amount=500

[RefineryPriority]
Stone
Iron
Nickel
Cobalt
Silicon
Magnesium
Silver
Gold
Platinum
Uranium

[AssemblerPriority]
SteelPlate
InteriorPlate
Construction
Computer
Motor
Display
MetalGrid
SmallTube
LargeTube
GravityGenerator
Superconductor

AutoCrafting=Component
SteelPlate=70000
InteriorPlate=70000
Construction=70000
Computer=10000
Motor=15000
MetalGrid=10000
Girder=10000
SmallTube=10000
LargeTube=10000
Display=5000
BulletproofGlass=5000
PowerCell=5000
SolarCell=1000
Detector=1000
RadioCommunication=1000
Medical=200
Reactor=10000
Thrust=12000
GravityGenerator=500
Superconductor=10000
Explosives=500
Canvas=200
ShieldComponent=2000";
        }

        private void ReadConfig()
        {
            _globalPause=false; _includeDockedGrids=false;
            _noSortTag="[No Sorting]"; _lockedTag="{Locked}"; _manualTag="{Manual}"; _hiddenTag="{Hidden}";
            _powerEnabled=true; _logisticsEnabled=true; _prodEnabled=true;
            _autoAssign=true; _maxMoves=2; _monitorOnly=true; _autocraftComps=true;
            _sortAsmQueue=true; _sortRefInput=true; _maxQueuePerRun=2; _maxQueueAmount=500;
            _fuelGenerators=""; _fuelH2Tanks=""; _fuelO2Tanks=""; _fuelUngrouped=true;
            _powerProfiles.Clear(); _refineryPriority.Clear(); _assemblerPriority.Clear(); _compQuotas.Clear();

            PowerProfile activePower=null; string section="";
            string[] lines=Me.CustomData.Split(new char[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries);
            for (int i=0; i<lines.Length; i++)
            {
                string line=StripComment(lines[i]).Trim(); if (line.Length==0) continue;
                if (line.StartsWith("[")&&line.EndsWith("]"))
                {
                    section=line.Substring(1,line.Length-2).Trim(); activePower=null;
                    if (section.StartsWith("Power:",SC)) { activePower=new PowerProfile(); activePower.Name=section.Substring(6).Trim(); if (activePower.Name.Length==0) activePower.Name="Base"; _powerProfiles.Add(activePower); }
                    continue;
                }
                string key,value;
                if (activePower!=null&&TrySplit(line,'=',out key,out value))
                {
                    if      (key.Equals("batteries",SC))        activePower.Batteries=value;
                    else if (key.Equals("reactors",SC))         activePower.Reactors=value;
                    else if (key.Equals("solar",SC))            activePower.Solar=value;
                    else if (key.Equals("wind",SC))             activePower.Wind=value;
                    else if (key.Equals("hydrogen",SC))         activePower.Hydrogen=value;
                    else if (key.Equals("include_ungrouped",SC))activePower.IncludeUngrouped=ParseBool(value,false);
                    continue;
                }
                if (!TrySplit(line,'=',out key,out value))
                { if (section.Equals("RefineryPriority",SC)) _refineryPriority.Add(line); if (section.Equals("AssemblerPriority",SC)) _assemblerPriority.Add(line); continue; }
                if (key.Equals("AutoCrafting",SC)&&value.Trim().Equals("Component",SC)) { section="ComponentQuotas"; continue; }
                if (section.Equals("Core",SC))
                {
                    if      (key.Equals("global_pause",SC))         _globalPause=ParseBool(value,false);
                    else if (key.Equals("include_docked_grids",SC)) _includeDockedGrids=ParseBool(value,false);
                    else if (key.Equals("no_sorting_tag",SC))       _noSortTag=value;
                    else if (key.Equals("locked_tag",SC))           _lockedTag=value;
                    else if (key.Equals("manual_tag",SC))           _manualTag=value;
                    else if (key.Equals("hidden_tag",SC))           _hiddenTag=value;
                    else if (key.Equals("power",SC))                _powerEnabled=ParseBool(value,true);
                    else if (key.Equals("logistics",SC))            _logisticsEnabled=ParseBool(value,true);
                    else if (key.Equals("production",SC))           _prodEnabled=ParseBool(value,true);
                }
                else if (section.Equals("Logistics",SC))
                { if (key.Equals("auto_assign",SC)) _autoAssign=ParseBool(value,true); else if (key.Equals("max_moves_per_run",SC)) int.TryParse(value,out _maxMoves); }
                else if (section.Equals("Production",SC))
                {
                    if      (key.Equals("monitor_only",SC))         _monitorOnly=ParseBool(value,true);
                    else if (key.Equals("autocraft_components",SC)) _autocraftComps=ParseBool(value,true);
                    else if (key.Equals("sort_assembler_queue",SC)) _sortAsmQueue=ParseBool(value,true);
                    else if (key.Equals("sort_refinery_input",SC))  _sortRefInput=ParseBool(value,true);
                    else if (key.Equals("max_queue_per_run",SC))    int.TryParse(value,out _maxQueuePerRun);
                    else if (key.Equals("max_queue_amount",SC))     int.TryParse(value,out _maxQueueAmount);
                }
                else if (section.Equals("ComponentQuotas",SC))
                { double quota; if (double.TryParse(value,out quota)&&quota>0) _compQuotas[key]=quota; }
                else if (section.Equals("FuelLifeSupport",SC))
                {
                    if      (key.Equals("o2h2_generators",SC)) _fuelGenerators=value;
                    else if (key.Equals("h2_tanks",SC))        _fuelH2Tanks=value;
                    else if (key.Equals("o2_tanks",SC))        _fuelO2Tanks=value;
                    else if (key.Equals("include_ungrouped",SC)) _fuelUngrouped=ParseBool(value,true);
                }
            }
            if (_powerProfiles.Count==0) { var p=new PowerProfile(); p.IncludeUngrouped=true; _powerProfiles.Add(p); }
            if (_maxMoves<1)_maxMoves=1; if (_maxMoves>10)_maxMoves=10;
            if (_maxQueuePerRun<1)_maxQueuePerRun=1; if (_maxQueuePerRun>10)_maxQueuePerRun=10;
            if (_maxQueueAmount<1)_maxQueueAmount=1; if (_maxQueueAmount>5000)_maxQueueAmount=5000;
        }

        private void WriteCoreValue(string key, string value)
        {
            string[] lines=Me.CustomData.Split(new char[]{'\r','\n'},StringSplitOptions.None);
            _sb.Clear(); bool inCore=false,wrote=false;
            for (int i=0; i<lines.Length; i++)
            {
                string t=lines[i].Trim();
                if (t.StartsWith("[")&&t.EndsWith("]")) { if (inCore&&!wrote){_sb.AppendLine(key+"="+value);wrote=true;} inCore=t.Equals("[Core]",SC); }
                string k,v;
                if (inCore&&TrySplit(t,'=',out k,out v)&&k.Equals(key,SC)) { _sb.AppendLine(key+"="+value);wrote=true;continue; }
                _sb.AppendLine(lines[i]);
            }
            if (!wrote) _sb.AppendLine(key+"="+value);
            Me.CustomData=_sb.ToString().TrimEnd();
        }

        // =========================================================================
        // LOGISTICS
        // =========================================================================
        private void BuildCargoAndSources()
        {
            _cargos.Clear(); _sources.Clear();
            for (int i=0; i<_blocks.Count; i++)
            {
                var b=_blocks[i]; if (b==null||!b.HasInventory||IsNoSort(b)) continue;
                if (b is IMyCargoContainer)
                {
                    var c=new CargoInfo(); c.Block=b; c.Inv=b.GetInventory(0);
                    c.Type=CargoTypeFromBlock(b); c.Index=CargoNumber(b,c.Type);
                    c.Locked=HasToken(b.CustomName,_lockedTag)||HasToken(b.CustomData,_lockedTag);
                    c.Manual=HasToken(b.CustomName,_manualTag)||HasToken(b.CustomData,_manualTag);
                    c.Hidden=HasToken(b.CustomName,_hiddenTag)||HasToken(b.CustomData,_hiddenTag);
                    _cargos.Add(c); if (!c.Locked&&!c.Manual&&!c.Hidden) AddSource(b,c.Inv,c.Type); continue;
                }
                if (b is IMyReactor||b is IMyGasGenerator||b is IMyGasTank) continue;
                if (HasToken(b.CustomName,_lockedTag)||HasToken(b.CustomName,_manualTag)||HasToken(b.CustomName,_hiddenTag)) continue;
                if (b.InventoryCount>=2&&(b is IMyAssembler||b is IMyRefinery)) AddSource(b,b.GetInventory(1),"");
                else if (b.InventoryCount==1) AddSource(b,b.GetInventory(0),"");
            }
            _cargos.Sort((a,b2)=>((float)b2.Inv.MaxVolume).CompareTo((float)a.Inv.MaxVolume));
        }

        private void AddSource(IMyTerminalBlock b, IMyInventory inv, string type)
        { if (inv!=null) _sources.Add(new SourceInfo{Block=b,Inv=inv,Type=type}); }

        private void RunLogistics()
        {
            _lastMoves=0; _logWarning="";
            if (!_logisticsEnabled){_logStatus="disabled";return;}
            if (_globalPause){_logStatus="paused";return;}
            if (_autoAssign) EnsureBaselineDestinations();
            if (_sources.Count==0){_logStatus="no sources";return;}
            int moves=0; if (_srcIndex<0||_srcIndex>=_sources.Count) _srcIndex=0;
            int checked2=0;
            while (checked2<_sources.Count&&moves<_maxMoves)
            {
                var src=_sources[_srcIndex]; _srcIndex=(_srcIndex+1)%_sources.Count; checked2++;
                _invItems.Clear(); src.Inv.GetItems(_invItems);
                for (int i=_invItems.Count-1; i>=0&&moves<_maxMoves; i--)
                {
                    var item=_invItems[i]; string type=ItemCategory(item.Type);
                    if (type.Length==0||src.Type.Equals(type,SC)) continue;
                    var dest=FindDest(type)??(_autoAssign?AssignCargo(type):null);
                    if (dest==null){_logWarning=type+" storage full";continue;}
                    if (TryMove(src.Inv,dest.Inv,i,item.Amount))
                    { moves++; _lastItem=item.Type.SubtypeId.ToString(); _lastFrom=src.Block.CustomName; _lastTo=dest.Block.CustomName; }
                }
            }
            _lastMoves=moves; _logStatus=moves>0?"moved "+moves:"idle";
        }

        private bool TryMove(IMyInventory src, IMyInventory dst, int idx, MyFixedPoint amt)
        {
            if (src.TransferItemTo(dst,idx,null,true)) return true;
            double a=(double)amt, b=Math.Min(a,1000.0);
            if (b>0&&src.TransferItemTo(dst,idx,null,true,(MyFixedPoint)b)) return true;
            b=Math.Min(a,100.0);
            if (b>0&&src.TransferItemTo(dst,idx,null,true,(MyFixedPoint)b)) return true;
            return src.TransferItemTo(dst,idx,null,true,(MyFixedPoint)1.0);
        }

        private CargoInfo FindDest(string type)
        {
            CargoInfo best=null; int bestN=int.MaxValue;
            for (int i=0; i<_cargos.Count; i++)
            { var c=_cargos[i]; if (c.Locked||c.Manual||c.Hidden||!c.Type.Equals(type,SC)||!HasSpace(c.Inv)) continue; int n=c.Index>0?c.Index:CargoNumber(c.Block,type); if (n<bestN){best=c;bestN=n;} }
            return best;
        }

        private void EnsureBaselineDestinations()
        { string[] types={"Ore","Ingot","Component","Ammo","Tool","Bottle"}; for (int i=0;i<types.Length;i++) if (FindDest(types[i])==null) AssignCargo(types[i]); }

        private CargoInfo AssignCargo(string type)
        {
            for (int i=0; i<_cargos.Count; i++)
            { var c=_cargos[i]; if (c.Locked||c.Manual||c.Hidden||c.Type.Length>0||c.Inv.ItemCount>0) continue; int n=NextCargoNumber(type); c.Block.CustomName=CleanCargoName(c.Block.CustomName)+" {"+TagType(type)+" "+n+"}"; c.Type=type;c.Index=n; return c; }
            return null;
        }

        private int NextCargoNumber(string type)
        { int max=0; for (int i=0;i<_cargos.Count;i++) if (_cargos[i].Type.Equals(type,SC)&&_cargos[i].Index>max) max=_cargos[i].Index; return max+1; }

        private string CargoTypeFromBlock(IMyTerminalBlock b)
        {
            string n=b.CustomName;
            if (HasTagType(n,"Ore")) return "Ore"; if (HasTagType(n,"Ingot")) return "Ingot";
            if (HasTagType(n,"Component")) return "Component"; if (HasTagType(n,"Ammo")) return "Ammo";
            if (HasTagType(n,"Tools")||HasTagType(n,"Tool")) return "Tool"; if (HasTagType(n,"Bottle")) return "Bottle";
            return "";
        }

        private int CargoNumber(IMyTerminalBlock block, string type)
        {
            string tag=TagType(type),name=block.CustomName; int idx=name.IndexOf("{"+tag,SC); if (idx<0) return 999;
            int start=idx+tag.Length+1; while (start<name.Length&&name[start]==' ') start++;
            int end=start; while (end<name.Length&&char.IsDigit(name[end])) end++;
            int n; return int.TryParse(name.Substring(start,end-start),out n)?n:999;
        }

        private bool HasTagType(string name,string tag){return name.IndexOf("{"+tag,SC)>=0||name.IndexOf("["+tag+"]",SC)>=0;}
        private bool HasSpace(IMyInventory inv){return (float)inv.CurrentVolume<(float)inv.MaxVolume*0.98f;}
        private string TagType(string t){return t.Equals("Tool",SC)?"Tools":t;}
        private string CleanCargoName(string n){int i=n.IndexOf("{");return i>=0?n.Substring(0,i).Trim():n.Trim();}

        // =========================================================================
        // PRODUCTION
        // =========================================================================
        private void BuildProductionLists()
        {
            _assemblers.Clear(); _refineries.Clear();
            for (int i=0; i<_blocks.Count; i++)
            {
                var b=_blocks[i]; if (b==null||IsNoSort(b)||HasToken(b.CustomName,_hiddenTag)) continue;
                var asm=b as IMyAssembler; if (asm!=null&&asm.IsFunctional&&!HasToken(asm.CustomName,_manualTag)){_assemblers.Add(asm);continue;}
                var ref2=b as IMyRefinery; if (ref2!=null&&ref2.IsFunctional&&!HasToken(ref2.CustomName,_manualTag)) _refineries.Add(ref2);
            }
        }

        private void RunProduction()
        {
            _lastQueued=0;_lastAsmMoves=0;_lastRefMoves=0;_lastProdAction="";_prodWarning="";
            if (!_prodEnabled){_prodStatus="disabled";return;} if (_globalPause){_prodStatus="paused";return;}
            if (_assemblers.Count==0&&_refineries.Count==0){_prodStatus="no machines";return;}
            UpdateCompStock(); UpdateQueuedComps();
            if (_sortRefInput) _lastRefMoves=SortRefineryInputs();
            if (_sortAsmQueue) _lastAsmMoves=SortAssemblerQueues();
            if (_monitorOnly){_prodStatus="monitoring";return;}
            if (_autocraftComps) _lastQueued=QueueCompQuotas();
            _prodStatus=(_lastQueued+_lastAsmMoves+_lastRefMoves>0)?"active":"idle";
        }

        private void UpdateCompStock()
        {
            _compStock.Clear();
            for (int i=0;i<_blocks.Count;i++)
            { var b=_blocks[i]; if (b==null||!b.HasInventory||IsNoSort(b)||HasToken(b.CustomName,_hiddenTag)) continue;
              for (int inv=0;inv<b.InventoryCount;inv++) { _invItems.Clear(); b.GetInventory(inv).GetItems(_invItems);
                for (int j=0;j<_invItems.Count;j++) { var t=_invItems[j].Type; if (!t.TypeId.ToString().EndsWith("_Component")) continue;
                  string sub=t.SubtypeId.ToString(); double cur; _compStock.TryGetValue(sub,out cur); _compStock[sub]=cur+(double)_invItems[j].Amount; } } }
        }

        private void UpdateQueuedComps()
        {
            _compQueued.Clear();
            for (int i=0;i<_assemblers.Count;i++) { _queue.Clear(); _assemblers[i].GetQueue(_queue);
              for (int q=0;q<_queue.Count;q++) { string item=CompFromBP(_queue[q].BlueprintId); if (item.Length==0) continue;
                double cur; _compQueued.TryGetValue(item,out cur); _compQueued[item]=cur+(double)_queue[q].Amount; } }
        }

        private int QueueCompQuotas()
        {
            int queued=0;
            foreach (var quota in _compQuotas)
            {
                if (queued>=_maxQueuePerRun) break;
                double stock,alreadyQueued; _compStock.TryGetValue(quota.Key,out stock); _compQueued.TryGetValue(quota.Key,out alreadyQueued);
                double need=quota.Value-stock-alreadyQueued; if (need<1) continue;
                MyDefinitionId bp; var asm=FindAsmFor(quota.Key,out bp);
                if (asm==null){_prodWarning="No blueprint for "+quota.Key;continue;}
                double amount=Math.Min(Math.Ceiling(need),_maxQueueAmount);
                asm.AddQueueItem(bp,(MyFixedPoint)amount); queued++; _lastProdAction="Queued "+amount.ToString("0")+" "+quota.Key;
            }
            return queued;
        }

        private IMyAssembler FindAsmFor(string item, out MyDefinitionId bp)
        {
            bp=new MyDefinitionId(); string prefix="MyObjectBuilder_BlueprintDefinition/";
            string[] cands={prefix+item,prefix+item+"Component",prefix+"Position0010_"+item,prefix+"Position0010_"+item+"Component"};
            for (int c=0;c<cands.Length;c++) { MyDefinitionId id; if (!MyDefinitionId.TryParse(cands[c],out id)) continue;
              for (int i=0;i<_assemblers.Count;i++) if (_assemblers[i].CustomName.IndexOf("!disassemble-only",SC)<0&&_assemblers[i].CanUseBlueprint(id)){bp=id;return _assemblers[i];} }
            return null;
        }

        private int SortAssemblerQueues()
        {
            int moved=0;
            for (int i=0;i<_assemblers.Count&&moved<_maxQueuePerRun;i++)
            { _queue.Clear();_assemblers[i].GetQueue(_queue); if (_queue.Count<2) continue; int best=BestAsmQueueIdx(_queue,_assemblers[i].IsProducing); if (best<=0) continue; _assemblers[i].MoveQueueItemRequest(_queue[best].ItemId,0);moved++; }
            return moved;
        }

        private int BestAsmQueueIdx(List<MyProductionItem> q, bool producing)
        { int best=-1,bestP=int.MaxValue; for (int i=producing?0:1;i<q.Count;i++){int p=PriorityIdx(_assemblerPriority,CompFromBP(q[i].BlueprintId));if(p<bestP){bestP=p;best=i;}} return bestP==int.MaxValue?-1:best; }

        private int SortRefineryInputs()
        {
            int moved=0;
            for (int r=0;r<_refineries.Count&&moved<_maxQueuePerRun;r++)
            { var input=_refineries[r].GetInventory(0); _invItems.Clear();input.GetItems(_invItems); if (_invItems.Count<2) continue; int best=BestRefIdx(_invItems); if (best<=0) continue; input.TransferItemTo(input,best,0,true);moved++; }
            return moved;
        }

        private int BestRefIdx(List<MyInventoryItem> items)
        { int best=-1,bestP=int.MaxValue; for (int i=0;i<items.Count;i++){if(!items[i].Type.TypeId.ToString().EndsWith("_Ore"))continue;int p=PriorityIdx(_refineryPriority,items[i].Type.SubtypeId.ToString());if(p<bestP){bestP=p;best=i;}} return bestP==int.MaxValue?-1:best; }

        private int PriorityIdx(List<string> list, string item)
        { for (int i=0;i<list.Count;i++) if (item.IndexOf(list[i],SC)>=0||list[i].IndexOf(item,SC)>=0) return i; return int.MaxValue; }

        private string CompFromBP(MyDefinitionId bp)
        { string s=bp.SubtypeName; if (s.StartsWith("Position",SC)){int idx=s.IndexOf("_");if(idx>=0)s=s.Substring(idx+1);} if (s.EndsWith("Component",SC)) s=s.Substring(0,s.Length-9); return s; }

        // =========================================================================
        // POWER
        // =========================================================================
        private PowerStats BuildPowerStats(PowerProfile prof)
        {
            var st=new PowerStats(); _selectedIds.Clear();
            AddBatteries(st,prof.Batteries,prof.IncludeUngrouped);
            AddProducers(st,prof.Reactors,"reactor",prof.IncludeUngrouped);
            AddProducers(st,prof.Solar,"solar",prof.IncludeUngrouped);
            AddProducers(st,prof.Wind,"wind",prof.IncludeUngrouped);
            AddProducers(st,prof.Hydrogen,"hydrogen",prof.IncludeUngrouped);
            return st;
        }

        private void AddBatteries(PowerStats st, string spec, bool ungrouped)
        {
            for (int i=0;i<_blocks.Count;i++)
            { var b=_blocks[i] as IMyBatteryBlock; if (b==null||!MatchSpec(b,spec,ungrouped)||!_selectedIds.Add(b.EntityId)) continue;
              st.Batteries++;st.Stored+=b.CurrentStoredPower;st.Capacity+=b.MaxStoredPower;st.Input+=b.CurrentInput;st.Output+=b.CurrentOutput; }
        }

        private void AddProducers(PowerStats st, string spec, string kind, bool ungrouped)
        {
            for (int i=0;i<_blocks.Count;i++)
            { var p=_blocks[i] as IMyPowerProducer; var b=_blocks[i];
              if (p==null||p is IMyBatteryBlock||!IsProducerKind(b,kind)||!MatchSpec(b,spec,ungrouped)||!_selectedIds.Add(b.EntityId)) continue;
              st.Producers++;st.Output+=p.CurrentOutput;st.MaxOutput+=p.MaxOutput;
              if (kind=="reactor")st.Reactors++;else if (kind=="solar")st.Solar++;else if (kind=="wind")st.Wind++;else if (kind=="hydrogen")st.Hydrogen++; }
        }

        private bool MatchSpec(IMyTerminalBlock b, string spec, bool ungrouped)
        {
            if (string.IsNullOrWhiteSpace(spec)) return ungrouped; spec=spec.Trim(); if (spec.Length==0) return ungrouped;
            if (spec.StartsWith("G:",SC)) { string grpName=spec.Substring(2).Trim(); if (grpName.Length==0) return ungrouped; var grp=GridTerminalSystem.GetBlockGroupWithName(grpName); if (grp==null) return false; _groupBuf.Clear();grp.GetBlocks(_groupBuf);return _groupBuf.Contains(b); }
            return b.CustomName.IndexOf(spec,SC)>=0;
        }

        private bool IsProducerKind(IMyTerminalBlock b, string kind)
        {
            if (kind=="reactor") return b is IMyReactor; if (kind=="solar") return b is IMySolarPanel;
            string def=b.BlockDefinition.TypeIdString+"/"+b.BlockDefinition.SubtypeId;
            if (kind=="wind") return def.IndexOf("WindTurbine",SC)>=0; if (kind=="hydrogen") return def.IndexOf("HydrogenEngine",SC)>=0;
            return false;
        }

        // =========================================================================
        // STOCK CACHE
        // =========================================================================
        private void BuildStockCache()
        {
            if (_stockEntries.Count>0) return;
            for (int b=0;b<_blocks.Count;b++)
            { var block=_blocks[b]; if (block==null||!block.HasInventory||IsNoSort(block)||HasToken(block.CustomName,_hiddenTag)) continue;
              for (int inv=0;inv<block.InventoryCount;inv++) { _invItems.Clear();block.GetInventory(inv).GetItems(_invItems);
                for (int i=0;i<_invItems.Count;i++) { var t=_invItems[i].Type; string cat=ItemCategory(t);if(cat.Length==0)continue;
                  string name=DisplayName(t),key=cat+"/"+name; StockEntry entry;
                  if (!_stockByKey.TryGetValue(key,out entry)){entry=new StockEntry{Category=cat,Name=name,Icon=ItemIcon(t)};_stockByKey[key]=entry;_stockEntries.Add(entry);}
                  entry.Amount+=(double)_invItems[i].Amount; } } }
        }

        private double StockAmount(string cat, string item)
        { BuildStockCache(); string wanted=NormKey(item); for (int i=0;i<_stockEntries.Count;i++) if (_stockEntries[i].Category.Equals(cat,SC)&&NormKey(_stockEntries[i].Name).Equals(wanted,SC)) return _stockEntries[i].Amount; return 0; }

        // =========================================================================
        // SCREEN ROUTING
        // =========================================================================
        private bool HasDashboardCmd(IMyTerminalBlock b)
        { string d=b.CustomData??""; return d.IndexOf("CoreDashboard",SC)>=0||d.IndexOf("PowerDashboard",SC)>=0||d.IndexOf("LogisticsDashboard",SC)>=0||d.IndexOf("ProductionDashboard",SC)>=0||d.IndexOf("Stock",SC)>=0||d.IndexOf("Autocrafting",SC)>=0||d.IndexOf("FuelLifeSupport",SC)>=0||d.IndexOf("AGM-",SC)>=0; }

        private void DrawScreen(IMyTerminalBlock block)
        {
            var prov=block as IMyTextSurfaceProvider; if (prov==null||prov.SurfaceCount<=0) return;
            var surf=prov.GetSurface(0); string d=block.CustomData??"";
            if      (d.IndexOf("PowerDashboard",SC)>=0||d.IndexOf("AGM-Power",SC)>=0) DrawPowerDash(surf);
            else if (d.IndexOf("LogisticsDashboard",SC)>=0)  DrawLogisticsDash(surf);
            else if (d.IndexOf("ProductionDashboard",SC)>=0) DrawProductionDash(surf);
            else if (d.IndexOf("FuelLifeSupport",SC)>=0||d.IndexOf("LifeSupport",SC)>=0) DrawFuelDash(surf);
            else if (d.IndexOf("Autocrafting",SC)>=0||d.IndexOf("AutoCrafting",SC)>=0) DrawAutocraftDash(surf,DashPage(block,"Autocrafting"));
            else { string sk=StockKind(block); if (sk.Length>0) DrawStockDash(surf,sk,StockPage(block,sk)); else DrawCoreDash(surf); }
        }

        private void DrawPbScreen()
        {
            var surf=Me.GetSurface(0); if (surf==null) return;
            PrepSurf(surf); var vp=VP(surf); var panel=Inset(vp,10f);
            using (var fr=surf.DrawFrame())
            {
                Fill(fr,vp,COL_BG);Fill(fr,panel,COL_PANEL);DrawBorder(fr,panel,COL_ACCENT,3f);
                var cx=panel.X+panel.Width*0.5f;
                Txt(fr,"AUTOGRID MANAGER",cx,panel.Y+24f,COL_ACCENT2,0.85f,TextAlignment.CENTER);
                Txt(fr,"RevGamer",cx,panel.Y+54f,COL_TEXT,0.42f,TextAlignment.CENTER);
                // hex animation in centre
                var animArea = new RectangleF(panel.X, panel.Y+70f, panel.Width, 130f);
                DrawHexAnim(fr, animArea);
                float y=panel.Y+220f;
                Row(fr,panel,y,"Power",      _powerEnabled?"ONLINE":"OFF",      _powerEnabled?COL_OK:COL_DIM);y+=30f;
                Row(fr,panel,y,"Logistics",  _logisticsEnabled?"ONLINE":"OFF",  _logisticsEnabled?COL_OK:COL_DIM);y+=30f;
                Row(fr,panel,y,"Production", _prodEnabled?"ONLINE":"OFF",       _prodEnabled?COL_OK:COL_DIM);y+=30f;
                Row(fr,panel,y,"Log",   _logStatus.ToUpperInvariant(),COL_TEXT);y+=30f;
                Row(fr,panel,y,"Prod",  _prodStatus.ToUpperInvariant(),COL_TEXT);y+=30f;
                Row(fr,panel,y,"Screens",_screens.Count.ToString(),COL_TEXT);
                Txt(fr,"AutoGrid Manager v1.0",cx,panel.Bottom-18f,COL_DIM,0.34f,TextAlignment.CENTER);
            }
        }

        // =========================================================================
        // DASHBOARD DRAWS
        // =========================================================================
        private void DrawCoreDash(IMyTextSurface s)
        {
            PrepSurf(s);var vp=VP(s);var panel=Inset(vp,10f);
            using (var fr=s.DrawFrame())
            {
                Fill(fr,vp,COL_BG);Fill(fr,panel,COL_PANEL);DrawBorder(fr,panel,COL_ACCENT,3f);
                Txt(fr,"CORE STATUS",panel.X+24f,panel.Y+24f,COL_ACCENT2,0.95f,TextAlignment.LEFT);
                float y=panel.Y+72f;
                Row(fr,panel,y,"Global pause",_globalPause?"ON":"OFF",_globalPause?COL_WARN:COL_OK);y+=32f;
                Row(fr,panel,y,"Power",   _powerEnabled?"ON":"OFF",  _powerEnabled?COL_OK:COL_DIM);y+=32f;
                Row(fr,panel,y,"Logistics",_logisticsEnabled?"ON":"OFF",_logisticsEnabled?COL_OK:COL_DIM);y+=32f;
                Row(fr,panel,y,"Production",_prodEnabled?"ON":"OFF",_prodEnabled?COL_OK:COL_DIM);y+=32f;
                Row(fr,panel,y,"Log",_logStatus.ToUpperInvariant(),COL_TEXT);y+=32f;
                Row(fr,panel,y,"Prod",_prodStatus.ToUpperInvariant(),COL_TEXT);y+=32f;
                Row(fr,panel,y,"Screens",_screens.Count.ToString(),COL_TEXT);
                Txt(fr,"AutoGrid Manager v1.0",panel.X+24f,panel.Bottom-24f,COL_DIM,0.36f,TextAlignment.LEFT);
            }
        }

        private void DrawPowerDash(IMyTextSurface s)
        {
            PrepSurf(s);
            var prof=_powerProfiles.Count>0?_powerProfiles[0]:new PowerProfile();
            if (string.IsNullOrWhiteSpace(prof.Batteries)&&string.IsNullOrWhiteSpace(prof.Reactors)&&string.IsNullOrWhiteSpace(prof.Solar)&&string.IsNullOrWhiteSpace(prof.Wind)&&string.IsNullOrWhiteSpace(prof.Hydrogen)) prof.IncludeUngrouped=true;
            var st=BuildPowerStats(prof);
            double bPct=st.Capacity>0?st.Stored/st.Capacity:0, oPct=st.MaxOutput>0?st.Output/st.MaxOutput:0;
            var vp=VP(s);var panel=Inset(vp,10f);
            using (var fr=s.DrawFrame())
            {
                Fill(fr,vp,COL_BG);Fill(fr,panel,COL_PANEL);DrawBorder(fr,panel,COL_ACCENT,3f);
                Txt(fr,"POWER",panel.X+24f,panel.Y+24f,COL_ACCENT2,0.92f,TextAlignment.LEFT);
                Txt(fr,_powerEnabled?"ONLINE":"OFF",panel.Right-24f,panel.Y+28f,_powerEnabled?COL_OK:COL_BAD,0.48f,TextAlignment.RIGHT);
                float y=panel.Y+72f;
                Row(fr,panel,y,"Profile",prof.Name,COL_TEXT);y+=32f;
                Row(fr,panel,y,"Batteries",st.Batteries+" | "+Pct(bPct),BatCol(bPct));y+=32f;
                Row(fr,panel,y,"Stored",FmtPow(st.Stored)+"Wh / "+FmtPow(st.Capacity)+"Wh",COL_ACCENT2);y+=32f;
                Row(fr,panel,y,"Output",FmtPow(st.Output)+"W / "+FmtPow(st.MaxOutput)+"W",OutCol(oPct));y+=32f;
                string src2=""; if (st.Reactors>0)src2+="Reactors:"+st.Reactors+" "; if (st.Solar>0)src2+="Solar:"+st.Solar+" "; if (st.Wind>0)src2+="Wind:"+st.Wind+" "; if (st.Hydrogen>0)src2+="H2:"+st.Hydrogen; if (src2.Length==0)src2="None";
                Row(fr,panel,y,"Sources",src2.Trim(),st.Producers>0?COL_TEXT:COL_DIM);y+=32f;
                Row(fr,panel,y,"Input",FmtPow(st.Input)+"W",COL_TEXT);
                Txt(fr,"AutoGrid Manager v1.0",panel.X+panel.Width*0.5f,panel.Bottom-18f,COL_DIM,0.34f,TextAlignment.CENTER);
            }
        }

        private void DrawLogisticsDash(IMyTextSurface s)
        {
            PrepSurf(s);var vp=VP(s);var panel=Inset(vp,10f);
            using (var fr=s.DrawFrame())
            {
                Fill(fr,vp,COL_BG);Fill(fr,panel,COL_PANEL);DrawBorder(fr,panel,COL_ACCENT,3f);
                Txt(fr,"LOGISTICS",panel.X+24f,panel.Y+24f,COL_ACCENT2,0.95f,TextAlignment.LEFT);
                Txt(fr,_logisticsEnabled?"ONLINE":"OFF",panel.Right-24f,panel.Y+28f,_logisticsEnabled?COL_OK:COL_BAD,0.44f,TextAlignment.RIGHT);
                float y=panel.Y+72f;
                Row(fr,panel,y,"State",_logStatus.ToUpperInvariant(),_logisticsEnabled?COL_OK:COL_DIM);y+=32f;
                Row(fr,panel,y,"Cargo",_cargos.Count.ToString(),COL_TEXT);y+=32f;
                Row(fr,panel,y,"Sources",_sources.Count.ToString(),COL_TEXT);y+=32f;
                DrawLogTypeRow(fr,panel,y,"Ore/Ingot","Ore","Ingot");y+=32f;
                DrawLogTypeRow(fr,panel,y,"Component","Component","");y+=32f;
                DrawLogTypeRow(fr,panel,y,"Ammo/Tool","Ammo","Tool");y+=32f;
                DrawLogTypeRow(fr,panel,y,"Bottle","Bottle","");y+=32f;
                Row(fr,panel,y,"Moved/run",_lastMoves+"/"+_maxMoves,_lastMoves>0?COL_OK:COL_DIM);y+=32f;
                Row(fr,panel,y,"Last item",_lastItem.Length>0?_lastItem:"-",COL_TEXT);y+=32f;
                Row(fr,panel,y,"From",_lastFrom.Length>0?Trim(_lastFrom,28):"-",COL_TEXT);y+=32f;
                Row(fr,panel,y,"To",_lastTo.Length>0?Trim(_lastTo,28):"-",COL_TEXT);
                if (_logWarning.Length>0){y+=32f;Row(fr,panel,y,"Warning",_logWarning,COL_BAD);}
                Txt(fr,"AutoGrid Manager v1.0",panel.X+24f,panel.Bottom-24f,COL_DIM,0.34f,TextAlignment.LEFT);
            }
        }

        private void DrawProductionDash(IMyTextSurface s)
        {
            PrepSurf(s);var vp=VP(s);var panel=Inset(vp,10f);
            using (var fr=s.DrawFrame())
            {
                Fill(fr,vp,COL_BG);Fill(fr,panel,COL_PANEL);DrawBorder(fr,panel,COL_ACCENT,3f);
                Txt(fr,"PRODUCTION",panel.X+24f,panel.Y+24f,COL_ACCENT2,0.80f,TextAlignment.LEFT);
                Txt(fr,_prodEnabled?"ONLINE":"OFF",panel.Right-24f,panel.Y+28f,_prodEnabled?COL_OK:COL_BAD,0.48f,TextAlignment.RIGHT);
                float y=panel.Y+72f;
                Row(fr,panel,y,"State",_prodStatus.ToUpperInvariant(),_prodEnabled?COL_OK:COL_DIM);y+=32f;
                Row(fr,panel,y,"Mode",_monitorOnly?"MONITOR ONLY":"ACTIVE",_monitorOnly?COL_DIM:COL_OK);y+=32f;
                Row(fr,panel,y,"Assemblers",ProdAsmProducing()+"/"+_assemblers.Count,COL_ACCENT2);y+=32f;
                Row(fr,panel,y,"Queued",ProdAsmQueued()+" machines",COL_TEXT);y+=32f;
                Row(fr,panel,y,"Refineries",ProdRefProducing()+"/"+_refineries.Count,COL_ACCENT2);y+=32f;
                Row(fr,panel,y,"Ref input",ProdRefInputFill().ToString("0.0")+"%",COL_TEXT);y+=32f;
                Row(fr,panel,y,"Autocraft",_lastQueued+" queued",COL_TEXT);
                if (_prodWarning.Length>0){y+=32f;Row(fr,panel,y,"Warning",_prodWarning,COL_BAD);}
                Txt(fr,"AutoGrid Manager v1.0",panel.X+24f,panel.Bottom-24f,COL_DIM,0.34f,TextAlignment.LEFT);
            }
        }

        private void DrawStockDash(IMyTextSurface s, string cat, int page)
        {
            BuildStockCache();
            var filtered=new List<StockEntry>(); for (int i=0;i<_stockEntries.Count;i++) if (cat.Equals("Inventory",SC)||_stockEntries[i].Category.Equals(cat,SC)) filtered.Add(_stockEntries[i]);
            filtered.Sort((a,b)=>{int c=a.Category.CompareTo(b.Category);return c!=0?c:a.Name.CompareTo(b.Name);});
            PrepSurf(s);var vp=VP(s);var panel=Inset(vp,10f);
            using (var fr=s.DrawFrame())
            {
                Fill(fr,vp,COL_BG);Fill(fr,panel,COL_PANEL);DrawBorder(fr,panel,COL_ACCENT,3f);
                int rows=Math.Max(1,(int)((panel.Height-116f)/34f)),pages=Math.Max(1,(int)Math.Ceiling(filtered.Count/(double)rows));
                if (page<1)page=1;if (page>pages)page=pages;int start=(page-1)*rows,end=Math.Min(filtered.Count,start+rows);
                Txt(fr,cat.ToUpperInvariant()+" STOCK",panel.X+24f,panel.Y+24f,COL_ACCENT2,0.85f,TextAlignment.LEFT);
                Txt(fr,"P"+page+"/"+pages,panel.Right-24f,panel.Y+28f,COL_DIM,0.44f,TextAlignment.RIGHT);
                float y=panel.Y+70f; for (int i=start;i<end;i++){DrawStockRow(fr,panel,y,filtered[i]);y+=34f;}
                Txt(fr,"AutoGrid Manager v1.0",panel.X+24f,panel.Bottom-24f,COL_DIM,0.36f,TextAlignment.LEFT);
            }
        }

        private void DrawStockRow(MySpriteDrawFrame fr, RectangleF panel, float y, StockEntry e)
        {
            var row=new RectangleF(panel.X+16f,y,panel.Width-32f,28f); Fill(fr,row,COL_PANEL2);DrawBorder(fr,row,COL_DIM,1f);
            double quota=StockQuota(e);double pct=quota>0?Math.Min(1.0,e.Amount/quota):0.0; Color bc=pct>=0.35?COL_PROG_FILL:COL_WARN;
            string icon=string.IsNullOrEmpty(e.Icon)?"IconInventory":e.Icon;
            fr.Add(new MySprite(SpriteType.TEXTURE,icon,new Vector2(row.X+14f,row.Y+14f),new Vector2(20f,20f),COL_ROW_TEXT));
            Txt(fr,Trim(e.Name,20),row.X+30f,y+5f,COL_ROW_TEXT,0.43f,TextAlignment.LEFT);
            Txt(fr,FmtAmt(e.Amount),row.Right-108f,y+5f,COL_ROW_TEXT,0.43f,TextAlignment.RIGHT);
            var bar=new RectangleF(row.Right-96f,y+8f,82f,10f); Fill(fr,bar,COL_PROG_BG);
            Fill(fr,new RectangleF(bar.X,bar.Y,bar.Width*(float)pct,bar.Height),bc); DrawBorder(fr,bar,COL_DIM,1f);
        }

        private void DrawAutocraftDash(IMyTextSurface s, int page)
        {
            BuildStockCache(); var names=new List<string>(_compQuotas.Keys);names.Sort();
            PrepSurf(s);var vp=VP(s);var panel=Inset(vp,10f);
            using (var fr=s.DrawFrame())
            {
                Fill(fr,vp,COL_BG);Fill(fr,panel,COL_PANEL);DrawBorder(fr,panel,COL_ACCENT,3f);
                int rows=Math.Max(1,(int)((panel.Height-128f)/34f)),pages=Math.Max(1,(int)Math.Ceiling(names.Count/(double)rows));
                if (page<1)page=1;if (page>pages)page=pages;int start=(page-1)*rows,end=Math.Min(names.Count,start+rows);
                Txt(fr,"AUTOCRAFTING",panel.X+24f,panel.Y+24f,COL_ACCENT2,0.95f,TextAlignment.LEFT);
                float y=panel.Y+86f;
                for (int i=start;i<end;i++)
                { string item=names[i];double quota=_compQuotas[item],stock=StockAmount("Component",item),pct=quota>0?Math.Min(1.0,stock/quota):0.0;
                  var row=new RectangleF(panel.X+16f,y,panel.Width-32f,28f);Fill(fr,row,COL_PANEL2);DrawBorder(fr,row,COL_DIM,1f);
                  fr.Add(new MySprite(SpriteType.TEXTURE,"MyObjectBuilder_Component/"+item,new Vector2(row.X+14f,row.Y+14f),new Vector2(20f,20f),COL_ROW_TEXT));
                  Txt(fr,Trim(SplitName(item),18),row.X+30f,y+5f,COL_ROW_TEXT,0.43f,TextAlignment.LEFT);
                  Txt(fr,FmtAmt(stock)+" / "+FmtAmt(quota),row.Right-10f,y+5f,COL_ROW_TEXT,0.43f,TextAlignment.RIGHT);
                  var bar=new RectangleF(row.X+10f,row.Bottom-6f,row.Width-20f,4f);Fill(fr,bar,COL_PROG_BG);
                  Fill(fr,new RectangleF(bar.X,bar.Y,bar.Width*(float)pct,bar.Height),pct>=0.5?COL_PROG_FILL:COL_WARN);y+=34f; }
            }
        }

        private bool FuelBlockMatches(IMyTerminalBlock b, string spec)
        { if (string.IsNullOrWhiteSpace(spec)) return _fuelUngrouped; spec=spec.Trim();
          if (spec.StartsWith("G:",SC)){string grpName=spec.Substring(2).Trim();if(grpName.Length==0)return _fuelUngrouped;var grp=GridTerminalSystem.GetBlockGroupWithName(grpName);if(grp==null)return false;_groupBuf.Clear();grp.GetBlocks(_groupBuf);return _groupBuf.Contains(b);}
          return b.CustomName.IndexOf(spec,SC)>=0; }

        private void DrawFuelDash(IMyTextSurface s)
        {
            BuildStockCache();
            double h2N=0,h2M=0,o2N=0,o2M=0,genIce=0; int h2C=0,o2C=0,gC=0,gOn=0,gWk=0,vOk=0,vLeak=0; string leaks="";
            _vents.Clear(); var ice=MyItemType.MakeOre("Ice");
            for (int i=0;i<_blocks.Count;i++)
            {
                var b=_blocks[i];
                var tank=b as IMyGasTank;
                if (tank!=null)
                { double cap=tank.Capacity,fil=cap*tank.FilledRatio; bool isH2=b.BlockDefinition.TypeIdString.IndexOf("Hydrogen",SC)>=0||b.BlockDefinition.SubtypeId.IndexOf("Hydrogen",SC)>=0||b.CustomName.IndexOf("Hydrogen",SC)>=0;
                  if (isH2){if(!FuelBlockMatches(b,_fuelH2Tanks))continue;h2C++;h2M+=cap;h2N+=fil;} else {if(!FuelBlockMatches(b,_fuelO2Tanks))continue;o2C++;o2M+=cap;o2N+=fil;} continue; }
                var gen=b as IMyGasGenerator;
                if (gen!=null){if(!FuelBlockMatches(b,_fuelGenerators))continue;gC++;if(gen.Enabled)gOn++;if(gen.IsWorking)gWk++;if(gen.InventoryCount>0)genIce+=(double)gen.GetInventory(0).GetItemAmount(ice);continue;}
                var vent=b as IMyAirVent;
                if (vent!=null&&b.CustomData.IndexOf("InteriorVent",SC)>=0)
                { _vents.Add(vent);bool ok=vent.IsWorking&&vent.CanPressurize&&vent.GetOxygenLevel()>=0.95f;
                  if(ok)vOk++;else{vLeak++;if(leaks.Length<80){if(leaks.Length>0)leaks+=", ";leaks+=vent.CustomName;}} }
            }
            PrepSurf(s);var vp=VP(s);var panel=Inset(vp,10f);
            using (var fr=s.DrawFrame())
            {
                Fill(fr,vp,COL_BG);Fill(fr,panel,COL_PANEL);DrawBorder(fr,panel,COL_ACCENT,3f);
                Txt(fr,"FUEL & LIFE SUPPORT",panel.X+24f,panel.Y+24f,COL_ACCENT2,0.92f,TextAlignment.LEFT);
                Txt(fr,"ONLINE",panel.Right-24f,panel.Y+28f,COL_OK,0.48f,TextAlignment.RIGHT);
                float y=panel.Y+64f;
                DrawTankRow(fr,panel,y,"H2 Hydrogen",h2N,h2M,h2C);y+=62f;
                DrawTankRow(fr,panel,y,"O2 Oxygen",o2N,o2M,o2C);y+=62f;
                Row(fr,panel,y,"Generators",gWk+" working / "+gOn+" on / "+gC,COL_TEXT);y+=32f;
                Row(fr,panel,y,"Ice in gens",FmtAmt(genIce),COL_ACCENT2);y+=32f;
                Row(fr,panel,y,"Ice stock",FmtAmt(StockAmount("Ore","Ice")),COL_ACCENT2);y+=32f;
                Row(fr,panel,y,"Vents",vOk+" OK / "+vLeak+" leaking",vLeak>0?COL_BAD:COL_OK);
            }
        }

        private void DrawTankRow(MySpriteDrawFrame fr, RectangleF panel, float y, string label, double filled, double cap, int count)
        {
            var row=new RectangleF(panel.X+16f,y,panel.Width-32f,54f);Fill(fr,row,COL_PANEL2);DrawBorder(fr,row,COL_DIM,1f);
            double pct=cap>0?Math.Min(1.0,filled/cap):0.0;
            Txt(fr,label,row.X+10f,row.Y+6f,COL_ROW_TEXT,0.48f,TextAlignment.LEFT);
            Txt(fr,Pct(pct)+" "+FmtGas(filled)+"/"+FmtGas(cap),row.Right-10f,row.Y+6f,COL_ROW_TEXT,0.38f,TextAlignment.RIGHT);
            Txt(fr,count+" tanks",row.Right-10f,row.Y+24f,COL_ROW_DIM,0.30f,TextAlignment.RIGHT);
            var bar=new RectangleF(row.X+10f,row.Y+40f,row.Width-20f,8f);Fill(fr,bar,COL_PROG_BG);
            Fill(fr,new RectangleF(bar.X,bar.Y,bar.Width*(float)pct,bar.Height),pct>=0.20?COL_PROG_FILL:COL_WARN);DrawBorder(fr,bar,COL_ROW_DIM,1f);
        }

        // =========================================================================
        // PB SCREEN ANIMATION
        // =========================================================================
        private double _animTime   = 0.0;
        private double _animPulse  = 0.0;
        private float  _animRot    = 0.0f;

        private void TickPbAnim(double dt)
        {
            _animTime  += dt;
            _animPulse += dt * 2.5;
            _animRot   += (float)(dt * 0.5);
        }

        private void DrawHexAnim(MySpriteDrawFrame fr, RectangleF panel)
        {
            float cx = panel.X + panel.Width  * 0.5f;
            float cy = panel.Y + panel.Height * 0.5f;

            // 3 hex rings
            int[] ringCounts = { 1, 6, 12 };
            float[] ringRadii = { 0f, 22f, 44f };
            float[] hexSizes  = { 14f, 12f, 10f };

            for (int ring = 0; ring < 3; ring++)
            {
                int n = ringCounts[ring];
                for (int i = 0; i < n; i++)
                {
                    float angle = (ring == 0) ? 0f : (float)(i * Math.PI * 2.0 / n) + _animRot * (ring == 1 ? 1f : -0.6f);
                    float hx = cx + (float)Math.Cos(angle) * ringRadii[ring];
                    float hy = cy + (float)Math.Sin(angle) * ringRadii[ring];
                    float sz = hexSizes[ring];
                    float alpha = (ring == 0) ? 0.5f : (0.35f - ring * 0.1f);
                    Color col = new Color(
                        (byte)(COL_ACCENT.R),
                        (byte)(COL_ACCENT.G),
                        (byte)(COL_ACCENT.B),
                        (byte)(alpha * 255));
                    fr.Add(new MySprite(SpriteType.TEXTURE, "Circle",
                        new Vector2(hx, hy), new Vector2(sz, sz), col));
                }
            }

            // pulsing centre
            float pr = 6f + (float)Math.Sin(_animPulse) * 2f;
            float pa = 0.7f + (float)Math.Sin(_animPulse) * 0.25f;
            Color pulseCol = new Color(COL_ACCENT2.R, COL_ACCENT2.G, COL_ACCENT2.B, (byte)(pa * 255));
            fr.Add(new MySprite(SpriteType.TEXTURE, "Circle",
                new Vector2(cx, cy), new Vector2(pr * 2f, pr * 2f), pulseCol));

            // outer ring
            float or2 = 54f + (float)Math.Sin(_animPulse * 0.5) * 2f;
            Color ringCol = new Color(COL_ACCENT.R, COL_ACCENT.G, COL_ACCENT.B, 40);
            fr.Add(new MySprite(SpriteType.TEXTURE, "Circle",
                new Vector2(cx, cy), new Vector2(or2 * 2f, or2 * 2f), ringCol));
        }

        // =========================================================================
        // BOOT SCREEN
        // =========================================================================
        private void DrawBootSurface(IMyTextSurface s, double progress)
        {
            if (s==null) return; PrepSurf(s);var vp=VP(s);var panel=Inset(vp,10f);
            using (var fr=s.DrawFrame())
            {
                Fill(fr,vp,COL_BG);Fill(fr,panel,COL_PANEL);DrawBorder(fr,panel,COL_ACCENT,3f);
                var cx=panel.X+panel.Width*0.5f;
                Txt(fr,"AUTOGRID MANAGER",cx,panel.Y+panel.Height*0.30f,COL_ACCENT2,0.85f,TextAlignment.CENTER);
                Txt(fr,"RevGamer",cx,panel.Y+panel.Height*0.42f,COL_TEXT,0.44f,TextAlignment.CENTER);
                Txt(fr,"BOOTING"+new string('.',(int)(_bootDots)),cx,panel.Y+panel.Height*0.52f,COL_OK,0.52f,TextAlignment.CENTER);
                float bw=panel.Width*0.6f; var bar=new RectangleF(cx-bw*0.5f,panel.Y+panel.Height*0.62f,bw,12f);
                Fill(fr,bar,COL_PROG_BG);Fill(fr,new RectangleF(bar.X,bar.Y,bar.Width*(float)progress,bar.Height),progress>=1.0?COL_OK:COL_PROG_FILL);
                DrawBorder(fr,bar,COL_ACCENT2,1f);
                Txt(fr,((int)(progress*100)).ToString()+"%",cx,bar.Bottom+20f,COL_DIM,0.40f,TextAlignment.CENTER);
                Txt(fr,"AutoGrid Manager v1.0",cx,panel.Bottom-18f,COL_DIM,0.34f,TextAlignment.CENTER);
            }
        }

        // =========================================================================
        // SPRITE / LAYOUT HELPERS
        // =========================================================================
        private void PrepSurf(IMyTextSurface s){s.ContentType=ContentType.SCRIPT;s.Script="";s.Font="Monospace";s.FontSize=1.0f;s.TextPadding=1f;s.ScriptBackgroundColor=COL_BG;s.BackgroundColor=COL_BG;}
        private RectangleF VP(IMyTextSurface s){return new RectangleF((s.TextureSize-s.SurfaceSize)*0.5f,s.SurfaceSize);}
        private RectangleF Inset(RectangleF r,float a){return new RectangleF(r.X+a,r.Y+a,r.Width-a*2f,r.Height-a*2f);}
        private void Fill(MySpriteDrawFrame fr,RectangleF r,Color c){fr.Add(new MySprite(SpriteType.TEXTURE,"SquareSimple",r.Position+r.Size*0.5f,r.Size,c));}
        private void DrawBorder(MySpriteDrawFrame fr,RectangleF r,Color c,float t){Fill(fr,new RectangleF(r.X,r.Y,r.Width,t),c);Fill(fr,new RectangleF(r.X,r.Bottom-t,r.Width,t),c);Fill(fr,new RectangleF(r.X,r.Y,t,r.Height),c);Fill(fr,new RectangleF(r.Right-t,r.Y,t,r.Height),c);}
        private void Txt(MySpriteDrawFrame fr,string text,float x,float y,Color c,float sc2,TextAlignment al){fr.Add(new MySprite(SpriteType.TEXT,text??"",new Vector2(x,y),null,c,"Monospace",al,sc2));}
        private void Row(MySpriteDrawFrame fr,RectangleF panel,float y,string label,string value,Color vc)
        { var row=new RectangleF(panel.X+16f,y,panel.Width-32f,26f);Fill(fr,row,COL_PANEL2);DrawBorder(fr,row,COL_DIM,1f);
          Txt(fr,label,row.X+10f,row.Y+4f,COL_ROW_TEXT,0.46f,TextAlignment.LEFT);Txt(fr,Trim(value,28),row.Right-10f,row.Y+4f,RVC(vc),0.46f,TextAlignment.RIGHT); }
        private Color RVC(Color c){if(c.PackedValue==COL_OK.PackedValue||c.PackedValue==COL_WARN.PackedValue||c.PackedValue==COL_BAD.PackedValue)return c;return COL_ROW_TEXT;}
        private void DrawLogTypeRow(MySpriteDrawFrame fr,RectangleF panel,float y,string label,string type1,string type2)
        { int count=0;double cur=0,max2=0; for(int i=0;i<_cargos.Count;i++){if(!_cargos[i].Type.Equals(type1,SC)&&(type2.Length==0||!_cargos[i].Type.Equals(type2,SC)))continue;count++;cur+=(double)_cargos[i].Inv.CurrentVolume;max2+=(double)_cargos[i].Inv.MaxVolume;}
          double pct=max2>0?cur/max2*100.0:0.0; var row=new RectangleF(panel.X+16f,y,panel.Width-32f,26f);Fill(fr,row,COL_PANEL2);DrawBorder(fr,row,COL_DIM,1f);
          Txt(fr,label,row.X+10f,row.Y+4f,COL_ROW_TEXT,0.46f,TextAlignment.LEFT);Txt(fr,count+" cargo  "+pct.ToString("0.0")+"%",row.Right-10f,row.Y+4f,pct>97?COL_BAD:COL_ROW_TEXT,0.46f,TextAlignment.RIGHT); }

        // =========================================================================
        // STRING / MATH HELPERS
        // =========================================================================
        private string ItemCategory(MyItemType t){string s=t.TypeId.ToString();if(s.EndsWith("_Ore"))return "Ore";if(s.EndsWith("_Ingot"))return "Ingot";if(s.EndsWith("_Component"))return "Component";if(s.EndsWith("_AmmoMagazine"))return "Ammo";if(s.EndsWith("_PhysicalGunObject"))return "Tool";if(s.EndsWith("_GasContainerObject")||s.EndsWith("_OxygenContainerObject"))return "Bottle";return "";}
        private string DisplayName(MyItemType t){string n=t.SubtypeId.ToString();if(n=="Stone"&&t.TypeId.ToString().EndsWith("_Ingot"))return "Gravel";return SplitName(n);}
        private string ItemIcon(MyItemType t){string s=t.TypeId.ToString(),sub=t.SubtypeId.ToString();if(s.EndsWith("_Ore"))return "MyObjectBuilder_Ore/"+sub;if(s.EndsWith("_Ingot"))return "MyObjectBuilder_Ingot/"+sub;if(s.EndsWith("_Component"))return "MyObjectBuilder_Component/"+sub;if(s.EndsWith("_AmmoMagazine"))return "MyObjectBuilder_AmmoMagazine/"+sub;return "IconInventory";}
        private string SplitName(string n){if(string.IsNullOrEmpty(n))return "";_sb.Clear();for(int i=0;i<n.Length;i++){char c=n[i];if(i>0&&char.IsUpper(c)&&char.IsLower(n[i-1]))_sb.Append(' ');_sb.Append(c);}return _sb.ToString();}
        private double StockQuota(StockEntry e){if(e.Category.Equals("Component",SC)){double q;if(_compQuotas.TryGetValue(e.Name.Replace(" ",""),out q))return q;if(e.Name.IndexOf("Steel Plate",SC)>=0)return 50000;if(e.Name.IndexOf("Interior Plate",SC)>=0)return 50000;}return Math.Max(1,e.Amount);}
        private string StockKind(IMyTerminalBlock b){string d=b.CustomData??"";if(d.IndexOf("InventoryStock",SC)>=0||d.IndexOf("Inventory Stock",SC)>=0)return "Inventory";if(d.IndexOf("OreStock",SC)>=0)return "Ore";if(d.IndexOf("IngotStock",SC)>=0)return "Ingot";if(d.IndexOf("ComponentStock",SC)>=0)return "Component";if(d.IndexOf("AmmoStock",SC)>=0)return "Ammo";if(d.IndexOf("ToolStock",SC)>=0)return "Tool";if(d.IndexOf("BottleStock",SC)>=0)return "Bottle";return "";}
        private int StockPage(IMyTerminalBlock b,string kind){string d=b.CustomData??"",compact=(kind.Equals("Inventory",SC)?"InventoryStock":kind+"Stock");string[]lines=d.Split(new char[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries);for(int i=0;i<lines.Length;i++){string line=StripComment(lines[i]).Trim();if(line.IndexOf(compact,SC)<0)continue;int n=FirstNum(line);if(n>0)return n;}return 1;}
        private int DashPage(IMyTerminalBlock b,string cmd){string d=b.CustomData??"";string[]lines=d.Split(new char[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries);for(int i=0;i<lines.Length;i++){string line=StripComment(lines[i]).Trim();if(line.IndexOf(cmd,SC)<0)continue;int n=FirstNum(line);if(n>0)return n;}return 1;}
        private int FirstNum(string s){int v=0;bool f=false;for(int i=0;i<s.Length;i++){if(char.IsDigit(s[i])){f=true;v=v*10+(s[i]-'0');}else if(f)break;}return v;}
        private bool IsNoSort(IMyTerminalBlock b){return HasToken(b.CustomName,_noSortTag)||b.CubeGrid.CustomName.IndexOf(_noSortTag,SC)>=0;}
        private bool HasToken(string s,string t){return !string.IsNullOrEmpty(s)&&!string.IsNullOrEmpty(t)&&s.IndexOf(t,SC)>=0;}
        private string NormKey(string v){if(v==null)return "";_sb.Clear();for(int i=0;i<v.Length;i++)if(char.IsLetterOrDigit(v[i]))_sb.Append(char.ToLowerInvariant(v[i]));return _sb.ToString();}
        private string Trim(string s,int max){if(s==null)return "";return s.Length<=max?s:s.Substring(0,max-1)+"~";}
        private string Pct(double r){return (r*100.0).ToString("0.0")+"%";}
        private string FmtAmt(double v){if(v>=1e9)return (v/1e9).ToString("0.##")+"B";if(v>=1e6)return (v/1e6).ToString("0.##")+"M";if(v>=1e3)return (v/1e3).ToString("0.##")+"K";return v.ToString("0.##");}
        private string FmtPow(double mw){double w=mw*1e6;if(w>=1e9)return (w/1e9).ToString("0.##")+"G";if(w>=1e6)return (w/1e6).ToString("0.##")+"M";if(w>=1e3)return (w/1e3).ToString("0.##")+"k";return w.ToString("0");}
        private string FmtGas(double l){if(l>=1e9)return (l/1e9).ToString("0.##")+"GL";if(l>=1e6)return (l/1e6).ToString("0.##")+"ML";if(l>=1e3)return (l/1e3).ToString("0.##")+"KL";return l.ToString("0.##")+"L";}
        private Color BatCol(double p){if(p<0.25)return COL_BAD;if(p<0.50)return COL_WARN;return COL_OK;}
        private Color OutCol(double p){if(p>0.90)return COL_BAD;if(p>0.75)return COL_WARN;return COL_OK;}
        private bool TrySplit(string line,char sep,out string key,out string val){key="";val="";int i=line.IndexOf(sep);if(i<0)return false;key=line.Substring(0,i).Trim();val=line.Substring(i+1).Trim();return key.Length>0;}
        private bool ParseBool(string v,bool fb){if(v==null)return fb;v=v.Trim().ToLowerInvariant();if(v=="true"||v=="yes"||v=="on"||v=="1")return true;if(v=="false"||v=="no"||v=="off"||v=="0")return false;return fb;}
        private string StripComment(string line){int i=line.IndexOf("//");if(i>=0)return line.Substring(0,i);i=line.IndexOf(';');if(i>=0)return line.Substring(0,i);return line;}
        private int ProdAsmProducing(){int n=0;for(int i=0;i<_assemblers.Count;i++)if(_assemblers[i].IsProducing)n++;return n;}
        private int ProdAsmQueued()   {int n=0;for(int i=0;i<_assemblers.Count;i++)if(!_assemblers[i].IsQueueEmpty)n++;return n;}
        private int ProdRefProducing(){int n=0;for(int i=0;i<_refineries.Count;i++)if(_refineries[i].IsProducing)n++;return n;}
        private double ProdRefInputFill(){double c=0,m=0;for(int i=0;i<_refineries.Count;i++){var inv=_refineries[i].GetInventory(0);c+=(double)inv.CurrentVolume;m+=(double)inv.MaxVolume;}return m>0?c/m*100.0:0.0;}
    }
}
