using Deepleo.ImageMerge;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace ImageMerge.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images");
            var files = Directory.GetFiles(dir);
            var images = new List<string>(files);
            TestLocal(images).ConfigureAwait(false).GetAwaiter().GetResult();
            //TestNewwork();
        }

        /// <summary>
        /// 测试网络图片
        /// 如果遇到网络图片过期，请更换图片地址即可
        /// </summary>
        private static async Task TestNewwork()
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images");
            var images = new List<string>()  {
               "https://avatars2.githubusercontent.com/u/5965882?s=460&v=4",
               "https://avatars2.githubusercontent.com/u/2503423?s=460&v=4",
               "https://avatars2.githubusercontent.com/u/499550?s=460&v=4",
               "https://avatars2.githubusercontent.com/u/233907?s=400&v=4" };
            Size size = new Size(400, 400);
            var path = "网络图片";
            await TestMerge(path, images, MergeLayoutEnum.Merge1C, size);
            await TestMerge(path, images, MergeLayoutEnum.Merge2LR, size);
            await TestMerge(path, images, MergeLayoutEnum.Merge2TB, size);

            await TestMerge(path, images, MergeLayoutEnum.Merge3L1R2, size);
            await TestMerge(path, images, MergeLayoutEnum.Merge3L2R1, size);
            await TestMerge(path, images, MergeLayoutEnum.Merge3T1B2, size);
            await TestMerge(path, images, MergeLayoutEnum.Merge3T2B1, size);

            await TestMerge(path, images, MergeLayoutEnum.Merge4S, size);

            await TestMerge(path, images, MergeLayoutEnum.Merge8L4R4, new Size(400, 400));
            await TestMerge(path, images, MergeLayoutEnum.Merge8T4B4, new Size(400, 200));

            Console.WriteLine("网络图片测试完成");
        }

        /// <summary>
        /// 测试本地图片
        /// </summary>
        private static async Task TestLocal(List<string> images)
        {
            Size size = new Size(400, 400);
            var path = "本地图片";
            await TestMerge(path, images, MergeLayoutEnum.Merge1C, size);
            await TestMerge(path, images, MergeLayoutEnum.Merge2LR, size);
            await TestMerge(path, images, MergeLayoutEnum.Merge2TB, size);

            await TestMerge(path, images, MergeLayoutEnum.Merge3L1R2, size);
            await TestMerge(path, images, MergeLayoutEnum.Merge3L2R1, size);
            await TestMerge(path, images, MergeLayoutEnum.Merge3T1B2, size);
            await TestMerge(path, images, MergeLayoutEnum.Merge3T2B1, size);

            await TestMerge(path, images, MergeLayoutEnum.Merge4S, size);

            await TestMerge(path, images, MergeLayoutEnum.Merge8L4R4, new Size(400, 400));
            await TestMerge(path, images, MergeLayoutEnum.Merge8T4B4, new Size(400, 200));

            Console.WriteLine("本地图片测试完成");
        }

        private static async Task TestMerge(string type, List<string> imagePath, MergeLayoutEnum layout, Size size)
        {
            var m1 = await ImageMergeHelper.MergeImagesAsync(imagePath, layout, size);
            var m1Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, type + "-" + layout.ToString() + ".png");
            if (File.Exists(m1Path))
            {
                File.Delete(m1Path);
            }
            m1.Save(m1Path);
        }
    }
}
