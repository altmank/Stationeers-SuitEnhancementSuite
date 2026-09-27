using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using UnityEngine;

namespace SuitEnhancementSuite;

/// <summary>
/// Uses the waste bag in the Waste slot the way a hand-held bag is used: an open bag through its own OnUseItem, a
/// folded bag by unfolding one into the slot as SanitationStack.UnfoldAndFill does. Unlike by hand, the bag is used
/// inside a suit (SanitationPacket.HumanChecks refuses that through Human.CanDefecate), as the Food and Water slots
/// also serve with the helmet closed.
/// </summary>
internal static class WasteBags
{
    // SanitationStack.UnfoldAndFill: the new bag appears half a metre in front of the player.
    private const float UnfoldDistance = 0.5f;

    public static StepResult Use(Slot slot, PlayerInventory player, ConsumptionPolicy policy, out float amount)
    {
        amount = 0f;
        if (!policy.ShouldRelieve(player.Human.SanitationRatio)) return StepResult.Idle;
        return slot.Get() switch
        {
            SanitationPacket packet => Fill(packet, player, out amount),
            SanitationStack stack => Unfold(stack, slot, player, out amount),
            _ => StepResult.Idle,
        };
    }

    private static StepResult Fill(SanitationPacket packet, PlayerInventory player, out float amount)
    {
        amount = 0f;
        var before = packet.Quantity;
        if (packet.IsStackFull || !packet.OnUseItem(1f, player.Human)) return StepResult.Idle;
        amount = (packet.Quantity - before) / packet.MaxQuantity;
        return StepResult.Done;
    }

    /// <summary>
    /// A last bag leaves the slot as vanilla's does (to the world, where its stack is destroyed at zero); the rest of a
    /// bigger stack needs a free inventory slot, found before anything is created.
    /// </summary>
    private static StepResult Unfold(SanitationStack stack, Slot slot, PlayerInventory player, out float amount)
    {
        amount = 0f;
        if (stack.PacketPrefab == null) return StepResult.Idle;
        Slot rest = null;
        if (stack.Quantity > 1 && !player.TryFindFreeSlot(stack, out rest)) return StepResult.NoRoomFor(stack);
        var human = player.Human;
        var forward = human.CharacterRotationY.rotation * Vector3.forward;
        var packet = OnServer.Create<SanitationPacket>(stack.PacketPrefab,
            stack.GetSafeDropPosition(stack.Position, forward, UnfoldDistance), stack.Rotation);
        if (packet == null) return StepResult.Idle;
        packet.DestroyAtZero = false;
        packet.SetQuantity(0f);
        if (!packet.OnUseItem(1f, human))
        {
            OnServer.Destroy(packet);
            return StepResult.Idle;
        }
        if (rest == null) OnServer.MoveToWorld(stack, stack.Position, stack.Rotation, Vector3.zero, Vector3.zero);
        else Moves.ToSlot(stack, rest);
        if (!Moves.ToSlot(packet, slot) && !player.TryStow(packet))
            Plugin.Log.LogWarning($"Auto consume: the waste bag unfolded for {human.DisplayName} found no slot and lies at their feet.");
        stack.DecrementQuantity();
        amount = packet.Quantity / packet.MaxQuantity;
        return StepResult.Done;
    }
}
