using ImageMerge;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Deepleo.ImageMerge
{
    /// <summary>
    /// 
    /// </summary>
    public class ImageMergeHelper
    {
        /// <summary>
        /// 合并N张图片（网络/本地）
        /// </summary>
        /// <param name="imageUrls">图片路径（网路路径或者本地路径）</param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static async Task<Image> MergeImagesAsync(List<string> imageUrls, MergeLayoutEnum mergeLayout, Size size)
        {
            if (imageUrls == null || imageUrls.Count <= 0)
            {
                return null;
            }

            return await MergeImagesInternalAsync(imageUrls, mergeLayout, size);
        }

        private static async Task<Image> MergeImagesInternalAsync(List<string> imageUrls, MergeLayoutEnum mergeLayout, Size size)
        {
            int picCount = 0;
            var stringLayout = mergeLayout.ToString();
            if (stringLayout.StartsWith("Merge1C"))
            {
                picCount = 1;
            }
            else if (stringLayout.StartsWith("Merge2"))
            {
                picCount = 2;
            }
            else if (stringLayout.StartsWith("Merge3"))
            {
                picCount = 3;
            }
            else if (stringLayout.StartsWith("Merge4"))
            {
                picCount = 4;
            }
            else if (stringLayout.StartsWith("Merge8"))
            {
                picCount = 8;
            }

            if (imageUrls.Count < picCount)
            {
                throw new ArgumentException("argument invalid.");
            }

            List<Image> list = new List<Image>(picCount);
            for (int i = 0; i < picCount; i++)
            {
                var bytes = await GetImageByteArrayAsync(imageUrls[i]);
                if (bytes != null && bytes.Length > 0)
                {
                    list.Add(ConvertToImage(bytes));
                }
            }

            var result = MergeImages(list, size, mergeLayout);
            return result;
        }

        /// <summary>
        /// 合并2张图片
        /// </summary>
        /// <param name="image1"></param>
        /// <param name="image2"></param>
        /// <param name="layout"></param>
        /// <returns></returns>
        private static Image MergeImages(List<Image> imageList, Size size, MergeLayoutEnum layout = MergeLayoutEnum.Merge2LR)
        {
            var width = size.Width;
            var height = size.Height;
            var pf = PixelFormat.Format32bppArgb;
            using (var bg = new Bitmap(width, height, pf))
            {
                using (var g = Graphics.FromImage(bg))
                {
                    g.FillRectangle(Brushes.Transparent, 0, 0, width, height);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.CompositingQuality = CompositingQuality.HighQuality;

                    List<ImageElement> list = GetImageElements(imageList, size, layout);

                    list.ForEach(data =>
                    {
                        var dest = ZoomRectangle(data.Image.Width, data.Image.Height, data.DestRectangle.Height, data.DestRectangle.Width);
                        dest = new Rectangle(dest.X + data.DestRectangle.X + data.OffsetX, dest.Y + data.DestRectangle.Y + data.OffsetY, dest.Width, dest.Height);
                        g.DrawImage(data.Image, dest, data.SrcRectangle, GraphicsUnit.Pixel);
                        data.Image.Dispose();
                    });

                    g.Save();
                }

                imageList.ForEach(image => { image.Dispose(); });

                using (var ms = new MemoryStream())
                {
                    bg.Save(ms, ImageFormat.Png);
                    var buffers = ms.ToArray();
                    return ConvertToImage(buffers);
                }
            }
        }

        /// <summary>
        /// 获取图片数据
        /// </summary>
        /// <param name="imageUrl"></param>
        /// <returns></returns>
        private static async Task<byte[]> GetImageByteArrayAsync(string imagePath)
        {
            if (imagePath.StartsWith("http"))
            {
                using (HttpClient httpClient = new HttpClient())
                using (var ms = new MemoryStream())
                {
                    var stream = await httpClient.GetStreamAsync(imagePath);
                    stream.Seek(0, SeekOrigin.Begin);
                    await stream.CopyToAsync(ms);
                    return ms.ToArray();
                }
            }

            using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
            using (var ms = new MemoryStream())
            {
                await fs.CopyToAsync(ms);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 将byte数组转化为Image
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static Image ConvertToImage(byte[] buffer)
        {
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                return Image.FromStream(ms);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orginal"></param>
        /// <param name="destHeight"></param>
        /// <param name="destWidth"></param>
        /// <param name="zoom">只在原图小于目标尺寸，true:裁剪或居中显示，false返回原图</param>
        /// <returns></returns>
        private static (Image, Rectangle) ZoomImage(Image orginal, int destHeight, int destWidth, bool zoom)
        {
            try
            {
                Image sourImage = orginal;
                var dest = ZoomRectangle(sourImage.Width, sourImage.Height, destHeight, destWidth);
                Image destBitmap = new Bitmap(destWidth, destHeight);
                using (Graphics g = Graphics.FromImage(destBitmap))
                {
                    g.FillRectangle(Brushes.Transparent, 0, 0, destWidth, destHeight);
                    //设置画布的描绘质量           
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(sourImage, dest, new Rectangle(0, 0, sourImage.Width, sourImage.Height), GraphicsUnit.Pixel);
                    sourImage.Dispose();
                    return (destBitmap, dest);
                }
            }
            catch
            {
                return (orginal, new Rectangle());
            }
        }

        private static Rectangle ZoomRectangle(int sourWidth, int sourHeight, int destHeight, int destWidth)
        {
            int width = 0, height = 0;
            //按比例缩放             
            if (sourHeight > destHeight || sourWidth > destWidth)
            {
                if ((sourWidth * destHeight) > (sourHeight * destWidth))
                {
                    width = destWidth;
                    height = (destWidth * sourHeight) / sourWidth;
                }
                else
                {
                    height = destHeight;
                    width = (sourWidth * destHeight) / sourHeight;
                }
            }
            else
            {
                width = sourWidth;
                height = sourHeight;
            }

            return new Rectangle((destWidth - width) / 2, (destHeight - height) / 2, width, height);
        }

        private static List<ImageElement> GetImageElements(List<Image> images, Size size, MergeLayoutEnum mergeLayout)
        {
            List<ImageElement> list = new List<ImageElement>();
            var height = size.Height;
            var width = size.Width;
            int newHeight = 0;
            int newWidth = 0;

            switch (mergeLayout)
            {
                case MergeLayoutEnum.Merge1C:
                    for (int i = 0; i < images.Count; i++)
                    {
                        Rectangle destRectangle = new Rectangle(0, 0, width, height);
                        var imageElement = GenerateImageElement(images[i], destRectangle, OffsetXType.NoOffsetX, OffsetYType.NoOffsetY);
                        list.Add(imageElement);
                    }

                    break;

                case MergeLayoutEnum.Merge2TB:
                    for (int i = 0; i < images.Count; i++)
                    {
                        newHeight = height / 2;     // =125
                        newWidth = width;          //  =250
                        (var zoomImage, var dest) = ZoomImage(images[i], newHeight, newWidth, true);
                        var element = new ImageElement { Image = zoomImage, SrcRectangle = new Rectangle(0, 0, zoomImage.Width, zoomImage.Height) };
                        element.DestRectangle = new Rectangle(0, 0, newWidth, newHeight);
                        if (i == 0)
                        {
                            element.OffsetY = dest.Y;
                        }
                        else if (i == 1)
                        {
                            element.DestRectangle = new Rectangle(0, size.Height / 2, newWidth, newHeight);
                            element.OffsetY = -dest.Y;
                        }
                        list.Add(element);
                    }

                    break;

                case MergeLayoutEnum.Merge2LR:
                    for (int i = 0; i < images.Count; i++)
                    {
                        newWidth = width / 2;//   =125
                        newHeight = height;  //   =250
                        (var zoomImage, var dest) = ZoomImage(images[i], newHeight, newWidth, true);
                        var element = new ImageElement { Image = zoomImage, SrcRectangle = new Rectangle(0, 0, zoomImage.Width, zoomImage.Height) };
                        element.DestRectangle = new Rectangle(0, 0, newWidth, newHeight);
                        if (i == 0)
                        {
                            element.OffsetX = dest.X;
                        }
                        if (i == 1)
                        {
                            element.DestRectangle = new Rectangle(size.Width / 2, 0, newWidth, newHeight);
                            element.OffsetX = -dest.X;
                        }
                        list.Add(element);
                    }

                    break;

                case MergeLayoutEnum.Merge3T1B2:
                    for (int i = 0; i < images.Count; i++)
                    {
                        Rectangle destRectangle;
                        OffsetXType offsetX = OffsetXType.NoOffsetX;
                        OffsetYType offsetY = OffsetYType.NoOffsetY;

                        if (i == 0)
                        {
                            newHeight = height / 2;// =125
                            newWidth = width;//       =250
                            destRectangle = new Rectangle(0, 0, newWidth, newHeight);
                            offsetY = OffsetYType.PositiveY;
                        }
                        else if (i == 1)
                        {
                            newHeight = height / 2;//  =125
                            newWidth = width / 2;  //  =125
                            destRectangle = new Rectangle(0, height / 2, newWidth, newHeight);
                            offsetX = OffsetXType.PositiveX;
                            offsetY = OffsetYType.NegativeY;
                        }
                        else
                        {
                            newHeight = height / 2; //  =125
                            newWidth = width / 2;   //  =125
                            destRectangle = new Rectangle(width / 2, height / 2, newWidth, newHeight);
                            offsetY = OffsetYType.NegativeY;
                            offsetX = OffsetXType.NegativeX;
                        }

                        var imageElement = GenerateImageElement(images[i], destRectangle, offsetX, offsetY);
                        list.Add(imageElement);
                    }
                    break;

                case MergeLayoutEnum.Merge3T2B1:
                    for (int i = 0; i < images.Count; i++)
                    {
                        Rectangle destRectangle;
                        OffsetXType offsetX = OffsetXType.NoOffsetX;
                        OffsetYType offsetY = OffsetYType.NoOffsetY;
                        if (i == 0)
                        {
                            newHeight = height / 2;//  =125
                            newWidth = width / 2;  //  =125
                            destRectangle = new Rectangle(0, 0, newWidth, newHeight);
                            offsetX = OffsetXType.PositiveX;
                            offsetY = OffsetYType.PositiveY;
                        }
                        else if (i == 1)
                        {
                            newHeight = height / 2; //  =125
                            newWidth = width / 2;   //  =125
                            destRectangle = new Rectangle(width / 2, 0, newWidth, newHeight);
                            offsetY = OffsetYType.PositiveY;
                            offsetX = OffsetXType.NegativeX;
                        }
                        else
                        {
                            newHeight = height / 2;// =125
                            newWidth = width;//       =250
                            destRectangle = new Rectangle(0, height / 2, newWidth, newHeight);
                            offsetY = OffsetYType.NegativeY;
                        }

                        var imageElement = GenerateImageElement(images[i], destRectangle, offsetX, offsetY);
                        list.Add(imageElement);
                    }
                    break;

                case MergeLayoutEnum.Merge3L1R2:
                    for (int i = 0; i < images.Count; i++)
                    {
                        Rectangle destRectangle;
                        OffsetXType offsetX = OffsetXType.NoOffsetX;
                        OffsetYType offsetY = OffsetYType.NoOffsetY;
                        if (i == 0)
                        {
                            newHeight = height;//     =250
                            newWidth = width / 2;//   =125
                            destRectangle = new Rectangle(0, 0, newWidth, newHeight);
                            offsetX = OffsetXType.PositiveX;
                        }
                        else if (i == 1)
                        {
                            newHeight = height / 2;//  =125
                            newWidth = width / 2;  //  =125
                            destRectangle = new Rectangle(width / 2, 0, newWidth, newHeight);
                            offsetX = OffsetXType.NegativeX;
                            offsetY = OffsetYType.PositiveY;
                        }
                        else
                        {
                            newHeight = height / 2; //  =125
                            newWidth = width / 2;   //  =125
                            destRectangle = new Rectangle(width / 2, height / 2, newWidth, newHeight);
                            offsetX = OffsetXType.NegativeX;
                            offsetY = OffsetYType.NegativeY;
                        }

                        var imageElement = GenerateImageElement(images[i], destRectangle, offsetX, offsetY);
                        list.Add(imageElement);
                    }
                    break;

                case MergeLayoutEnum.Merge3L2R1:
                    for (int i = 0; i < images.Count; i++)
                    {
                        Rectangle destRectangle;
                        OffsetXType offsetX = OffsetXType.NoOffsetX;
                        OffsetYType offsetY = OffsetYType.NoOffsetY;
                        if (i == 0)
                        {
                            newHeight = height / 2;//  =125
                            newWidth = width / 2;  //  =125
                            destRectangle = new Rectangle(0, 0, newWidth, newHeight);
                            offsetX = OffsetXType.PositiveX;
                            offsetY = OffsetYType.PositiveY;
                        }
                        else if (i == 1)
                        {
                            newHeight = height / 2; //  =125
                            newWidth = width / 2;   //  =125
                            destRectangle = new Rectangle(0, height / 2, newWidth, newHeight);
                            offsetX = OffsetXType.PositiveX;
                            offsetY = OffsetYType.NegativeY;
                        }
                        else
                        {
                            newHeight = height;//     =250
                            newWidth = width / 2;//   =125
                            destRectangle = new Rectangle(width / 2, 0, newWidth, newHeight);
                            offsetX = OffsetXType.NegativeX;
                        }

                        var imageElement = GenerateImageElement(images[i], destRectangle, offsetX, offsetY);
                        list.Add(imageElement);
                    }
                    break;

                case MergeLayoutEnum.Merge4S:
                    for (int i = 0; i < images.Count; i++)
                    {
                        Rectangle destRectangle;
                        newHeight = height / 2;//  =125
                        newWidth = width / 2;  //  =125
                        OffsetXType offsetX = OffsetXType.NoOffsetX;
                        OffsetYType offsetY = OffsetYType.NoOffsetY;

                        if (i == 0)
                        {
                            destRectangle = new Rectangle(0, 0, newWidth, newHeight);
                            offsetX = OffsetXType.PositiveX;
                            offsetY = OffsetYType.PositiveY;
                        }
                        else if (i == 1)
                        {
                            destRectangle = new Rectangle(width / 2, 0, newWidth, newHeight);
                            offsetX = OffsetXType.NegativeX;
                            offsetY = OffsetYType.PositiveY;
                        }
                        else if (i == 2)
                        {
                            destRectangle = new Rectangle(0, height / 2, newWidth, newHeight);
                            offsetX = OffsetXType.PositiveX;
                            offsetY = OffsetYType.NegativeY;
                        }
                        else
                        {
                            destRectangle = new Rectangle(width / 2, height / 2, newWidth, newHeight);
                            offsetX = OffsetXType.NegativeX;
                            offsetY = OffsetYType.NegativeY;
                        }

                        var imageElement = GenerateImageElement(images[i], destRectangle, offsetX, offsetY);
                        list.Add(imageElement);
                    }
                    break;

                case MergeLayoutEnum.Merge8T4B4:
                    for (int i = 0; i < images.Count; i++)
                    {
                        Rectangle destRectangle;
                        newHeight = height / 2;//  =125
                        newWidth = width / 4;  //  =125
                        OffsetXType offsetX = OffsetXType.NoOffsetX;
                        OffsetYType offsetY = OffsetYType.NoOffsetY;

                        if (i == 0)
                        {
                            destRectangle = new Rectangle(0, 0, newWidth, newHeight);
                            offsetY = OffsetYType.PositiveY;
                        }
                        else if (i == 1)
                        {
                            destRectangle = new Rectangle(width / 4, 0, newWidth, newHeight);
                            offsetY = OffsetYType.PositiveY;
                        }
                        else if (i == 2)
                        {
                            destRectangle = new Rectangle(width / 2, 0, newWidth, newHeight);
                            offsetY = OffsetYType.PositiveY;
                        }
                        else if (i == 3)
                        {
                            destRectangle = new Rectangle(width / 4 * 3, 0, newWidth, newHeight);
                            offsetY = OffsetYType.PositiveY;
                        }
                        else if (i == 4)
                        {
                            destRectangle = new Rectangle(0, height / 2, newWidth, newHeight);
                            offsetY = OffsetYType.NegativeY;
                        }
                        else if (i == 5)
                        {
                            destRectangle = new Rectangle(width / 4, height / 2, newWidth, newHeight);
                            offsetY = OffsetYType.NegativeY;
                        }
                        else if (i == 6)
                        {
                            destRectangle = new Rectangle(width / 2, height / 2, newWidth, newHeight);
                            offsetY = OffsetYType.NegativeY;
                        }
                        else
                        {
                            destRectangle = new Rectangle(width / 4 * 3, height / 2, newWidth, newHeight);
                            offsetY = OffsetYType.NegativeY;
                        }

                        var imageElement = GenerateImageElement(images[i], destRectangle, offsetX, offsetY);
                        list.Add(imageElement);
                    }
                    break;

                case MergeLayoutEnum.Merge8L4R4:
                    for (int i = 0; i < images.Count; i++)
                    {
                        Rectangle destRectangle;
                        newHeight = height / 4;//  =125
                        newWidth = width / 2;  //  =125
                        OffsetXType offsetX = OffsetXType.NoOffsetX;
                        OffsetYType offsetY = OffsetYType.NoOffsetY;

                        if (i == 0)
                        {
                            destRectangle = new Rectangle(0, 0, newWidth, newHeight);
                            offsetX = OffsetXType.PositiveX;
                        }
                        else if (i == 1)
                        {
                            destRectangle = new Rectangle(0, height / 4, newWidth, newHeight);
                            offsetX = OffsetXType.PositiveX;
                        }
                        else if (i == 2)
                        {
                            destRectangle = new Rectangle(0, height / 2, newWidth, newHeight);
                            offsetX = OffsetXType.PositiveX;
                        }
                        else if (i == 3)
                        {
                            destRectangle = new Rectangle(0, height / 4 * 3, newWidth, newHeight);
                            offsetX = OffsetXType.PositiveX;
                        }
                        else if (i == 4)
                        {
                            destRectangle = new Rectangle(width / 2, 0, newWidth, newHeight);
                            offsetX = OffsetXType.NegativeX;
                        }
                        else if (i == 5)
                        {
                            destRectangle = new Rectangle(width / 2, height / 4, newWidth, newHeight);
                            offsetX = OffsetXType.NegativeX;
                        }
                        else if (i == 6)
                        {
                            destRectangle = new Rectangle(width / 2, height / 2, newWidth, newHeight);
                            offsetX = OffsetXType.NegativeX;
                        }
                        else
                        {
                            destRectangle = new Rectangle(width / 2, height / 4 * 3, newWidth, newHeight);
                            offsetX = OffsetXType.NegativeX;
                        }

                        var imageElement = GenerateImageElement(images[i], destRectangle, offsetX, offsetY);
                        list.Add(imageElement);
                    }
                    break;
            }

            return list;
        }

        private static ImageElement GenerateImageElement(Image image, Rectangle destRectangle, OffsetXType offsetX, OffsetYType offsetY)
        {
            (var zoomImage, var dest) = ZoomImage(image, destRectangle.Height, destRectangle.Width, true);
            Rectangle srcRectangle = new Rectangle(0, 0, zoomImage.Width, zoomImage.Height);
            var imageElement = new ImageElement { Image = zoomImage, DestRectangle = destRectangle, SrcRectangle = srcRectangle };
            switch (offsetX)
            {
                case OffsetXType.PositiveX:
                    imageElement.OffsetX = dest.X;
                    break;

                case OffsetXType.NegativeX:
                    imageElement.OffsetX = -dest.X;
                    break;

                case OffsetXType.NoOffsetX:
                    imageElement.OffsetX = 0;
                    break;
            }

            switch (offsetY)
            {
                case OffsetYType.PositiveY:
                    imageElement.OffsetY = dest.Y;
                    break;

                case OffsetYType.NegativeY:
                    imageElement.OffsetY = -dest.Y;
                    break;

                case OffsetYType.NoOffsetY:
                    imageElement.OffsetY = 0;
                    break;
            }

            return imageElement;
        }
    }
}

