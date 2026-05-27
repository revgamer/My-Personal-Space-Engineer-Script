private const string VERSION = "1.0-production";
private const string PB_TAG = "{AGM-Production}";
private const string CORE_TAG = "{AGM-Core}";
private static readonly StringComparison SC = StringComparison.OrdinalIgnoreCase;
private readonly Color COLOR_BG = new Color(5, 16, 28);
private readonly Color COLOR_PANEL = new Color(9, 24, 40);
private readonly Color COLOR_ACCENT = new Color(255, 231, 38);
private readonly Color COLOR_ACCENT_2 = new Color(255, 225, 94);
private readonly Color COLOR_TEXT = new Color(244, 227, 184);
private readonly Color COLOR_DIM = new Color(191, 160, 100);
private readonly Color COLOR_OK = new Color(91, 242, 159);
private readonly Color COLOR_WARN = new Color(255, 205, 89);
private readonly Color COLOR_BAD = new Color(255, 100, 78);

private readonly List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
private readonly List<IMyAssembler> assemblers = new List<IMyAssembler>();
private readonly List<IMyRefinery> refineries = new List<IMyRefinery>();
private readonly List<MyProductionItem> queue = new List<MyProductionItem>();
private readonly List<MyInventoryItem> invItems = new List<MyInventoryItem>();
private readonly List<string> refineryPriority = new List<string>();
private readonly List<string> assemblerPriority = new List<string>();
private readonly Dictionary<string, double> componentQuotas = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
private readonly Dictionary<string, double> componentStock = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
private readonly Dictionary<string, double> queuedComponents = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
private readonly StringBuilder sb = new StringBuilder();

private bool coreFound = false;
private bool coreEnabled = false;
private bool productionEnabled = false;
private bool globalPause = false;
private bool includeDockedGrids = false;
private bool monitorOnly = true;
private bool autocraftComponents = true;
private bool sortAssemblerQueue = true;
private bool sortRefineryInput = true;
private int maxQueuePerRun = 2;
private int maxQueueAmount = 500;
private int tick = 0;
private bool booting = true;
private double bootElapsed = 0.0;
private DateTime lastRun = DateTime.Now;
private string lastStatus = "boot";
private string lastAction = "";
private int lastQueued = 0;
private int lastAssemblerMoves = 0;
private int lastRefineryMoves = 0;
private string warning = "";
private string noSortingTag = "[No Sorting]";
private string manualTag = "{Manual}";
private string hiddenTag = "{Hidden}";

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    EnsureConfig();
    Reload();
}

public void Save()
{
    WriteState();
}

public void Main(string argument, UpdateType updateSource)
{
    string arg = argument == null ? "" : argument.Trim();
    if (arg.Equals("reload", SC) || arg.Equals("rescan", SC) || arg.Equals("reboot", SC) || arg.Equals("boot", SC))
    {
        Reload();
        if (arg.Equals("reboot", SC) || arg.Equals("boot", SC))
            RestartBoot();
    }
    if (arg.Equals("run", SC) || arg.Equals("production", SC))
    {
        Reload();
        RunProduction();
    }

    if ((updateSource & (UpdateType.Update10 | UpdateType.Update100)) != 0)
    {
        tick += (updateSource & UpdateType.Update100) != 0 ? 100 : 10;
        if (tick >= 100)
        {
            tick = 0;
            Reload();
            RunProduction();
        }
    }

    if (booting)
    {
        bootElapsed += Math.Max(0.1, (DateTime.Now - lastRun).TotalSeconds);
        lastRun = DateTime.Now;
        DrawModuleBoot(Me.GetSurface(0), Math.Min(1.0, bootElapsed / 4.0));
        if (bootElapsed >= 4.0) booting = false;
        return;
    }
    lastRun = DateTime.Now;

    DrawPbStatus();
    WriteState();
    EchoStatus();
}

private void RestartBoot()
{
    booting = true;
    bootElapsed = 0.0;
    lastRun = DateTime.Now;
}

private void EnsureConfig()
{
    if (!string.IsNullOrWhiteSpace(Me.CustomData)) return;
    Me.CustomData =
@"[Production]
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

[ComponentQuotas]
SteelPlate=50000
InteriorPlate=50000
Construction=50000
Computer=5000
Motor=10000
Display=1000
MetalGrid=5000
SmallTube=5000
LargeTube=5000
GravityGenerator=100";
}

