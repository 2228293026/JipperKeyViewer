using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    public interface IRainSettings
    {
        Color RainColor { get; }
        Color RainColor2 { get; }
        Color RainColor3 { get; }
        bool EnableRainForRow1 { get; }
        bool EnableRainForRow2 { get; }
        bool EnableRainForRow3 { get; }
        float RainSpeedRow1 { get; }
        float RainSpeedRow2 { get; }
        float RainSpeedRow3 { get; }
        float RainHeightRow1 { get; }
        float RainHeightRow2 { get; }
        float RainHeightRow3 { get; }
        bool EnableRainFade { get; }
    }
}
