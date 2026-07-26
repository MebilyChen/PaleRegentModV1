using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace PaleRegentModV1.PaleRegentModV1Code.Traits;

/// <summary>
/// 集中定义本 Mod 自定义关键词的静态悬停词条（static hover tips）。
///
/// 【原理说明】（参考原版 stsoriginal 源码）：
/// - 每张卡的 CardModel.HoverTips = ExtraHoverTips + 附魔/诅咒词条 + CanonicalKeywords 自动词条。
/// - 手牌聚焦时 NCardHolder.CreateHoverTips() 会自动读取 Model.HoverTips 并显示，
///   所以卡牌类只需要重写 ExtraHoverTips 声明"这张卡要展示哪些词条"即可，UI 层无需任何代码。
/// - Power 词条用 HoverTipFactory.FromPower&lt;TPower&gt;((int?)null)，
///   会自动读取 powers.json 的 title + description（dumb 描述，不带具体层数）。
/// - 生成牌预览用 HoverTipFactory.FromCard&lt;TCard&gt;(isUpgraded)，
///   悬停时直接显示该卡的完整卡面（即表格备注要求的 Hover Card Preview）。
/// - 自定义名词（失心/苍白/纯粹/模具/驾驭等非 Power 概念）用
///   new HoverTip(new LocString("static_hover_tips", "KEY.title"), new LocString("static_hover_tips", "KEY.description"))，
///   文案登记在 localization/zhs/static_hover_tips.json。
///
/// 文案与机制表《苍白之王_杀塔机制整理20260725.xlsx》"卡牌名词（提供HoverTips）"sheet 对齐。
/// </summary>
public static class ModHoverTips
{
    private const string Table = "static_hover_tips";

    private static HoverTip Static(string key) => new HoverTip(
        new LocString(Table, $"PALEREGENTMODV1-{key}.title"),
        new LocString(Table, $"PALEREGENTMODV1-{key}.description"));

    /// <summary>【虚空】能量计数器词条（名词表#18：回合开始按虚空数量扣灵魂，跨回合保留）。</summary>
    public static IHoverTip VoidCounter => Static("VOID_COUNTER");

    /// <summary>【失心 Lost】词条（名词表#1：灵魂转虚空费、重放1、取消苍白）。</summary>
    public static IHoverTip Lost => Static("LOST");

    /// <summary>【苍白 Pale】词条（名词表#3：移除失心与虚空费，获得虚无）。</summary>
    public static IHoverTip Pale => Static("PALE");

    /// <summary>【纯粹 Pure】词条（名词表#2：战斗中无法被变化）。</summary>
    public static IHoverTip Pure => Static("PURE");

    /// <summary>【模具 Mould】词条（名词表#9：有概率获得该牌制成的遗物奖励）。</summary>
    public static IHoverTip Mould => Static("MOULD");

    /// <summary>【驾驭 Harness】词条（名词表#4：造物牌伤害/格挡提高。表格要求不做成 Power，故用静态词条）。</summary>
    public static IHoverTip Harness => Static("HARNESS");

    /// <summary>【感染】特质规则词条（名词表#8：回合结束留在手牌时变化随机手牌为感染并触发疑虑）。</summary>
    public static IHoverTip InfectionRule => Static("INFECTION_RULE");
}