private void Reload()
{
    ReadOwnConfig();
    ScanBlocks();
    ReadCore();
    if (includeDockedGrids)
        ScanBlocks();
    BuildProductionLists();
}

private void ReadOwnConfig()
{
    monitorOnly = true;
    autocraftComponents = true;
    sortAssemblerQueue = true;
    sortRefineryInput = true;
    maxQueuePerRun = 2;
    maxQueueAmount = 500;
    refineryPriority.Clear();
    assemblerPriority.Clear();
    componentQuotas.Clear();
    string section = "";
    string[] lines = Me.CustomData.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    for (int i = 0; i < lines.Length; i++)
    {
        string line = StripComment(lines[i]).Trim();
        if (line.Length == 0) continue;
        if (line.StartsWith("[") && line.EndsWith("]"))
        {
            section = line.Substring(1, line.Length - 2).Trim();
            continue;
        }
        string key, value;
        if (TrySplitKeyValue(line, '=', out key, out value) && key.Equals("AutoCrafting", SC))
        {
            section = value.Trim().Equals("Component", SC) ? "ComponentQuotas" : "";
            continue;
        }
        if (section.Equals("Production", SC))
        {
            if (!TrySplitKeyValue(line, '=', out key, out value)) continue;
            if (key.Equals("monitor_only", SC)) monitorOnly = ParseBool(value, true);
            else if (key.Equals("autocraft_components", SC)) autocraftComponents = ParseBool(value, true);
            else if (key.Equals("sort_assembler_queue", SC)) sortAssemblerQueue = ParseBool(value, true);
            else if (key.Equals("sort_refinery_input", SC)) sortRefineryInput = ParseBool(value, true);
            else if (key.Equals("max_queue_per_run", SC)) int.TryParse(value, out maxQueuePerRun);
            else if (key.Equals("max_queue_amount", SC)) int.TryParse(value, out maxQueueAmount);
        }
        else if (section.Equals("RefineryPriority", SC))
        {
            if (!line.Contains("=")) refineryPriority.Add(line);
        }
        else if (section.Equals("AssemblerPriority", SC))
        {
            if (!line.Contains("=")) assemblerPriority.Add(line);
        }
        else if (section.Equals("ComponentQuotas", SC))
        {
            if (!TrySplitKeyValue(line, '=', out key, out value)) continue;
            double quota;
            if (double.TryParse(value, out quota) && quota > 0)
                componentQuotas[key.Trim()] = quota;
        }
    }
    if (maxQueuePerRun < 1) maxQueuePerRun = 1;
    if (maxQueuePerRun > 10) maxQueuePerRun = 10;
    if (maxQueueAmount < 1) maxQueueAmount = 1;
    if (maxQueueAmount > 5000) maxQueueAmount = 5000;
}

private void ScanBlocks()
{
    blocks.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks, b => includeDockedGrids || b.IsSameConstructAs(Me));
}

private void ReadCore()
{
    coreFound = false;
    coreEnabled = false;
    productionEnabled = false;
    globalPause = false;
    includeDockedGrids = false;
    noSortingTag = "[No Sorting]";
    manualTag = "{Manual}";
    hiddenTag = "{Hidden}";
    for (int i = 0; i < blocks.Count; i++)
    {
        IMyProgrammableBlock pb = blocks[i] as IMyProgrammableBlock;
        if (pb == null || pb == Me || pb.CustomName.IndexOf(CORE_TAG, SC) < 0) continue;
        coreFound = true;
        string[] lines = pb.CustomData.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int l = 0; l < lines.Length; l++)
        {
            string key, value;
            if (!TrySplitKeyValue(StripComment(lines[l]).Trim(), '=', out key, out value)) continue;
            if (key.Equals("enabled", SC) || key.Equals("core_enabled", SC)) coreEnabled = ParseBool(value, true);
            else if (key.Equals("production", SC) || key.Equals("production_enabled", SC)) productionEnabled = ParseBool(value, true);
            else if (key.Equals("global_pause", SC)) globalPause = ParseBool(value, false);
            else if (key.Equals("include_docked_grids", SC)) includeDockedGrids = ParseBool(value, false);
            else if (key.Equals("no_sorting_tag", SC)) noSortingTag = value.Trim();
            else if (key.Equals("manual_tag", SC)) manualTag = value.Trim();
            else if (key.Equals("hidden_tag", SC)) hiddenTag = value.Trim();
        }
        break;
    }
}

