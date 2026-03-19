using System.Drawing;

namespace D2RPriceChecker.Services;

public class TooltipPipelineResult
{
    public Bitmap Screenshot { get; set; }
    public Bitmap? BorderMask { get; set; }
    public Bitmap? Components { get; set; }
    public Bitmap? BorderOverlay { get; set; }
    public Bitmap? Tooltip { get; set; }

    public bool IsTooltipFound => Tooltip != null;

    public TooltipPipelineResult(Bitmap screenshot)
    {
        Screenshot = screenshot;
    }
}
