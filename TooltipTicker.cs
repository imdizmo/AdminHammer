using SeneaLUI.Components;
using UnityEngine;

namespace Jatify.AdminHammer
{
    // SeneaL shows its tooltip only while one of its own windows is open, and only the first
    // Tooltip.Tick of a frame counts. This runs before everything else, so while the admin panel is
    // open the tooltip sits on our canvas and is allowed to show.
    [DefaultExecutionOrder(-10000)]
    internal sealed class TooltipTicker : MonoBehaviour
    {
        internal static Panel Panel;

        private void Update()
        {
            if (Panel == null || !Panel.Open) return;
            Tooltip.Attach(Panel.Root.parent);
            Tooltip.Tick(true);
        }
    }
}