private void BuildProductionLists()
{
    assemblers.Clear();
    refineries.Clear();
    for (int i = 0; i < blocks.Count; i++)
    {
        IMyTerminalBlock b = blocks[i];
        if (b == null || IsNoSortingBlock(b) || HasAnyToken(b, hiddenTag)) continue;
        IMyAssembler asm = b as IMyAssembler;
        if (asm != null && asm.IsFunctional && !HasAnyToken(asm, manualTag))
        {
            assemblers.Add(asm);
            continue;
        }
        IMyRefinery refinery = b as IMyRefinery;
        if (refinery != null && refinery.IsFunctional && !HasAnyToken(refinery, manualTag))
            refineries.Add(refinery);
    }
}

private void RunProduction()
{
    warning = "";
    lastQueued = 0;
    lastAssemblerMoves = 0;
    lastRefineryMoves = 0;
    lastAction = "";
    if (!coreFound) { lastStatus = "no core"; return; }
    if (!coreEnabled) { lastStatus = "core off"; return; }
    if (!productionEnabled) { lastStatus = "disabled"; return; }
    if (globalPause) { lastStatus = "paused"; return; }
    if (assemblers.Count == 0 && refineries.Count == 0) { lastStatus = "no machines"; return; }
    UpdateComponentStock();
    UpdateQueuedComponents();
    if (monitorOnly)
    {
        lastStatus = "monitoring";
        return;
    }
    if (sortRefineryInput) lastRefineryMoves = SortRefineryInputs();
    if (sortAssemblerQueue) lastAssemblerMoves = SortAssemblerQueues();
    if (autocraftComponents) lastQueued = QueueComponentQuotas();
    lastStatus = "active";
    if (lastQueued + lastAssemblerMoves + lastRefineryMoves == 0)
        lastStatus = "idle";
}

private void UpdateComponentStock()
{
    componentStock.Clear();
    for (int i = 0; i < blocks.Count; i++)
    {
        IMyTerminalBlock b = blocks[i];
        if (b == null || !b.HasInventory || IsNoSortingBlock(b) || HasAnyToken(b, hiddenTag)) continue;
        for (int inv = 0; inv < b.InventoryCount; inv++)
        {
            IMyInventory inventory = b.GetInventory(inv);
            invItems.Clear();
            inventory.GetItems(invItems);
            for (int item = 0; item < invItems.Count; item++)
            {
                MyItemType type = invItems[item].Type;
                if (!type.TypeId.ToString().EndsWith("_Component")) continue;
                string subtype = type.SubtypeId.ToString();
                double current;
                componentStock.TryGetValue(subtype, out current);
                componentStock[subtype] = current + (double)invItems[item].Amount;
            }
        }
    }
}

private void UpdateQueuedComponents()
{
    queuedComponents.Clear();
    for (int i = 0; i < assemblers.Count; i++)
    {
        queue.Clear();
        assemblers[i].GetQueue(queue);
        for (int q = 0; q < queue.Count; q++)
        {
            string item = ComponentFromBlueprint(queue[q].BlueprintId);
            if (item.Length == 0) continue;
            double current;
            queuedComponents.TryGetValue(item, out current);
            queuedComponents[item] = current + (double)queue[q].Amount;
        }
    }
}

private int QueueComponentQuotas()
{
    int queued = 0;
    foreach (var quota in componentQuotas)
    {
        if (queued >= maxQueuePerRun) break;
        double stock, alreadyQueued;
        componentStock.TryGetValue(quota.Key, out stock);
        queuedComponents.TryGetValue(quota.Key, out alreadyQueued);
        double need = quota.Value - stock - alreadyQueued;
        if (need < 1) continue;
        MyDefinitionId blueprint;
        IMyAssembler assembler = FindAssemblerFor(quota.Key, out blueprint);
        if (assembler == null)
        {
            warning = "No blueprint for " + quota.Key;
            continue;
        }
        double amount = Math.Min(Math.Ceiling(need), maxQueueAmount);
        assembler.AddQueueItem(blueprint, (MyFixedPoint)amount);
        queued++;
        lastAction = "Queued " + amount.ToString("0") + " " + quota.Key;
    }
    return queued;
}

