using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace PaleRegentModV1.PaleRegentModV1Code.Patches.CardFX;

/// <summary>
/// CardFX 运行中枢。所有状态事件最终都由此查询注册表并播放特效。
/// </summary>
public static class CardFxPlayer
{
    private sealed class ActiveFx(
        CardModel card,
        string slot,
        CardFxDefinition definition,
        Node2D node)
    {
        public CardModel Card { get; } = card;
        public string Slot { get; } = slot;
        public CardFxDefinition Definition { get; } = definition;
        public Node2D Node { get; } = node;
        public Tween? LifetimeTween { get; set; }
    }

    private static readonly List<ActiveFx> ActiveEffects = [];
    private static CanvasLayer? _layer;

    public static bool Enabled { get; set; } = true;

    public static void Trigger(CardFxContext context)
    {
        if (!Enabled || !EnsureLayer())
        {
            return;
        }

        PruneInvalidEffects();

        foreach (CardFxStopRule rule in
                 CardFxRegistry.GetStopRules(context.Card, context.State))
        {
            StopSlot(context.Card, rule.Slot);
        }

        foreach (CardFxBinding binding in
                 CardFxRegistry.GetBindings(context.Card, context.State))
        {
            Play(context, binding);
        }
    }

    public static void StopAll(CardModel card)
    {
        foreach (ActiveFx active in ActiveEffects
                     .Where(active => ReferenceEquals(active.Card, card))
                     .ToArray())
        {
            Stop(active, useFade: true);
        }
    }

    private static void Play(
        CardFxContext context,
        CardFxBinding binding)
    {
        ActiveFx[] existing = ActiveEffects
            .Where(active =>
                ReferenceEquals(active.Card, context.Card) &&
                string.Equals(
                    active.Slot,
                    binding.Slot,
                    StringComparison.Ordinal))
            .ToArray();

        if (binding.ReplayPolicy == CardFxReplayPolicy.IgnoreWhilePlaying &&
            existing.Length > 0)
        {
            return;
        }

        if (binding.ReplayPolicy == CardFxReplayPolicy.Replace)
        {
            foreach (ActiveFx active in existing)
            {
                Stop(active, useFade: false);
            }
        }

        Node2D? node;

        try
        {
            node = binding.Effect.CreateNode(context);
        }
        catch (Exception exception)
        {
            GD.PushError(
                $"[CardFX] 创建 {binding.Effect.DebugName} 失败：{exception}");
            return;
        }

        if (node is null || _layer is null)
        {
            return;
        }

        CardFxPlacement placement = binding.Effect.Placement;
        Color targetModulate = binding.Effect.Modulate;

        node.Position = context.Resolve(
            placement.Anchor,
            placement.Offset);
        node.Scale *= placement.Scale;
        node.RotationDegrees = placement.RotationDegrees;
        node.ZIndex = placement.ZIndex;
        node.Modulate = binding.Effect.FadeInSeconds > 0.0f
            ? new Color(
                targetModulate.R,
                targetModulate.G,
                targetModulate.B,
                0.0f)
            : targetModulate;

        _layer.AddChild(node);

        ActiveFx activeFx = new(
            context.Card,
            binding.Slot,
            binding.Effect,
            node);

        ActiveEffects.Add(activeFx);

        try
        {
            binding.Effect.Start(node);
        }
        catch (Exception exception)
        {
            GD.PushError(
                $"[CardFX] 启动 {binding.Effect.DebugName} 失败：{exception}");
            Stop(activeFx, useFade: false);
            return;
        }

        if (binding.Effect.FadeInSeconds > 0.0f)
        {
            Tween fadeIn = node.CreateTween();
            fadeIn.TweenProperty(
                node,
                "modulate:a",
                targetModulate.A,
                binding.Effect.FadeInSeconds);
        }

        if (placement.MoveToEnd)
        {
            float moveDuration = placement.MoveDuration > 0.0f
                ? placement.MoveDuration
                : binding.Effect.DurationSeconds;

            Vector2 endPosition = context.Resolve(
                placement.EndAnchor,
                placement.EndOffset);

            Tween moveTween = node.CreateTween();
            moveTween.TweenProperty(
                    node,
                    "position",
                    endPosition,
                    moveDuration)
                .SetTrans(placement.MoveTransition)
                .SetEase(placement.MoveEase);
        }

        if (!binding.Effect.Persistent)
        {
            ScheduleLifetime(activeFx);
        }
    }

    private static void ScheduleLifetime(ActiveFx active)
    {
        if (!GodotObject.IsInstanceValid(active.Node))
        {
            return;
        }

        float fadeDuration = Math.Clamp(
            active.Definition.FadeOutSeconds,
            0.0f,
            active.Definition.DurationSeconds);
        float holdDuration =
            active.Definition.DurationSeconds - fadeDuration;

        Tween tween = active.Node.CreateTween();
        active.LifetimeTween = tween;

        if (holdDuration > 0.0f)
        {
            tween.TweenInterval(holdDuration);
        }

        if (fadeDuration > 0.0f)
        {
            tween.TweenProperty(
                active.Node,
                "modulate:a",
                0.0f,
                fadeDuration);
        }

        tween.TweenCallback(
            Callable.From(() => Finish(active)));
    }

    private static void StopSlot(
        CardModel card,
        string slot)
    {
        foreach (ActiveFx active in ActiveEffects
                     .Where(active =>
                         ReferenceEquals(active.Card, card) &&
                         string.Equals(
                             active.Slot,
                             slot,
                             StringComparison.Ordinal))
                     .ToArray())
        {
            Stop(active, useFade: true);
        }
    }

    private static void Stop(
        ActiveFx active,
        bool useFade)
    {
        ActiveEffects.Remove(active);
        active.LifetimeTween?.Kill();

        if (!GodotObject.IsInstanceValid(active.Node))
        {
            return;
        }

        active.Definition.BeforeStop(active.Node);

        if (useFade && active.Definition.FadeOutSeconds > 0.0f)
        {
            Tween tween = active.Node.CreateTween();
            tween.TweenProperty(
                active.Node,
                "modulate:a",
                0.0f,
                active.Definition.FadeOutSeconds);
            tween.TweenCallback(
                Callable.From(() => QueueFree(active.Node)));
            return;
        }

        QueueFree(active.Node);
    }

    private static void Finish(ActiveFx active)
    {
        ActiveEffects.Remove(active);

        if (GodotObject.IsInstanceValid(active.Node))
        {
            active.Definition.BeforeStop(active.Node);
            active.Node.QueueFree();
        }
    }

    private static void QueueFree(Node node)
    {
        if (GodotObject.IsInstanceValid(node))
        {
            node.QueueFree();
        }
    }

    private static bool EnsureLayer()
    {
        NCombatRoom? room = NCombatRoom.Instance;

        if (room is null || !GodotObject.IsInstanceValid(room))
        {
            return false;
        }

        if (_layer is not null && GodotObject.IsInstanceValid(_layer))
        {
            return true;
        }

        ActiveEffects.Clear();

        _layer = new CanvasLayer
        {
            Name = "PaleRegentCardFxLayer",
            Layer = 90
        };

        room.AddChild(_layer);
        return true;
    }

    private static void PruneInvalidEffects()
    {
        ActiveEffects.RemoveAll(
            active => !GodotObject.IsInstanceValid(active.Node));
    }
}
