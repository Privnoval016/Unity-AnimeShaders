using System.Collections.Generic;
using UnityEngine;

namespace AnimeShaders
{
    /**
     * <summary>Every owner's values on each renderer slot, written as one property block, so no writer wipes another's
     * (a property block is replaced whole and cannot drop one key). A slot no owner holds goes back to no block, so it
     * batches as authored.</summary>
     */
    public static class AnimeBlocks
    {
        #region Private Fields

        private static readonly Dictionary<(Renderer Renderer, int Slot), List<(IAnimeBlockOwner Owner, AnimeBlockValues Values)>> Slots = new();
        private static readonly List<(Renderer Renderer, int Slot)> Released = new();
        private static readonly MaterialPropertyBlock Block = new();

        #endregion

        #region Public API

        /**
         * <summary>Holds <paramref name="values" /> as <paramref name="owner" />'s on <paramref name="renderer" />'s
         * material slot <paramref name="slot" /> (empty or none: the owner's hold let go) and writes the slot's block
         * from every owner's values.</summary>
         */
        public static void Write(IAnimeBlockOwner owner, Renderer renderer, int slot, AnimeBlockValues values)
        {
            if (renderer == null)
                return;
            (Renderer, int) key = (renderer, slot);
            if (!Slots.TryGetValue(key, out List<(IAnimeBlockOwner Owner, AnimeBlockValues Values)> owners))
            {
                if (values == null || values.IsEmpty)
                    return;
                owners = new List<(IAnimeBlockOwner, AnimeBlockValues)>();
                Slots[key] = owners;
            }
            int at = IndexOf(owners, owner);
            if (values == null || values.IsEmpty)
            {
                if (at >= 0)
                    owners.RemoveAt(at);
            }
            else if (at >= 0)
            {
                owners[at] = (owner, values);
            }
            else
            {
                owners.Add((owner, values));
            }
            Apply(key, owners);
        }

        /**
         * <summary>Lets go of every slot <paramref name="owner" /> holds, rewriting each from the owners left, and drops
         * the slots of renderers since destroyed.</summary>
         */
        public static void Release(IAnimeBlockOwner owner)
        {
            Released.Clear();
            foreach (KeyValuePair<(Renderer Renderer, int Slot), List<(IAnimeBlockOwner Owner, AnimeBlockValues Values)>> slot in Slots)
            {
                if (slot.Key.Renderer == null || RemoveOwner(slot.Value, owner))
                    Released.Add(slot.Key);
            }
            foreach ((Renderer Renderer, int Slot) key in Released)
                Apply(key, Slots[key]);
        }

        #endregion

        #region Helpers

        /** <summary>Empties the store as play starts, for a project that enters Play Mode without reloading its scripts.</summary> */
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Slots.Clear();
            Released.Clear();
        }

        private static bool RemoveOwner(List<(IAnimeBlockOwner Owner, AnimeBlockValues Values)> owners, IAnimeBlockOwner owner)
        {
            int at = IndexOf(owners, owner);
            if (at < 0)
                return false;
            owners.RemoveAt(at);
            return true;
        }

        private static int IndexOf(List<(IAnimeBlockOwner Owner, AnimeBlockValues Values)> owners, IAnimeBlockOwner owner)
        {
            for (int i = 0; i < owners.Count; i++)
            {
                if (ReferenceEquals(owners[i].Owner, owner))
                    return i;
            }
            return -1;
        }

        private static void Apply((Renderer Renderer, int Slot) key, List<(IAnimeBlockOwner Owner, AnimeBlockValues Values)> owners)
        {
            if (key.Renderer == null || owners.Count == 0)
            {
                Slots.Remove(key);
                if (key.Renderer != null)
                    key.Renderer.SetPropertyBlock(null, key.Slot);
                return;
            }
            Block.Clear();
            foreach ((IAnimeBlockOwner _, AnimeBlockValues values) in owners)
                values.AddTo(Block);
            key.Renderer.SetPropertyBlock(Block, key.Slot);
        }

        #endregion
    }
}