private IMyAssembler FindAssemblerFor(string item, out MyDefinitionId blueprint)
{
    blueprint = new MyDefinitionId();
    string[] candidates = BlueprintCandidates(item);
    for (int c = 0; c < candidates.Length; c++)
    {
        MyDefinitionId bp;
        if (!MyDefinitionId.TryParse(candidates[c], out bp)) continue;
        for (int i = 0; i < assemblers.Count; i++)
        {
            if (assemblers[i].CustomName.IndexOf("!disassemble-only", SC) >= 0) continue;
            if (assemblers[i].CanUseBlueprint(bp))
            {
                blueprint = bp;
                return assemblers[i];
            }
        }
    }
    return null;
}

private string[] BlueprintCandidates(string item)
{
    string prefix = "MyObjectBuilder_BlueprintDefinition/";
    return new string[]
    {
        prefix + item,
        prefix + item + "Component",
        prefix + "Position0010_" + item,
        prefix + "Position0010_" + item + "Component"
    };
}

private int SortAssemblerQueues()
{
    int moved = 0;
    for (int i = 0; i < assemblers.Count && moved < maxQueuePerRun; i++)
    {
        queue.Clear();
        assemblers[i].GetQueue(queue);
        if (queue.Count < 2) continue;
        int best = BestAssemblerQueueIndex(queue, assemblers[i].IsProducing);
        if (best <= 0) continue;
        assemblers[i].MoveQueueItemRequest(queue[best].ItemId, 0);
        moved++;
        lastAction = "Moved " + ComponentFromBlueprint(queue[best].BlueprintId) + " to assembler front";
    }
    return moved;
}

private int BestAssemblerQueueIndex(List<MyProductionItem> q, bool isProducing)
{
    int best = -1;
    int bestPriority = int.MaxValue;
    int start = isProducing ? 0 : 1;
    for (int i = start; i < q.Count; i++)
    {
        string item = ComponentFromBlueprint(q[i].BlueprintId);
        int p = PriorityIndex(assemblerPriority, item);
        if (p < bestPriority)
        {
            bestPriority = p;
            best = i;
        }
    }
    if (bestPriority == int.MaxValue) return -1;
    return best;
}

private int SortRefineryInputs()
{
    int moved = 0;
    for (int r = 0; r < refineries.Count && moved < maxQueuePerRun; r++)
    {
        IMyInventory input = refineries[r].GetInventory(0);
        invItems.Clear();
        input.GetItems(invItems);
        if (invItems.Count < 2) continue;
        int best = BestRefineryInputIndex(invItems);
        if (best <= 0) continue;
        input.TransferItemTo(input, best, 0, true);
        moved++;
        lastAction = "Moved " + invItems[best].Type.SubtypeId + " to refinery front";
    }
    return moved;
}

private int BestRefineryInputIndex(List<MyInventoryItem> items)
{
    int best = -1;
    int bestPriority = int.MaxValue;
    for (int i = 0; i < items.Count; i++)
    {
        if (!items[i].Type.TypeId.ToString().EndsWith("_Ore")) continue;
        int p = PriorityIndex(refineryPriority, items[i].Type.SubtypeId.ToString());
        if (p < bestPriority)
        {
            bestPriority = p;
            best = i;
        }
    }
    if (bestPriority == int.MaxValue) return -1;
    return best;
}

private int PriorityIndex(List<string> priorities, string item)
{
    if (string.IsNullOrEmpty(item)) return int.MaxValue;
    for (int i = 0; i < priorities.Count; i++)
        if (item.IndexOf(priorities[i], SC) >= 0 || priorities[i].IndexOf(item, SC) >= 0)
            return i;
    return int.MaxValue;
}

private string ComponentFromBlueprint(MyDefinitionId blueprint)
{
    string s = blueprint.SubtypeName;
    if (s.StartsWith("Position", SC))
    {
        int idx = s.IndexOf("_");
        if (idx >= 0 && idx + 1 < s.Length) s = s.Substring(idx + 1);
    }
    if (s.EndsWith("Component", SC))
        s = s.Substring(0, s.Length - "Component".Length);
    return s;
}

private int WorkingAssemblers()
{
    int count = 0;
    for (int i = 0; i < assemblers.Count; i++)
        if (assemblers[i].IsWorking)
            count++;
    return count;
}

