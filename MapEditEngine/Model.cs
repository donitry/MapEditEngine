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
        #region ��Ա����
        private bool _isBlock; //�Ƿ���Ƭ����
        /// <summary>
        /// ���������������.ǰ5��λ�ù̶�����(0-int ��Ƭ��,��ͼ����ʱ���ò����̶�Ϊ1;1-int ��ģ�����XXYYYZ;2-string �ļ�����(����Ƭ,��Ϊ����+���00-99);3-image ��Ƭģ��;ѭ��3-4�洢ֱ����Ƭ�洢���)
        /// </summary>
        private ArrayList _arryModle;
        #endregion

        #region ����
        public bool isBlock { get { return _isBlock; } }
        public ArrayList arryModle { get { return _arryModle; } }
        #endregion

        #region ���캯��
        public Model()
        {
            _isBlock = false;
            _arryModle = new ArrayList();
        }
        #endregion

        #region ����
        /// <summary>
        /// װ��ģ�͵�arraylist���༭��ʹ��
        /// </summary>
        /// <param name="filePath">�ļ�·��</param>
        /// <param name="TypeModle">ģ������-XXYYYZ</param>
        /// <param name="NameModle">ģ������</param>
        /// <param name="isBlocks">�Ƿ���Ƭ,Ĭ��false</param>
        /// <param name="blockSize">��Ƭ��С,isBlocksΪfalseʱ,�ò�����Ч</param>
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
        /// ��¼ģ�͵���
        /// </summary>
        /// <param name="arry_Modle">ģ����</param>
        /// <returns>byte[] ���͹���¼������</returns>
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
        /// �������Ŀ���Ƿ����
        /// </summary>
        /// <param name="image">ͼ�Ŀ���</param>
        /// <param name="size">���С</param>
        /// <returns>ture-���� false-����</returns>
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
