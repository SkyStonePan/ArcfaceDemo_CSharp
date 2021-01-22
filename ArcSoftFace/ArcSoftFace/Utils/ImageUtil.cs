using ArcSoftFace.Entity;
using ArcSoftFace.SDKModels;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ArcSoftFace.Utils
{
    public class ImageUtil
    {
        /// <summary>
        /// 获取图片信息
        /// </summary>
        /// <param name="image">图片</param>
        /// <returns>成功或失败</returns>
        public static ImageInfo ReadBMP(Image image)
        {
            ImageInfo imageInfo = new ImageInfo();

            //将Image转换为Format24bppRgb格式的BMP
            Bitmap bm = new Bitmap(image);
            BitmapData data = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                //位图中第一个像素数据的地址。它也可以看成是位图中的第一个扫描行
                IntPtr ptr = data.Scan0;

                //定义数组长度
                int soureBitArrayLength = data.Height * Math.Abs(data.Stride);
                byte[] sourceBitArray = new byte[soureBitArrayLength];

                //将bitmap中的内容拷贝到ptr_bgr数组中
                MemoryUtil.Copy(ptr, sourceBitArray, 0, soureBitArrayLength);

                //填充引用对象字段值
                imageInfo.width = data.Width;
                imageInfo.height = data.Height;
                imageInfo.format = ASF_ImagePixelFormat.ASVL_PAF_RGB24_B8G8R8;

                //获取去除对齐位后度图像数据
                int line = imageInfo.width * 3;
                int pitch = Math.Abs(data.Stride);
                int bgr_len = line * imageInfo.height;
                byte[] destBitArray = new byte[bgr_len];

                /*
                 * 图片像素数据在内存中是按行存储，一般图像库都会有一个内存对齐，在每行像素的末尾位置
                 * 每行的对齐位会使每行多出一个像素空间（三通道如RGB会多出3个字节，四通道RGBA会多出4个字节）
                 * 以下循环目的是去除每行末尾的对齐位，将有效的像素拷贝到新的数组
                 */
                for (int i = 0; i < imageInfo.height; ++i)
                {
                    Array.Copy(sourceBitArray, i * pitch, destBitArray, i * line, line);
                }

                imageInfo.imgData = MemoryUtil.Malloc(destBitArray.Length);
                MemoryUtil.Copy(destBitArray, 0, imageInfo.imgData, destBitArray.Length);

                return imageInfo;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                bm.UnlockBits(data);
                bm.Dispose();
            }

            return null;
        }
        
        /// <summary>
        /// 用矩形框标记图片指定区域
        /// </summary>
        /// <param name="image">图片</param>
        /// <param name="startX">矩形框左上角X坐标</param>
        /// <param name="startY">矩形框左上角Y坐标</param>
        /// <param name="width">矩形框宽度</param>
        /// <param name="height">矩形框高度</param>
        /// <returns>标记后的图片</returns>
        public static Image MarkRect(Image image, int startX, int startY, int width, int height)
        {
            Image clone = (Image)image.Clone();
            Graphics g = Graphics.FromImage(clone);
            try
            {
                Brush brush = new SolidBrush(Color.Red);
                Pen pen = new Pen(brush, 2);
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                g.DrawRectangle(pen, new Rectangle(startX, startY, width, height));
                return clone;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                g.Dispose();
            }

            return null;
        }

        /// <summary>
        /// 用矩形框标记图片指定区域，添加年龄和性别标注
        /// </summary>
        /// <param name="image">图片</param>
        /// <param name="startX">矩形框左上角X坐标</param>
        /// <param name="startY">矩形框左上角Y坐标</param>
        /// <param name="width">矩形框宽度</param>
        /// <param name="height">矩形框高度</param>
        /// <param name="age">年龄</param>
        /// <param name="gender">性别</param>
        /// <returns>标记后的图片</returns>
        public static Image MarkRectAndString(Image image, int startX, int startY, int width, int height, int age, int gender,int showWidth)
        {
            Image clone = (Image)image.Clone();
            Graphics g = Graphics.FromImage(clone);
            try
            {
                Brush brush = new SolidBrush(Color.Red);
                int penWidth = image.Width / showWidth;
                Pen pen = new Pen(brush, penWidth > 1? 2* penWidth:2);
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                g.DrawRectangle(pen, new Rectangle(startX < 1 ? 0 : startX, startY < 1 ? 0 : startY, width, height));
                string genderStr = "";
                if(gender >= 0)
                {
                    if(gender == 0)
                    {
                        genderStr = "男";
                    }
                    else if (gender == 1)
                    {
                        genderStr = "女";
                    }
                }
                int fontSize = image.Width / showWidth;
                if (fontSize > 1)
                {
                    int temp = 12;
                    for (int i = 0; i < fontSize; i++)
                    {
                        temp += 6;
                    }
                    fontSize = temp;
                } else if (fontSize == 1) {
                    fontSize = 14;
                } else
                {
                    fontSize = 12;
                }
                g.DrawString(string.Format("Age:{0}   Gender:{1}", age, genderStr), new Font(FontFamily.GenericSerif, fontSize), brush, startX < 1?0:startX, (startY - 20)< 1?0: startY - 20);

                return clone;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                g.Dispose();
            }

            return null;
        }

        /// <summary>
        /// 按指定宽高缩放图片
        /// </summary>
        /// <param name="image">原图片</param>
        /// <param name="dstWidth">目标图片宽</param>
        /// <param name="dstHeight">目标图片高</param>
        /// <returns></returns>
        public static Image ScaleImage(Image image, int dstWidth, int dstHeight)
        {
            Graphics g = null;
            try
            {
                //按比例缩放           
                float scaleRate = getWidthAndHeight(image.Width, image.Height, dstWidth, dstHeight);
                int width = (int)(image.Width * scaleRate);
                int height = (int)(image.Height * scaleRate);

                //将宽度调整为4的整数倍
                if (width % 4 != 0) {
                    width = width - width % 4;
                }

                Bitmap destBitmap = new Bitmap(width, height);
                g = Graphics.FromImage(destBitmap);
                g.Clear(Color.Transparent);

                //设置画布的描绘质量         
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, new Rectangle((width - width) / 2, (height - height) / 2, width, height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);

                //设置压缩质量     
                EncoderParameters encoderParams = new EncoderParameters();
                long[] quality = new long[1];
                quality[0] = 100;
                EncoderParameter encoderParam = new EncoderParameter(Encoder.Quality, quality);
                encoderParams.Param[0] = encoderParam;

                return destBitmap;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                if (g != null)
                {
                    g.Dispose();
                }
            }

            return null;
        }

        /// <summary>
        /// 获取图片缩放比例
        /// </summary>
        /// <param name="oldWidth">原图片宽</param>
        /// <param name="oldHeigt">原图片高</param>
        /// <param name="newWidth">目标图片宽</param>
        /// <param name="newHeight">目标图片高</param>
        /// <returns></returns>
        public static float getWidthAndHeight(int oldWidth,int oldHeigt,int newWidth,int newHeight)
        {
            //按比例缩放           
            float scaleRate = 0.0f;
            if (oldWidth >= newWidth && oldHeigt >= newHeight)
            {
                int widthDis = oldWidth - newWidth;
                int heightDis = oldHeigt - newHeight;
                if (widthDis > heightDis)
                {
                    scaleRate = newWidth * 1f / oldWidth;
                }
                else
                {
                    scaleRate = newHeight * 1f / oldHeigt;
                }
            }
            else if (oldWidth >= newWidth && oldHeigt < newHeight)
            {
                scaleRate = newWidth * 1f / oldWidth;
            }
            else if (oldWidth < newWidth && oldHeigt >= newHeight)
            {
                scaleRate = newHeight * 1f / oldHeigt;
            }
            else
            {
                int widthDis = newWidth - oldWidth;
                int heightDis = newHeight - oldHeigt;
                if (widthDis > heightDis)
                {
                    scaleRate = newHeight * 1f / oldHeigt;
                }
                else
                {
                    scaleRate = newWidth * 1f / oldWidth;
                }
            }
            return scaleRate;
        }

        /// <summary>
        /// 剪裁图片
        /// </summary>
        /// <param name="src">原图片</param>
        /// <param name="left">左坐标</param>
        /// <param name="top">顶部坐标</param>
        /// <param name="right">右坐标</param>
        /// <param name="bottom">底部坐标</param>
        /// <returns>剪裁后的图片</returns>
        public static Image CutImage(Image src,int left,int top,int right,int bottom)
        {
            try
            {
                Bitmap srcBitmap = new Bitmap(src);
                Bitmap dstBitmap = srcBitmap.Clone(new Rectangle(left,top,right - left,bottom - top),PixelFormat.DontCare);
                return dstBitmap;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return null;
        }


    }
}
