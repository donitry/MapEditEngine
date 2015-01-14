using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;

namespace TextDx5
{
    class Model : IDisposable
    {
        #region 成员变量
        private bool _isBlock; //是否切片导入
        /// <summary>
        /// 其余参数放置容器.前5个位置固定放置(0-int 切片数,整图导入时，该参数固定为1;1-int 此模型类别XXYYYZ;2-string 文件名称(若切片,则为名称+编号00-99);3-image 切片模型;循环3-4存储直至切片存储完毕)
        /// </summary>
        private ArrayList _arryModle;
        #endregion

        #region 属性
        public bool isBlock { get { return _isBlock; } }
        public ArrayList arryModle { get { return _arryModle; } }
        #endregion

        #region 构造函数
        public Model()
        {
            _isBlock = false;
            _arryModle = new ArrayList();
        }
        #endregion

        #region 方法
        /// <summary>
        /// 装载模型到arraylist供编辑器使用
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="TypeModle">模型类型-XXYYYZ</param>
        /// <param name="NameModle">模型名称</param>
        /// <param name="isBlocks">是否切片,默认false</param>
        /// <param name="blockSize">切片大小,isBlocks为false时,该参数无效</param>
        public bool InitialModle(string filePath, int TypeModle, string NameModle, bool isBlocks,Size blockSize) 
        {
            Bitmap sourceImage = new Bitmap(filePath);
            if (isBlocks)
            {
                int ModleWidth = sourceImage.Width;
                int ModleHeigh = sourceImage.Height;
                if (errorSize(ModleWidth, blockSize.Width) || errorSize(ModleHeigh, blockSize.Height))
                    return false;
                ModleWidth -= ModleWidth % blockSize.Width;
                ModleHeigh -= ModleHeigh % blockSize.Height;
                int VBlocks = ModleWidth / blockSize.Width;
                int HBlocks = ModleHeigh / blockSize.Height;
                //_arryModle.Add(VBlocks * HBlocks);
                _arryModle.Add(TypeModle);
                for (int i = 0; i < HBlocks; i++)
                {
                    for (int j = 0; j < VBlocks; j++)
                    {
                        Bitmap destImage = new Bitmap(blockSize.Width, blockSize.Height);
                        using (Graphics g = Graphics.FromImage(destImage))
                        {
                            g.DrawImage(sourceImage, new Rectangle(0, 0, blockSize.Width,blockSize.Height), new Rectangle(j*blockSize.Width, i*blockSize.Height, blockSize.Width,blockSize.Height), GraphicsUnit.Pixel);
                            _arryModle.Add(destImage);
                        }
                    }
                }
            }
            else
            {
                //_arryModle.Add(1);
                _arryModle.Add(TypeModle);
                _arryModle.Add(sourceImage);
            }
            return true;
        }


        /// <summary>
        /// 记录模型档案
        /// </summary>
        /// <param name="arry_Modle">模型组</param>
        /// <returns>byte[] 类型供记录用类型</returns>
        public byte[] SaveModle(ArrayList arry_Modle,int blockSize)
        {
            Bitmap temp_M = new Bitmap(blockSize, arry_Modle.Count * blockSize);
            using (Graphics g = Graphics.FromImage(temp_M))
            {
                for (int i = 0; i < arry_Modle.Count; i++)
                {
                    g.DrawImage((Image)arry_Modle[i], new Rectangle(0, i * blockSize, blockSize, blockSize), new Rectangle(0, 0, blockSize, blockSize), GraphicsUnit.Pixel);
                }
            }
            byte[] modleBytes = BmpToBytes_MemStream(temp_M);
            return modleBytes;
        }

        /// <summary>
        /// 检查输入的块参是否过大
        /// </summary>
        /// <param name="image">图的宽或高</param>
        /// <param name="size">块大小</param>
        /// <returns>ture-大了 false-不大</returns>
        private bool errorSize(int image,int size)
        {
            if (image % size == image)
                return true;
            return false;
        }

        // Bitmap bytes have to be created via a direct memory copy of the bitmap
        private byte[] BmpToBytes_MemStream(Bitmap bmp)
        {
            MemoryStream ms = new MemoryStream();
            // Save to memory using the Jpeg format
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

            // read to end
            byte[] bmpBytes = ms.GetBuffer();
            bmp.Dispose();
            ms.Close();

            return bmpBytes;
        }
        #endregion

        #region Clean Up
        public void Dispose()
        {
            if (_arryModle.Count > 0)
                _arryModle.Clear();
            _arryModle = null;
        }
        #endregion
    }
}
