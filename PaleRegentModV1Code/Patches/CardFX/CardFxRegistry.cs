using MegaCrit.Sts2.Core.Models;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches.CardFX;

internal sealed record CardFxBinding(
    CardFxState State,
    string Slot,
    CardFxReplayPolicy ReplayPolicy,
    CardFxDefinition Effect);

internal sealed record CardFxStopRule(
    CardFxState State,
    string Slot);

/// <summary>
/// CardFX 的集中式关联表。卡牌类保持纯逻辑，动画关系统一放在 CardFxCatalog。
/// </summary>
public static class CardFxRegistry
{
    private static readonly Dictionary<Type, List<CardFxBinding>> Bindings = [];
    private static readonly Dictionary<Type, List<CardFxStopRule>> StopRules = [];

    public static CardFxRegistration For<TCard>()
        where TCard : CardModel
    {
        return new CardFxRegistration(typeof(TCard));
    }

    internal static void AddBinding(
        Type cardType,
        CardFxBinding binding)
    {
        if (!Bindings.TryGetValue(cardType, out List<CardFxBinding>? list))
        {
            list = [];
            Bindings[cardType] = list;
        }

        list.Add(binding);
    }

    internal static void AddStopRule(
        Type cardType,
        CardFxStopRule rule)
    {
        if (!StopRules.TryGetValue(cardType, out List<CardFxStopRule>? list))
        {
            list = [];
            StopRules[cardType] = list;
        }

        list.Add(rule);
    }

    internal static IEnumerable<CardFxBinding> GetBindings(
        CardModel card,
        CardFxState state)
    {
        for (Type? type = card.GetType();
             type is not null && typeof(CardModel).IsAssignableFrom(type);
             type = type.BaseType)
        {
            if (!Bindings.TryGetValue(type, out List<CardFxBinding>? list))
            {
                continue;
            }

            foreach (CardFxBinding binding in list)
            {
                if (binding.State == state)
                {
                    yield return binding;
                }
            }
        }
    }

    internal static IEnumerable<CardFxStopRule> GetStopRules(
        CardModel card,
        CardFxState state)
    {
        for (Type? type = card.GetType();
             type is not null && typeof(CardModel).IsAssignableFrom(type);
             type = type.BaseType)
        {
            if (!StopRules.TryGetValue(type, out List<CardFxStopRule>? list))
            {
                continue;
            }

            foreach (CardFxStopRule rule in list)
            {
                if (rule.State == state)
                {
                    yield return rule;
                }
            }
        }
    }
}

/// <summary>
/// 为某个卡牌类型声明状态特效的链式注册器。
/// </summary>
public sealed class CardFxRegistration
{
    private readonly Type _cardType;

    internal CardFxRegistration(Type cardType)
    {
        _cardType = cardType;
    }

    public CardFxRegistration On(
        CardFxState state,
        CardFxDefinition effect,
        string? slot = null,
        CardFxReplayPolicy replayPolicy = CardFxReplayPolicy.Replace)
    {
        ArgumentNullException.ThrowIfNull(effect);

        string resolvedSlot = string.IsNullOrWhiteSpace(slot)
            ? $"{state}:{effect.DebugName}"
            : slot;

        CardFxRegistry.AddBinding(
            _cardType,
            new CardFxBinding(
                state,
                resolvedSlot,
                replayPolicy,
                effect));

        return this;
    }

    /// <summary>
    /// 进入指定状态时，停止此卡牌对应槽位中仍在播放的特效。
    /// 典型用法：HoverExit 停止 HoverEnter 的持续光效。
    /// </summary>
    public CardFxRegistration StopOn(
        CardFxState state,
        string slot)
    {
        if (string.IsNullOrWhiteSpace(slot))
        {
            throw new ArgumentException(
                "CardFX 停止槽位不能为空。",
                nameof(slot));
        }

        CardFxRegistry.AddStopRule(
            _cardType,
            new CardFxStopRule(state, slot));

        return this;
    }
}