private int ProducingAssemblers()
{
    int count = 0;
    for (int i = 0; i < assemblers.Count; i++)
        if (assemblers[i].IsProducing)
            count++;
    return count;
}

private int QueuedAssemblers()
{
    int count = 0;
    for (int i = 0; i < assemblers.Count; i++)
        if (!assemblers[i].IsQueueEmpty)
            count++;
    return count;
}

private double TotalQueueAmount()
{
    double total = 0;
    for (int i = 0; i < assemblers.Count; i++)
    {
        queue.Clear();
        assemblers[i].GetQueue(queue);
        for (int q = 0; q < queue.Count; q++)
            total += (double)queue[q].Amount;
    }
    return total;
}

private int WorkingRefineries()
{
    int count = 0;
    for (int i = 0; i < refineries.Count; i++)
        if (refineries[i].IsWorking)
            count++;
    return count;
}

private int ProducingRefineries()
{
    int count = 0;
    for (int i = 0; i < refineries.Count; i++)
        if (refineries[i].IsProducing)
            count++;
    return count;
}

private double RefineryInputFill()
{
    double cur = 0, max = 0;
    for (int i = 0; i < refineries.Count; i++)
    {
        IMyInventory inv = refineries[i].GetInventory(0);
        cur += (double)inv.CurrentVolume;
        max += (double)inv.MaxVolume;
    }
    return max > 0 ? cur / max * 100.0 : 0.0;
}

private double RefineryOutputFill()
{
    double cur = 0, max = 0;
    for (int i = 0; i < refineries.Count; i++)
    {
        IMyInventory inv = refineries[i].GetInventory(1);
        cur += (double)inv.CurrentVolume;
        max += (double)inv.MaxVolume;
    }
    return max > 0 ? cur / max * 100.0 : 0.0;
}

private bool IsNoSortingBlock(IMyTerminalBlock b)
{
    return HasToken(b.CustomName, noSortingTag) || b.CubeGrid.CustomName.IndexOf(noSortingTag, SC) >= 0;
}

private bool HasToken(string name, string token)
{
    return !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(token) && name.IndexOf(token, SC) >= 0;
}

private bool HasAnyToken(IMyTerminalBlock block, string token)
{
    if (block == null || string.IsNullOrEmpty(token)) return false;
    return HasToken(block.CustomName, token) || HasToken(block.CustomData, token);
}

private void DrawPbStatus()
{
    DrawProduction(Me.GetSurface(0));
}

private void WriteState()
{
    sb.Clear();
    sb.AppendLine("[ProductionState]");
    sb.AppendLine("version=" + VERSION);
    sb.AppendLine("core_found=" + BoolText(coreFound));
    sb.AppendLine("core_enabled=" + BoolText(coreEnabled));
    sb.AppendLine("production_enabled=" + BoolText(productionEnabled));
    sb.AppendLine("global_pause=" + BoolText(globalPause));
    sb.AppendLine("state=" + StateText());
    sb.AppendLine("mode=" + (monitorOnly ? "monitor" : "active"));
    sb.AppendLine("monitor_only=" + BoolText(monitorOnly));
    sb.AppendLine("autocraft_components=" + BoolText(autocraftComponents));
    sb.AppendLine("autocraft_type=Component");
    sb.AppendLine("autocraft_quota_count=" + componentQuotas.Count);
    sb.AppendLine("sort_assembler_queue=" + BoolText(sortAssemblerQueue));
    sb.AppendLine("sort_refinery_input=" + BoolText(sortRefineryInput));
    sb.AppendLine("queued_last_run=" + lastQueued);
    sb.AppendLine("assembler_moves=" + lastAssemblerMoves);
    sb.AppendLine("refinery_moves=" + lastRefineryMoves);
    sb.AppendLine("last_action=" + lastAction);
    sb.AppendLine("assemblers=" + assemblers.Count);
    sb.AppendLine("assemblers_working=" + WorkingAssemblers());
    sb.AppendLine("assemblers_online=" + WorkingAssemblers());
    sb.AppendLine("assemblers_producing=" + ProducingAssemblers());
    sb.AppendLine("assemblers_queued=" + QueuedAssemblers());
    sb.AppendLine("queued_machines=" + QueuedAssemblers());
    sb.AppendLine("queue_amount=" + TotalQueueAmount().ToString("0"));
    sb.AppendLine("queued_items=" + TotalQueueAmount().ToString("0"));
    sb.AppendLine("refineries=" + refineries.Count);
    sb.AppendLine("refineries_working=" + WorkingRefineries());
    sb.AppendLine("refineries_online=" + WorkingRefineries());
    sb.AppendLine("refineries_producing=" + ProducingRefineries());
    sb.AppendLine("refinery_input_pct=" + RefineryInputFill().ToString("0.0"));
    sb.AppendLine("refinery_input_percent=" + RefineryInputFill().ToString("0.0"));
    sb.AppendLine("refinery_output_pct=" + RefineryOutputFill().ToString("0.0"));
    sb.AppendLine("warning=" + warning);
    foreach (var quota in componentQuotas)
    {
        double stock = 0;
        double queued = 0;
        componentStock.TryGetValue(quota.Key, out stock);
        queuedComponents.TryGetValue(quota.Key, out queued);
        sb.AppendLine("quota_" + quota.Key + "=" + quota.Value.ToString("0"));
        sb.AppendLine("stock_" + quota.Key + "=" + stock.ToString("0"));
        sb.AppendLine("queued_" + quota.Key + "=" + queued.ToString("0"));
    }
    Me.CustomData = UpsertSectionText(Me.CustomData, "[ProductionState]", sb.ToString());
}

