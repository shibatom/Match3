

using Internal.Scripts.Blocks;

namespace Internal.Scripts.Items
{
    public interface IColorGettable
    {
        int GenColor(Rectangle rectangle, int maxColors = 6, int exceptColor = -1, bool onlyNONEType = false);
    }
}