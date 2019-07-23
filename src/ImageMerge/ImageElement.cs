using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace ImageMerge
{
    public class ImageElement
    {
        public Image Image { get; set; }

        public Rectangle DestRectangle { get; set; }

        public Rectangle SrcRectangle { get; set; }

        public int OffsetX { get; set; }

        public int OffsetY { get; set; }
    }

    public enum OffsetXType
    {
        /// <summary>
        /// 正数X
        /// </summary>
        PositiveX = 1,

        /// <summary>
        /// 负数X
        /// </summary>
        NegativeX = 2,

        /// <summary>
        /// X无偏移
        /// </summary>
        NoOffsetX = 3,
    }

    public enum OffsetYType
    {
        /// <summary>
        /// 正数Y
        /// </summary>
        PositiveY = 1,

        /// <summary>
        /// 负数Y
        /// </summary>
        NegativeY = 2,

        /// <summary>
        /// Y无偏移
        /// </summary>
        NoOffsetY = 3
    }
}