private string UpsertSectionText(string raw, string header, string content)
{
    raw = raw ?? "";
    int start = raw.IndexOf(header, SC);
    if (start < 0)
    {
        if (raw.Trim().Length == 0) return content.TrimEnd();
        return raw.TrimEnd() + "\n\n" + content.TrimEnd();
    }
    int next = raw.IndexOf("\n[", start + header.Length, SC);
    if (next < 0)
        return raw.Substring(0, start).TrimEnd() + "\n\n" + content.TrimEnd();
    return raw.Substring(0, start).TrimEnd() + "\n\n" + content.TrimEnd() + "\n\n" + raw.Substring(next + 1).TrimStart();
}

private void DrawModuleBoot(IMyTextSurface surface, double progress)
{
    if (surface == null) return;
    surface.ContentType = ContentType.SCRIPT;
    surface.Script = "";
    surface.Font = "Monospace";
    surface.FontSize = 1.0f;
    surface.TextPadding = 1f;
    RectangleF vp = new RectangleF((surface.TextureSize - surface.SurfaceSize) * 0.5f, surface.SurfaceSize);
    using (var frame = surface.DrawFrame())
    {
        Fill(frame, vp, COLOR_BG);
        RectangleF panel = Inset(vp, 10f);
        Fill(frame, panel, COLOR_PANEL);
        DrawBorder(frame, panel, COLOR_ACCENT, 3f);
        Vector2 center = panel.Position + panel.Size * 0.5f;
        DrawText(frame, "AGM - Production", new Vector2(center.X, panel.Y + 56f), COLOR_ACCENT_2, 0.70f, TextAlignment.CENTER);
        DrawText(frame, "AutoGrid Manager", new Vector2(center.X, panel.Y + 88f), COLOR_TEXT, 0.42f, TextAlignment.CENTER);
        DrawText(frame, "BOOTING", new Vector2(center.X, panel.Y + 124f), COLOR_OK, 0.54f, TextAlignment.CENTER);
        RectangleF bar = new RectangleF(panel.X + 34f, center.Y + 36f, panel.Width - 68f, 12f);
        Fill(frame, bar, COLOR_BG);
        Fill(frame, new RectangleF(bar.X, bar.Y, bar.Width * (float)progress, bar.Height), COLOR_ACCENT);
        DrawBorder(frame, bar, COLOR_ACCENT_2, 1f);
        DrawText(frame, ((int)(progress * 100.0)).ToString() + "%", new Vector2(center.X, bar.Y + 28f), COLOR_DIM, 0.38f, TextAlignment.CENTER);
        DrawText(frame, "v" + VERSION, new Vector2(center.X, panel.Bottom - 18f), COLOR_DIM, 0.34f, TextAlignment.CENTER);
    }
}

