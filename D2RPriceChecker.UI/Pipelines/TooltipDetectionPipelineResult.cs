using System.Drawing;

namespace D2RPriceChecker.Pipelines;

public class TooltipDetectionPipelineResult
{
    public Bitmap Screenshot { get; set; }
    public Bitmap? BorderMask { get; set; }
    public Bitmap? Components { get; set; }
    public Bitmap? BorderOverlay { get; set; }
    public Bitmap? Tooltip { get; set; }

    public bool IsTooltipFound()
    {
        if (Tooltip is null)
            return false;

        if (Tooltip.Width < 300)
            return false;

        return true;
    }

    public TooltipDetectionPipelineResult(Bitmap screenshot)
    {
        Screenshot = screenshot;
    }
}
