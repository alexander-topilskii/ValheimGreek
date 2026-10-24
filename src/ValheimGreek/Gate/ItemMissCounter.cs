using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ValheimGreek
{
    public static class ItemMissCounter
    {
        public const string Key = "valheimgreek_misses";

        private static FieldInfo? _nviewField;

        public static int Get(ItemDrop item)
        {
            var view = View(item);
            if (view != null && view.IsValid())
            {
                var zdo = view.GetZDO();
                if (zdo != null)
                    return zdo.GetInt(Key, 0);
            }

            var local = item.GetComponent<LocalMissCount>();
            return local != null ? local.Misses : 0;
        }

        public static void Increment(ItemDrop item)
        {
            var view = View(item);
            if (view != null && view.IsValid())
            {
                var zdo = view.GetZDO();
                if (zdo != null)
                {
                    if (!view.IsOwner())
                        view.ClaimOwnership();
                    zdo.Set(Key, zdo.GetInt(Key, 0) + 1);
                    return;
                }
            }

            var local = item.GetComponent<LocalMissCount>();
            if (local == null)
                local = item.gameObject.AddComponent<LocalMissCount>();
            local.Misses++;
        }

        private static ZNetView? View(ItemDrop item)
        {
            if (_nviewField == null)
                _nviewField = AccessTools.Field(typeof(ItemDrop), "m_nview");
            if (_nviewField == null)
                return null;
            return _nviewField.GetValue(item) as ZNetView;
        }
    }

    public sealed class LocalMissCount : MonoBehaviour
    {
        public int Misses;
    }
}