private void DrawProduction(IMyTextSurface s)
{
    if (s == null) return;
    s.ContentType = ContentType.SCRIPT;
    s.Script = "";
    s.Font = "Monospace";
    s.FontSize = 0.72f;
    s.TextPadding = 1f;
    RectangleF vp = new RectangleF((s.TextureSize - s.SurfaceSize) * 0.5f, s.SurfaceSize);
    using (var frame = s.DrawFrame())
    {
        Fill(frame, vp, COLOR_BG);
        RectangleF panel = Inset(vp, 12f);
        Fill(frame, panel, COLOR_PANEL);
        DrawBorder(frame, panel, COLOR_ACCENT, 3f);
        float x = panel.X + 26f;
        float right = panel.Right - 26f;
        float y = panel.Y + 30f;
        DrawText(frame, "AGM PRODUCTION", new Vector2(x, y), COLOR_ACCENT_2, 0.58f, TextAlignment.LEFT);
        DrawText(frame, "LIVE " + DateTime.Now.ToString("HH:mm:ss"), new Vector2(right, y + 2f), COLOR_OK, 0.38f, TextAlignment.RIGHT);
        y += 34f;
        DrawText(frame, "AutoGrid Manager", new Vector2(x, y), COLOR_TEXT, 0.34f, TextAlignment.LEFT);
        DrawText(frame, "v" + VERSION, new Vector2(right, y), COLOR_DIM, 0.32f, TextAlignment.RIGHT);

        y += 34f;
        DrawRow(frame, panel, y, "Core", coreFound ? "CONNECTED" : "MISSING", coreFound ? COLOR_OK : COLOR_BAD);
        y += 28f;
        DrawRow(frame, panel, y, "State", StateText(), StateColor());
        y += 28f;
        DrawRow(frame, panel, y, "Mode", monitorOnly ? "MONITOR ONLY" : "READY", monitorOnly ? COLOR_DIM : COLOR_OK);
        y += 36f;

        DrawRow(frame, panel, y, "Assemblers", ProducingAssemblers() + " producing / " + WorkingAssemblers() + " online / " + assemblers.Count + " total", COLOR_OK);
        y += 28f;
        DrawRow(frame, panel, y, "Queued", QueuedAssemblers() + " machines / " + TotalQueueAmount().ToString("0") + " items", QueuedAssemblers() > 0 ? COLOR_WARN : COLOR_DIM);
        y += 28f;
        DrawBarRow(frame, panel, y, "Asm input", AverageFill(assemblers, 0)); y += 34f;
        DrawBarRow(frame, panel, y, "Asm output", AverageFill(assemblers, 1)); y += 38f;

        DrawRow(frame, panel, y, "Refineries", ProducingRefineries() + " producing / " + WorkingRefineries() + " online / " + refineries.Count + " total", COLOR_OK);
        y += 28f;
        DrawBarRow(frame, panel, y, "Ref input", RefineryInputFill()); y += 34f;
        DrawBarRow(frame, panel, y, "Ref output", RefineryOutputFill());
        if (warning.Length > 0)
            DrawText(frame, ShortText(warning, 42), new Vector2(x, panel.Bottom - 42f), COLOR_BAD, 0.32f, TextAlignment.LEFT);
        DrawText(frame, monitorOnly ? "Production actions disabled by monitor_only" : "Autocraft + queue priority active", new Vector2(x, panel.Bottom - 20f), COLOR_DIM, 0.30f, TextAlignment.LEFT);
    }
}

private double AverageFill(List<IMyAssembler> list, int invIndex)
{
    double cur = 0, max = 0;
    for (int i = 0; i < list.Count; i++)
    {
        if (list[i].InventoryCount <= invIndex) continue;
        IMyInventory inv = list[i].GetInventory(invIndex);
        cur += (double)inv.CurrentVolume;
        max += (double)inv.MaxVolume;
    }
    return max > 0 ? cur / max * 100.0 : 0.0;
}

private void DrawRow(MySpriteDrawFrame frame, RectangleF panel, float y, string label, string value, Color valueColor)
{
    RectangleF row = new RectangleF(panel.X + 18f, y - 4f, panel.Width - 36f, 24f);
    Fill(frame, row, new Color(105, 73, 29));
    DrawText(frame, label, new Vector2(row.X + 8f, y), COLOR_TEXT, 0.32f, TextAlignment.LEFT);
    DrawText(frame, value, new Vector2(row.Right - 8f, y), valueColor, 0.31f, TextAlignment.RIGHT);
}

