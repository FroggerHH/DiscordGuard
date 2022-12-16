using System;
using UnityEngine;

namespace DiscordGuard
{
    public class DiscordGuard : MonoBehaviour
    {
        public ZNetView nview;        
        public PrivateArea privateArea;        
        void Awake()
        {
            nview = GetComponent<ZNetView>();
            privateArea = GetComponent<PrivateArea>();

            if(!Plugin.guards.Contains(this)) Plugin.guards.Add(this);
        }

        private void OnDestroy()
        {
            if (Plugin.guards.Contains(this)) Plugin.guards.Remove(this);
            Plugin.currentDiscordGuard = null;
        }
    }
}