private void DrawBarRow(MySpriteDrawFrame frame, RectangleF panel, float y, string label, double pct)
{
    RectangleF row = new RectangleF(panel.X + 18f, y - 4f, panel.Width - 36f, 30f);
    Fill(frame, row, new Color(105, 73, 29));
    DrawText(frame, label, new Vector2(row.X + 8f, y), COLOR_TEXT, 0.30f, TextAlignment.LEFT);
    DrawText(frame, pct.ToString("0.0") + "%", new Vector2(row.Right - 8f, y), pct > 95 ? COLOR_BAD : COLOR_OK, 0.30f, TextAlignment.RIGHT);
    RectangleF bar = new RectangleF(row.X + 116f, y + 12f, row.Width - 184f, 6f);
    Fill(frame, bar, COLOR_BG);
    Fill(frame, new RectangleF(bar.X, bar.Y, bar.Width * (float)Math.Min(1.0, pct / 100.0), bar.Height), pct > 95 ? COLOR_BAD : COLOR_OK);
}

private Color StateColor()
{
    string state = StateText();
    if (state.IndexOf("NO", SC) >= 0 || state.IndexOf("OFF", SC) >= 0 || state.IndexOf("DISABLED", SC) >= 0) return COLOR_BAD;
    if (state.IndexOf("PAUSED", SC) >= 0) return COLOR_WARN;
    return COLOR_OK;
}

private string StateText()
{
    if (!coreFound) return "NO CORE";
    if (!coreEnabled) return "CORE OFF";
    if (!productionEnabled) return "DISABLED";
    if (globalPause) return "PAUSED";
    return lastStatus.ToUpperInvariant();
}

private string ShortText(string text, int max)
{
    if (string.IsNullOrEmpty(text) || text.Length <= max) return text;
    return text.Substring(0, Math.Max(1, max - 2)) + "..";
}

private RectangleF Inset(RectangleF rect, float amount)
{
    return new RectangleF(rect.X + amount, rect.Y + amount, rect.Width - amount * 2f, rect.Height - amount * 2f);
}

private void Fill(MySpriteDrawFrame frame, RectangleF rect, Color color)
{
    frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", rect.Position + rect.Size * 0.5f, rect.Size, color));
}

private void DrawBorder(MySpriteDrawFrame frame, RectangleF r, Color color, float t)
{
    Fill(frame, new RectangleF(r.X, r.Y, r.Width, t), color);
    Fill(frame, new RectangleF(r.X, r.Bottom - t, r.Width, t), color);
    Fill(frame, new RectangleF(r.X, r.Y, t, r.Height), color);
    Fill(frame, new RectangleF(r.Right - t, r.Y, t, r.Height), color);
}

private void DrawText(MySpriteDrawFrame frame, string text, Vector2 pos, Color color, float scale, TextAlignment align)
{
    frame.Add(new MySprite(SpriteType.TEXT, text ?? "", pos, null, color, "Monospace", align, scale));
}

private void EchoStatus()
{
    Echo("AutoGrid Manager Production");
    Echo("Version: " + VERSION);
    Echo("PB tag : " + PB_TAG);
    Echo("Core   : " + (coreFound ? "CONNECTED" : "MISSING"));
    Echo("State  : " + StateText());
    Echo("Asm    : " + ProducingAssemblers() + " producing / " + assemblers.Count);
    Echo("Refs   : " + ProducingRefineries() + " producing / " + refineries.Count);
}

private string StripComment(string line)
{
    int idx = line.IndexOf("//");
    if (idx >= 0) return line.Substring(0, idx);
    idx = line.IndexOf(';');
    if (idx >= 0) return line.Substring(0, idx);
    return line;
}

private bool TrySplitKeyValue(string line, char separator, out string key, out string value)
{
    key = "";
    value = "";
    int idx = line.IndexOf(separator);
    if (idx < 0) return false;
    key = line.Substring(0, idx).Trim();
    value = line.Substring(idx + 1).Trim();
    return key.Length > 0;
}

private bool ParseBool(string value, bool fallback)
{
    bool parsed;
    if (bool.TryParse(value, out parsed)) return parsed;
    if (value.Equals("on", SC) || value.Equals("yes", SC) || value.Equals("1", SC)) return true;
    if (value.Equals("off", SC) || value.Equals("no", SC) || value.Equals("0", SC)) return false;
    return fallback;
}

private string BoolText(bool value)
{
    return value ? "true" : "false";
}
