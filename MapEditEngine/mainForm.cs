using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using TextDx5.Data;

namespace TextDx5
{
    public partial class mainForm : Form
    {
        #region 变量定义
        private static bool isNewMap = false;
        private Rectangle drawRect;    //地形红色矩形框
        private Rectangle recentBlock; //当前地形
        private Rectangle mouseRect_map;  //地图编辑区鼠标位置矩形
        private Bitmap sourceImage;       //地形资源图片
        private static Size mapSize = new Size(0,0);
        private static int blockSize = 48;  //地形块大小
        private static string fileName = ""; //地形资源文件名
        private static string saveName = ""; //保存文件名
        private ArrayList arry_rect;   //地形资源切片位于地形资源展示区的矩形
        private ArrayList arry_block;  //地形资源切片矩形
        private ArrayList[] arry_mapBlock;    //地图切片
        private ArrayList[] arry_posBlock;    //地图切片对应显示位置
        private ArrayList[] arry_mapData;     //地图存储序列(分两层)
        private ArrayList arry_modle_resource; //地图模型资源序列 类型arrayList
        private Hashtable hashModle;         //模型资源包哈希索引
        private byte AIX = 0;                //地图块偏移角度 占2位 00 为0度360度 01-90 10-180 11为270度
        private byte STOP = 0;               //地图阻止属性 占2位 00 为通行区 11为禁行区 其余为减速
        private byte MIRROR = 0;             //地图块静象标志 占1位 0为非静象 1为静象
        private byte AIX_M = 0;                //模型块偏移角度 占2位 00 为0度360度 01-90 10-180 11为270度
        private byte STOP_M = 0;               //模型阻止属性 占2位 00 为通行区 11为禁行区 其余为减速
        private byte MIRROR_M = 0;             //模型块静象标志 占1位 0为非静象 1为静象
        private int offsetX = 0, offsetY = 0;//地图显示偏移量
        private int MouseX=0, MouseY=0;     //当前鼠标位置
        private int recentBlockNO = 0;     //当前地图块编号
        private int temp_a = 0;            //地图框下拉条刻度数
        private int trueWidth = 0;         //导出时，真实宽度
        private int trueHeight = 0;        //导出时，真实高度
        #endregion

        #region 构造函数
        public mainForm()
        {
            InitializeComponent();
        }
        #endregion

        #region 自定义函数

        public void LoadMapArgs(string[] temp,bool isNew)
        {
            fileName = temp[2];
            blockSize = Convert.ToInt32(temp[3]);
            mapSize = new Size(Convert.ToInt32(temp[0]), Convert.ToInt32(temp[1]));
            isNewMap = isNew;
        }

        public void LoadMapBlock(int size)
        {
            blockSize = size;            //获取单位块大小
        }

        /// <summary>
        /// 读入资源图
        /// </summary>
        /// <param name="file">资源文件地址,不能为空</param>
        /// <param name="image">若资源文件为image可直接使用,若无则为null</param>
        private void LoadBitmap(string file,Image image)//读入的bitmap文件，进行分析和切割
        {
            this.progressBar1.Visible = true;
            if (image == null)
                sourceImage = new Bitmap(file);
            else
                sourceImage = new Bitmap(image);
            int mapWidth = sourceImage.Width;
            int mapHeigh = sourceImage.Height;
            mapWidth -= mapWidth % blockSize;
            mapHeigh -= mapHeigh % blockSize;
            int VBlocks = mapWidth / blockSize;
            int HBlocks = mapHeigh / blockSize;

            //进度条 开始读入地图块
            this.progressBar1.Visible = true;
            this.progressBar1.Maximum = HBlocks * VBlocks;
            this.progressBar1.Value = 0;
            for (int i = 0; i < HBlocks; i++)
            {
                for (int j = 0; j < VBlocks; j++)
                {
                    int v = j*blockSize;
                    int h = i*blockSize;
                    if (!isEmpty(sourceImage,v, h))
                    {
                        Rectangle rect = new Rectangle(v, h, blockSize, blockSize);
                        arry_block.Add(rect);
                    }
                    this.progressBar1.Value += 1;
                }
            }
            this.progressBar1.Value = 0;
            this.progressBar1.Visible = false;

            //画布初始化
            if (arry_block.Count > 0)
            {
                initialCanvas();
                int due = Int32.MaxValue;
                int val = Int32.MaxValue;
                val = Math.DivRem(arry_block.Count,pictureBox3.Width/blockSize,out due);
                if (due != 0)
                {
                    vScrollBar1.Maximum = val + 1 - pictureBox3.Height/blockSize;
                }
                else
                    vScrollBar1.Maximum = val + 0 - pictureBox3.Height/blockSize;             
            }
        }

        private void initialCanvas()
        {
            for (int i = 0; i < pictureBox3.Height / blockSize; i++)
            {
                for (int j = 0; j < pictureBox3.Width / blockSize; j++)
                {                  
                    Rectangle rect = new Rectangle(j*blockSize, i*blockSize, blockSize, blockSize);
                    arry_rect.Add(rect);
                }
            }
        }

        private bool isEmpty(Bitmap b, int x,int y)   //判断空白图块
        {
            Random rand = new Random();
            Bitmap bitmap = b;
            int a = 0;
            for (int i = 0; i < 10; i++)
            {
                a = rand.Next(blockSize);
                Color pixColor = bitmap.GetPixel(x + a , y + a);
                if (pixColor.ToArgb() != Color.White.ToArgb())
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 初始化地图编辑区域
        /// </summary>
        /// <param name="path">图源资源地址</param>
        /// <param name="image">若无image资源，则填写null</param>
        /// <param name="size">编辑地图的尺寸</param>
        private void InitializeEditor(string path,Image image,Size size)
        {
            if (image == null)
                LoadBitmap(fileName, null);
            else
                LoadBitmap("", image);
            mapSize = size;

            pictureBox3.Visible = true;
            drawBlocks();
            drawMaps();

            drawRect = new Rectangle(0, 0, blockSize, blockSize);
            pictureBox3.Refresh();
        }

        private void ShowLineJoin(int width,int height,PaintEventArgs e) //网格
        {

            Pen skyBluePen = new Pen(Brushes.Green, 0.1f);
            skyBluePen.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
            for (int i = 0; i < width / blockSize + 1; i++)
            {
                for (int j = 0; j < height / blockSize + 1; j++)
                {
                    e.Graphics.DrawLine(skyBluePen, new Point(i * blockSize, 0), new Point(i * blockSize, j * blockSize));   //x
                    e.Graphics.DrawLine(skyBluePen, new Point(0, j * blockSize), new Point(i * blockSize, j * blockSize));   //y
                }
            }
            skyBluePen.Dispose();
        }

        private void drawBlocks()          //地形资源区绘制
        {
            Bitmap dest = new Bitmap(pictureBox3.Width, pictureBox3.Height);
            using (Graphics g = Graphics.FromImage(dest))
            {
                if (arry_block.Count > 0)
                {
                    for (int i = 0; i < arry_rect.Count; i++)
                    {
                        int a = i + (temp_a * (pictureBox3.Width / blockSize));
                        if (a < arry_block.Count)
                            g.DrawImage(sourceImage, (Rectangle)arry_rect[i], (Rectangle)arry_block[a], GraphicsUnit.Pixel);
                        else
                            break;
                    }
                }
            }
            pictureBox3.Image = dest;
            pictureBox3.Update();
        }

        private void drawMaps()            //地形编辑区绘制
        {
            Bitmap dest = new Bitmap(pictureBox2.Width, pictureBox2.Height);
            using (Graphics g = Graphics.FromImage(dest))
            {
                for (int j = 0; j < 2; j++)
                {
                    if (arry_mapBlock[j].Count > 0)
                    {
                        for (int i = 0; i < arry_posBlock[j].Count; i++)
                        {
                            if (i < arry_mapBlock[j].Count)
                            {
                                Rectangle posRect = (Rectangle)arry_posBlock[j][i];
                                posRect.X = (posRect.X - offsetX) * blockSize;
                                posRect.Y = (posRect.Y - offsetY) * blockSize;
                                g.DrawImage((Image)arry_mapBlock[j][i], posRect, new Rectangle(0, 0, posRect.Width, posRect.Height), GraphicsUnit.Pixel);
                            }
                        }
                    }
                }
            }
            pictureBox2.Image = dest;
            pictureBox2.Update();
        }

        private void drawProper()          //地形属性区绘制
        {
            Bitmap bitmap = new Bitmap(blockSize, blockSize);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(sourceImage, new Rectangle(0, 0, blockSize, blockSize), recentBlock, GraphicsUnit.Pixel);
            }
            pictureBox1.Image = bitmap;
            pictureBox1.Update();
        }

        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

        // Bitmap bytes have to be created via a direct memory copy of the bitmap
        private byte[] BmpToBytes_MemStream(Bitmap bmp)
        {
            ImageCodecInfo myImageCodecInfo;
            System.Drawing.Imaging.Encoder myEncoder;
            EncoderParameter myEncoderParameter;
            EncoderParameters myEncoderParameters;

            MemoryStream ms = new MemoryStream();

            myImageCodecInfo = GetEncoderInfo("image/bmp");
            myEncoder = System.Drawing.Imaging.Encoder.ColorDepth;
            myEncoderParameters = new EncoderParameters(1);
            myEncoderParameter = new EncoderParameter(myEncoder, 32L);
            myEncoderParameters.Param[0] = myEncoderParameter;

            // Save to memory using the Jpeg format
            //bmp.Save(ms, myImageCodecInfo, myEncoderParameters);
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

            // read to end
            byte[] bmpBytes = ms.GetBuffer();
            bmp.Dispose();
            ms.Close();

            return bmpBytes;
        }

        //Bitmap bytes have to be created using Image.Save()
        private Image BytesToImg(byte[] bmpBytes)
        {
            MemoryStream ms = new MemoryStream(bmpBytes);
            Image img = Image.FromStream(ms);
            // Do NOT close the stream!

            return img;
        }

        /// <summary>
        /// 绘制红色叉框
        /// </summary>
        /// <param name="e">设备</param>
        /// <param name="rect">矩形范围</param>
        private void drawRectLineX(PaintEventArgs e, Rectangle rect)
        {
            using (Pen pen = new Pen(Brushes.Red, 0.3f))
            {
                e.Graphics.DrawLine(pen, new Point(rect.X, rect.Y), new Point(rect.X + rect.Width, rect.Y + rect.Height));
                e.Graphics.DrawLine(pen, new Point(rect.X, rect.Y + rect.Height), new Point(rect.X + rect.Width, rect.Y));
            }
        }

        /// <summary>
        /// 红色矩形框绘制
        /// </summary>
        /// <param name="e">设备</param>
        /// <param name="rect">绘制矩形</param>
        private void drawRectLine(PaintEventArgs e,Rectangle rect) //鼠标框绘制
        {
            using (Pen pen = new Pen(Brushes.Red, 0.3f))
            {
                e.Graphics.DrawRectangle(pen, rect);
            }
        }

        /// <summary>
        /// 图形resize
        /// </summary>
        /// <param name="b">需要resize的图形</param>
        /// <param name="nWidth">宽度</param>
        /// <param name="nHeight">高度</param>
        /// <returns>resize后的图形</returns>
        private Bitmap ResizeBitmap(Bitmap b, int nWidth, int nHeight)
        {
            Bitmap result = new Bitmap(nWidth, nHeight);
            using (Graphics g = Graphics.FromImage((Image)result))
                g.DrawImage(b, 0, 0, nWidth, nHeight);
            return result;
        }

        /// <summary>
        /// 图形翻转(单位90度)
        /// </summary>
        /// <param name="reverse">是否静象</param>
        /// <param name="b">需要处理的图形</param>
        /// <returns>处理完的图形</returns>
        private Bitmap RotateFlip(bool reverse,Bitmap b)
        {
            Bitmap bitmap = b;
            if (reverse)
            {
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
            }
            else
                bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
            return bitmap;
        }

        /// <summary>
        /// 档案文件大小记录
        /// </summary>
        /// <param name="byteLen"></param>
        /// <param name="fs"></param>
        /// <returns></returns>
        private bool MathWriteBytes(long byteLen, FileStream fs)
        {
            try
            {
                string a = Convert.ToString(byteLen);
                byte[] temp = Encoding.Default.GetBytes(a);
                int groupMultiple = 1;
                int gue = Int32.MaxValue;
                int gal = Int32.MaxValue;
               
                gal = Math.DivRem(temp.Length, 255, out gue);
                while (gal > 255)
                {
                    ++groupMultiple;
                    gal = Math.DivRem(temp.Length, 255 * groupMultiple, out gue);
                }

                fs.WriteByte((byte)groupMultiple);      //0-档案基数倍数
                fs.WriteByte((byte)gal);                //1-档案基数
                fs.WriteByte((byte)gue);                //2-余数
                fs.Write(temp, 0, temp.Length);
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message.ToString(), "MathWriteBytes");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 记录地图数据
        /// </summary>
        /// <param name="fileName">文件路径</param>
        private void saveMapData(string fileName)
        {
            string path = fileName;
            if (File.Exists(path))
                File.Delete(path);
            ArrayList[] arry_temp = new ArrayList[2];
            arry_temp[0] = new ArrayList(arry_mapData[0]); //层1
            arry_temp[1] = new ArrayList(arry_mapData[1]); //层2
            

            using (FileStream fs = File.Create(path))
            {
                #region lay 1
                if (arry_temp[0].Count > 0)
                {
                    //宽
                    int wue = Int32.MaxValue;
                    int wal = Int32.MaxValue;
                    wal = Math.DivRem(mapSize.Width, 255, out wue);
                    fs.WriteByte((byte)wal); //0
                    fs.WriteByte((byte)wue); //1
                    //高
                    int hue = Int32.MaxValue;
                    int hal = Int32.MaxValue;
                    hal = Math.DivRem(mapSize.Height, 255, out hue);
                    fs.WriteByte((byte)hal); //2
                    fs.WriteByte((byte)hue); //3

                    fs.WriteByte((byte)blockSize);  //4

                    int groupCounts = arry_temp[0].Count / 4;
                    MathWriteBytes(groupCounts, fs);

                    while (arry_temp[0].Count > 0)
                    {
                        byte[] imageBytes = BmpToBytes_MemStream(new Bitmap((Image)arry_temp[0][3]));
                        fs.WriteByte(Convert.ToByte((int)arry_temp[0][0])); //x
                        fs.WriteByte(Convert.ToByte((int)arry_temp[0][1])); //y
                        fs.WriteByte(Convert.ToByte((int)arry_temp[0][2])); //arg
                        MathWriteBytes(imageBytes.Length, fs);
                        fs.Write(imageBytes, 0, imageBytes.Length); //image

                        for (int i = 0; i < 4; i++)
                            arry_temp[0].RemoveAt(0);
                    }

                    //写入第一层资源
                    Bitmap temp_ = new Bitmap(blockSize, arry_block.Count * blockSize);
                    using (Graphics g = Graphics.FromImage(temp_))
                    {
                        for (int i = 0; i < arry_block.Count; i++)
                        {
                            g.DrawImage(sourceImage, new Rectangle(0, i * blockSize, blockSize, blockSize), (Rectangle)arry_block[i], GraphicsUnit.Pixel);
                        }
                    }
                    //byte[] bytes = (byte[])TypeDescriptor.GetConverter(temp_).ConvertTo(temp_, typeof(byte[]));
                    byte[] terrainBytes = BmpToBytes_MemStream(temp_);

                    MathWriteBytes(terrainBytes.Length, fs);
                    fs.Write(terrainBytes, 0, terrainBytes.Length);
                }
                #endregion

                #region lay 2
                if (arry_temp[1].Count > 0)
                {
                    int groupCounts = arry_temp[1].Count / 4;//x,y,arg,image
                    MathWriteBytes(groupCounts, fs);

                    while (arry_temp[1].Count > 0)
                    {
                        byte[] imageBytes = BmpToBytes_MemStream(new Bitmap((Image)arry_temp[1][3]));
                        fs.WriteByte(Convert.ToByte((int)arry_temp[1][0])); //x
                        fs.WriteByte(Convert.ToByte((int)arry_temp[1][1])); //y
                        fs.WriteByte(Convert.ToByte((int)arry_temp[1][2])); //arg
                        MathWriteBytes(imageBytes.Length, fs);
                        fs.Write(imageBytes, 0, imageBytes.Length); //image

                        for (int d = 0; d < 4; d++)
                            arry_temp[1].RemoveAt(0);
                    }

                    #region 模型资源存储
                    int hashGroups = hashModle.Count;
                    //MathWriteBytes(hashGroups, fs);
                    fs.WriteByte((byte)hashGroups);

                    foreach (ArrayList _tempModleArry in arry_modle_resource)
                    {
                        int _groupImage = _tempModleArry.Count;
                        MathWriteBytes(_groupImage, fs);

                        for (int i = 0; i < _tempModleArry.Count; i++)
                        {
                            int dataLen = 0;
                            
                            if (_tempModleArry[i].GetType().Equals(typeof(Int32)))
                            {
                                dataLen = (int)_tempModleArry[i];
                                fs.WriteByte((byte)255);          //0-档案基数倍数
                                fs.WriteByte((byte)255);                   //1-档案基数
                                fs.WriteByte((byte)255);                   //2-余数
                                fs.WriteByte((byte)dataLen);                //ascII - A~Z
                            }
                            else
                            {
                                Bitmap a = new Bitmap(_tempModleArry[i] as Bitmap);
                                byte[] tt = BmpToBytes_MemStream(a);
                                dataLen = tt.Length;
                                MathWriteBytes(dataLen, fs);
                                fs.Write(tt, 0, dataLen);
                            }
                        }
                    }

                    foreach (int i in hashModle.Keys)
                    {
                        MathWriteBytes(i, fs);

                        int _temp_a = i / 1000;
                        int _temp_b = (i - _temp_a * 1000) / 10;
                        int _temp_c = i - _temp_a * 1000 - _temp_b * 10;
                        int _modleNameCounts = this.treeView1.Nodes[_temp_a].Nodes[_temp_b].Nodes[_temp_c].Nodes.Count;

                        MathWriteBytes(_modleNameCounts, fs);

                        for (int j = 0; j < _modleNameCounts; j++)
                        {
                            byte[] nameModleBytes = Encoding.Default.GetBytes(this.treeView1.Nodes[_temp_a].Nodes[_temp_b].Nodes[_temp_c].Nodes[j].Text);
                            MathWriteBytes(nameModleBytes.Length, fs);
                            fs.Write(nameModleBytes, 0, nameModleBytes.Length); //写入模型名称
                        }
                    }
                }
                #endregion
                #endregion
            }
            arry_temp[0].Clear();
            arry_temp[1].Clear();
        }

        /// <summary>
        /// 读取地图数据存档
        /// </summary>
        /// <param name="fileName">文件路径</param>
        private void loadMapData(string fileName)
        {
            using (FileStream fs = File.OpenRead(fileName))
            {
                if (fs.Length > 0)
                {
                    this.Release();

                    byte[] sumBytes = new byte[fs.Length];
                    fs.Read(sumBytes, 0, sumBytes.Length);
                    #region lay_1
                    //获取编辑区内的地图

                    int wal = sumBytes[0]; //width基数
                    int wue = sumBytes[1]; //余数

                    int hal = sumBytes[2]; //height基数
                    int hue = sumBytes[3]; //余数

                    mapSize.Width = wal * 255 + wue;
                    mapSize.Height = hal * 255 + hue;

                    blockSize = sumBytes[4]; //获得块大小

                    int groupMultiple = sumBytes[5]; //倍率
                    int gal = sumBytes[6];
                    int gue = sumBytes[7];
                    int groupCounts = gal * groupMultiple * 255 + gue;

                    byte[] sumBytes_v = new byte[groupCounts];
                    Array.Copy(sumBytes, 8, sumBytes_v, 0, sumBytes_v.Length);
                    groupCounts = Convert.ToInt32(Encoding.Default.GetString(sumBytes_v));

                    sumBytes_v = new byte[sumBytes.Length - sumBytes_v.Length - 8];
                    Array.Copy(sumBytes, sumBytes.Length - sumBytes_v.Length, sumBytes_v, 0, sumBytes_v.Length);

                    
                    for (int i = 0; i < groupCounts; i++)
                    {
                        int x = sumBytes_v[0];
                        int y = sumBytes_v[1];
                        int arg = sumBytes_v[2];
                        int Multiple = sumBytes_v[3];
                        int val = sumBytes_v[4];
                        int due = sumBytes_v[5];
                        int temp = Multiple * val * 255 + due;
                        byte[] imageByte = new byte[temp];
                        Array.Copy(sumBytes_v, 6, imageByte, 0, imageByte.Length);
                        imageByte = new byte[Convert.ToInt32(Encoding.Default.GetString(imageByte))];

                        Array.Copy(sumBytes_v, 6 + temp, imageByte, 0, imageByte.Length);
                        sumBytes_v = new byte[sumBytes_v.Length - imageByte.Length - temp - 6];
                        Array.Copy(sumBytes, sumBytes.Length - sumBytes_v.Length, sumBytes_v, 0, sumBytes_v.Length);
                        Rectangle rect = new Rectangle(x, y, blockSize, blockSize);
                        using (MemoryStream ms = new MemoryStream(imageByte))
                        {
                            Image image = Image.FromStream(ms);
                            arry_posBlock[0].Add(rect);
                            arry_mapBlock[0].Add(image);
                            arry_mapData[0].Add(rect.X);        //记录地图块编号
                            arry_mapData[0].Add(rect.Y);
                            arry_mapData[0].Add(arg);
                            arry_mapData[0].Add(image);
                        }
                    }

                    int sourceMultiple = sumBytes_v[0];
                    int sal = sumBytes_v[1];
                    int sue = sumBytes_v[2];
                    int temp_ = sourceMultiple * sal * 255 + sue;
                    byte[] sourceImage = new byte[temp_];
                    Array.Copy(sumBytes_v, 3, sourceImage, 0, sourceImage.Length);
                    sourceImage = new byte[Convert.ToInt32(Encoding.Default.GetString(sourceImage))];
                    Array.Copy(sumBytes_v, 3 + temp_, sourceImage, 0, sourceImage.Length);
                    sumBytes_v = new byte[sumBytes_v.Length - sourceImage.Length - temp_ - 3];
                    Array.Copy(sumBytes, sumBytes.Length - sumBytes_v.Length, sumBytes_v, 0, sumBytes_v.Length);
                    #endregion

                    #region lay_2
                    //lay_2 编辑区内的模型
                    if (sumBytes_v.Length > 0)
                    {
                        groupMultiple = sumBytes_v[0];
                        gal = sumBytes_v[1];
                        gue = sumBytes_v[2];
                        groupCounts = gal * groupMultiple * 255 + gue;
                        byte[] c = new byte[groupCounts];
                        Array.Copy(sumBytes_v, 3, c, 0, c.Length);
                        groupCounts = Convert.ToInt32(Encoding.Default.GetString(c));
                        sumBytes_v = new byte[sumBytes_v.Length - c.Length - 3];
                        Array.Copy(sumBytes, sumBytes.Length - sumBytes_v.Length, sumBytes_v, 0, sumBytes_v.Length);
                        
                        for (int m = 0; m < groupCounts; m++)
                        {
                            int x = sumBytes_v[0];
                            int y = sumBytes_v[1];
                            int arg = sumBytes_v[2];
                            int Multiple = sumBytes_v[3];
                            int val = sumBytes_v[4];
                            int due = sumBytes_v[5];
                            int _temp = Multiple * val * 255 + due;
                            byte[] imageByte = new byte[_temp];
                            Array.Copy(sumBytes_v, 6, imageByte, 0, imageByte.Length);
                            imageByte = new byte[Convert.ToInt32(Encoding.Default.GetString(imageByte))];
                            Array.Copy(sumBytes_v, 6 + _temp, imageByte, 0, imageByte.Length);
                            sumBytes_v = new byte[sumBytes_v.Length - imageByte.Length - _temp - 6];
                            Array.Copy(sumBytes, sumBytes.Length - sumBytes_v.Length, sumBytes_v, 0, sumBytes_v.Length);
                            using (MemoryStream ms = new MemoryStream(imageByte))
                            {
                                Image image = Image.FromStream(ms);
                                Rectangle rect = new Rectangle(x, y, image.Width, image.Height);
                                arry_posBlock[1].Add(rect);
                                arry_mapBlock[1].Add(image);
                                arry_mapData[1].Add(rect.X);        //模型恢复
                                arry_mapData[1].Add(rect.Y);
                                arry_mapData[1].Add(arg);
                                arry_mapData[1].Add(image);
                            }
                        }
                        //哈希表复原
                        /* //当哈希表大于255时 这里可以启用
                        groupMultiple = sumBytes_v[0];
                        gal = sumBytes_v[1];
                        gue = sumBytes_v[2];
                        
                        groupCounts = gal * groupMultiple * 255 + gue;
                        byte[] d = new byte[groupCounts];
                        Array.Copy(sumBytes_v, 3, c, 0, c.Length);
                        groupCounts = Convert.ToInt32(Encoding.Default.GetString(d));
                        sumBytes_v = new byte[sumBytes_v.Length - c.Length - 3];
                        Array.Copy(sumBytes, sumBytes.Length - sumBytes_v.Length, sumBytes_v, 0, sumBytes_v.Length);
                        */
                        groupCounts = sumBytes_v[0];
                        sumBytes_v = new byte[sumBytes_v.Length - 1];
                        Array.Copy(sumBytes, sumBytes.Length - sumBytes_v.Length, sumBytes_v, 0, sumBytes_v.Length);

                        for (int m = 0; m < groupCounts; m++)
                        {
                            ArrayList _tempImages = new ArrayList();
                            int hMultiple = sumBytes_v[0];
                            int hval = sumBytes_v[1];
                            int hdue = sumBytes_v[2];
                            int hGroups = hval * hMultiple * 255 + hdue;
                            byte[] e = new byte[hGroups];
                            Array.Copy(sumBytes_v, 3, e, 0, e.Length);
                            hGroups = Convert.ToInt32(Encoding.Default.GetString(e));
                            sumBytes_v = new byte[sumBytes_v.Length - e.Length - 3];
                            Array.Copy(sumBytes, sumBytes.Length - sumBytes_v.Length, sumBytes_v, 0, sumBytes_v.Length);
                            for (int m_ = 0; m_ < hGroups; m_++)
                            {
                                int Multiple = sumBytes_v[0];
                                int val = sumBytes_v[1];
                                int due = sumBytes_v[2];
                                if (Multiple == due && due == val && val == 255)
                                {
                                    _tempImages.Add((int)sumBytes_v[3]);
                                    sumBytes_v = new byte[sumBytes_v.Length - 4];
                                    Array.Copy(sumBytes, sumBytes.Length - sumBytes_v.Length, sumBytes_v, 0, sumBytes_v.Length);
                                }
                                else
                                {
                                    int _temp_ = Multiple * val * 255 + due;
                                    byte[] imageByte = new byte[_temp_];
                                    Array.Copy(sumBytes_v, 3, imageByte, 0, imageByte.Length);
                                    imageByte = new byte[Convert.ToInt32(Encoding.Default.GetString(imageByte))];
                                    Array.Copy(sumBytes_v, 3 + _temp_, imageByte, 0, imageByte.Length);
                                    sumBytes_v = new byte[sumBytes_v.Length - imageByte.Length - _temp_ - 3];
                                    Array.Copy(sumBytes, sumBytes.Length - sumBytes_v.Length, sumBytes_v, 0, sumBytes_v.Length);
                                    using (MemoryStream ms = new MemoryStream(imageByte))
                                    {
                                        Image image = Image.FromStream(ms);
                                        _tempImages.Add(image);
                                    }
                                }
                            }
                            arry_modle_resource.Add(_tempImages);
                        }

                        for (int m__ = 0; m__ < groupCounts; m__++)
                        {
                            int hMultiple = sumBytes_v[0];
                            int hval = sumBytes_v[1];
                            int hdue = sumBytes_v[2];
                            int indexTree = hval * hMultiple * 255 + hdue;
                            byte[] f = new byte[indexTree];
                            Array.Copy(sumBytes_v, 3, f, 0, f.Length);
                            indexTree = Convert.ToInt32(Encoding.Default.GetString(f));
                            sumBytes_v = new byte[sumBytes_v.Length - f.Length - 3];
                            Array.Copy(sumBytes, sumBytes.Length - sumBytes_v.Length, sumBytes_v, 0, sumBytes_v.Length);
                            int _temp_a = indexTree / 1000;
                            int _temp_b = (indexTree - _temp_a * 1000) / 10;
                            int _temp_c = indexTree - _temp_a * 1000 - _temp_b * 10;

                            hMultiple = sumBytes_v[0];
                            hval = sumBytes_v[1];
                            hdue = sumBytes_v[2];
                            int hGroups = hval * hMultiple * 255 + hdue;
                            byte[] g = new byte[hGroups];
                            Array.Copy(sumBytes_v, 3, g, 0, g.Length);
                            hGroups = Convert.ToInt32(Encoding.Default.GetString(g));
                            sumBytes_v = new byte[sumBytes_v.Length - g.Length - 3];
                            Array.Copy(sumBytes, sumBytes.Length - sumBytes_v.Length, sumBytes_v, 0, sumBytes_v.Length);
                            for (int _m = 0; _m < hGroups; _m++)
                            {
                                int Multiple = sumBytes_v[0];
                                int val = sumBytes_v[1];
                                int due = sumBytes_v[2];
                                int _temp__ = Multiple * val * 255 + due;
                                byte[] nameModleBytes = new byte[_temp__];
                                Array.Copy(sumBytes_v, 3, nameModleBytes, 0, nameModleBytes.Length);
                                nameModleBytes = new byte[Convert.ToInt32(Encoding.Default.GetString(nameModleBytes))];
                                Array.Copy(sumBytes_v, 3 + _temp__, nameModleBytes, 0, nameModleBytes.Length);
                                sumBytes_v = new byte[sumBytes_v.Length - nameModleBytes.Length - _temp__ - 3];
                                Array.Copy(sumBytes, sumBytes.Length - sumBytes_v.Length, sumBytes_v, 0, sumBytes_v.Length);
                                this.treeView1.Nodes[_temp_a].Nodes[_temp_b].Nodes[_temp_c].Nodes.Add(Encoding.Default.GetString(nameModleBytes));
                            }
                            hashModle.Add(indexTree, arry_modle_resource.Count - 1);
                        }
                    }
                    #endregion
                    using (MemoryStream ms = new MemoryStream(sourceImage))
                    {
                        Bitmap image = new Bitmap(ms);
                        InitializeEditor("", image, mapSize);
                    }
                }
            }
        }

        /// <summary>
        /// 导出地图数据
        /// </summary>
        /// <param name="fileName">文件路径</param>
        private void exportMapData(string fileName)
        {
            if (pictureBox2.Image != null && blockSize > 0 && arry_mapData[0].Count > 0)
            {
                string path = fileName;
                if (File.Exists(path))
                    File.Delete(path);
                ArrayList[] arry_temp = new ArrayList[2];
                arry_temp[0] = new ArrayList(arry_mapData[0]); //层1
                arry_temp[1] = new ArrayList(arry_mapData[1]); //层2
                using (FileStream fs = File.Create(path))
                {
                    #region lay 1
                    if (arry_temp[0].Count > 0)
                    {
                        int v = 0;
                        int h = 0;
                        foreach (Rectangle i in arry_posBlock[0])
                        {
                            int first = 0;
                            int last = 0;
                            if (i.X == 0)
                            {
                                first = arry_posBlock[0].IndexOf(i);
                                last = arry_posBlock[0].IndexOf(i, first + 1);
                                if (last == -1)
                                    h++;
                            }

                            if (i.Y == 0)
                            {
                                first = arry_posBlock[0].IndexOf(i);
                                last = arry_posBlock[0].IndexOf(i, first + 1);
                                if (last == -1)
                                    v++;
                            }
                        }
                        trueWidth = v * blockSize;
                        trueHeight = h * blockSize;
                        int tue = 0;
                        int tal = 0;
                        tal = Math.DivRem(trueWidth, blockSize, out tue);
                        if (tue != 0)
                        {
                            trueWidth += (blockSize - tue);
                        }

                        tal = Math.DivRem(trueHeight, blockSize, out tue);
                        if (tue != 0)
                        {
                            trueHeight += (blockSize - tue);
                        }

                        //宽
                        int wue = Int32.MaxValue;
                        int wal = Int32.MaxValue;
                        wal = Math.DivRem(trueWidth, 255, out wue);
                        fs.WriteByte((byte)wal); //0
                        fs.WriteByte((byte)wue); //1
                        //高
                        int hue = Int32.MaxValue;
                        int hal = Int32.MaxValue;
                        hal = Math.DivRem(trueHeight, 255, out hue);
                        fs.WriteByte((byte)hal); //2
                        fs.WriteByte((byte)hue); //3

                        fs.WriteByte((byte)blockSize);  //4

                        int groupCounts = (arry_temp[0].Count + arry_temp[1].Count) / 4;

                        MathWriteBytes(groupCounts, fs);

                        Bitmap temp_ = new Bitmap(trueWidth, trueHeight);
                        using (Graphics g = Graphics.FromImage(temp_))
                        {
                            for (int i = 0; i < arry_temp[0].Count / 4; i++)
                            {
                                fs.WriteByte(Convert.ToByte((int)arry_temp[0][i * 4 + 0])); //x
                                fs.WriteByte(Convert.ToByte((int)arry_temp[0][i * 4 + 1])); //y
                                fs.WriteByte(Convert.ToByte((int)arry_temp[0][i * 4 + 2])); //arg
                                g.DrawImage((Bitmap)arry_temp[0][i * 4 + 3], new Rectangle((int)arry_temp[0][i * 4] * blockSize, (int)arry_temp[0][i * 4 + 1] * blockSize, blockSize, blockSize), new Rectangle(0,0,blockSize,blockSize), GraphicsUnit.Pixel);
                            }

                            for (int i = 0; i < arry_temp[1].Count / 4; i++)
                            {
                                fs.WriteByte(Convert.ToByte((int)arry_temp[1][i * 4 + 0])); //x
                                fs.WriteByte(Convert.ToByte((int)arry_temp[1][i * 4 + 1])); //y
                                fs.WriteByte(Convert.ToByte((int)arry_temp[1][i * 4 + 2])); //arg
                                Bitmap a = (Bitmap)arry_temp[1][i * 4 + 3];
                                g.DrawImage(a, new Rectangle((int)arry_temp[1][i * 4] * blockSize, (int)arry_temp[1][i * 4 + 1] * blockSize, a.Width, a.Height), new Rectangle(0, 0, a.Width, a.Height), GraphicsUnit.Pixel);
                            }
                        }
                        //byte[] bytes = (byte[])TypeDescriptor.GetConverter(temp_).ConvertTo(temp_, typeof(byte[]));
                        byte[] terrainBytes = BmpToBytes_MemStream(temp_);
                        fs.Write(terrainBytes, 0, terrainBytes.Length);
                    }
                    #endregion
                }
                arry_temp[0].Clear();
                arry_temp[1].Clear();
            }
        }
        #endregion

        #region 地图文件偏移计算
        private int mapSizeF(int width,int height) // 地形图识别
        {
            int a = blockSize;
            int b = Math.Max(width, height);
            int c = b % 255;
            int d = c % a;
            int e = 255 - d;
            return e;
        }
        #endregion

        #region 鼠标操作
        void pictureBox2_MouseUp(object sender, MouseEventArgs e)
        {
            if (mouseRect_map != null)
            {
                mouseRect_map = new Rectangle(0, 0, 0, 0);
            }
            pictureBox2.Refresh();
        }

        void pictureBox2_MouseDown(object sender, MouseEventArgs e)
        {
            MouseX = e.X;
            MouseY = e.Y;
        }

        void pictureBox2_MouseMove(object sender, MouseEventArgs e)
        {
            this.label11.Text = e.Location.ToString();
            if (e.Button == MouseButtons.Left)
            {
                Rectangle rect = new Rectangle(MouseX, MouseY, e.X - MouseX, e.Y - MouseY);
                mouseRect_map = rect;
                pictureBox2.Refresh();
            }
        }

        void pictureBox2_MouseClick(object sender, MouseEventArgs e)
        {
            if (this.tabControl1.SelectedTab == this.tabPage1)   //层一 地形
            {
                if (blockSize > 0 && pictureBox1.Image != null && e.Button == MouseButtons.Left)
                {
                    int arg = ((MIRROR << 5) + (AIX << 2) + (STOP));
                    int VCo = e.X / blockSize;
                    int HCo = e.Y / blockSize;
                    Rectangle rect = new Rectangle(VCo + offsetX, HCo + offsetY, blockSize, blockSize);
                    arry_posBlock[0].Add(rect);
                    arry_mapData[0].Add(rect.X);        //记录地图块编号
                    arry_mapData[0].Add(rect.Y);
                    arry_mapData[0].Add(arg);
                    arry_mapBlock[0].Add(pictureBox1.Image);
                    arry_mapData[0].Add(pictureBox1.Image);

                    this.drawMaps();
                }

                if (arry_posBlock[0].Count > 0 && arry_mapBlock[0].Count > 0 && e.Button == MouseButtons.Right)
                {
                    int delC = -1;
                    int VCo = e.X / blockSize;
                    int HCo = e.Y / blockSize;
                    Rectangle rect = new Rectangle(VCo + offsetX, HCo + offsetY, blockSize, blockSize);
                    if (arry_posBlock[0].Contains(rect))
                    {
                        delC = arry_posBlock[0].LastIndexOf(rect);
                        arry_posBlock[0].RemoveAt(delC);
                    }
                    if (delC > -1)
                    {
                        arry_mapBlock[0].RemoveAt(delC);
                        for (int i = 0; i < 4; i++)
                            arry_mapData[0].RemoveAt(delC * 4);
                    }
                    this.drawMaps();
                }

            }
            else                                               //层二 模型
            {
                if (blockSize > 0 && pictureBox4.Image != null && e.Button == MouseButtons.Left)
                {
                    int arg = ((MIRROR_M << 5) + (AIX_M << 2) + (STOP_M));
                    int VCo = e.X / blockSize;
                    int HCo = e.Y / blockSize;
                    Rectangle rect = new Rectangle(VCo + offsetX, HCo + offsetY, pictureBox4.Image.Width, pictureBox4.Image.Height);
                    arry_posBlock[1].Add(rect);
                    arry_mapBlock[1].Add(pictureBox4.Image);
                    arry_mapData[1].Add(rect.X);        //记录地图块编号
                    arry_mapData[1].Add(rect.Y);
                    arry_mapData[1].Add(arg);
                    arry_mapData[1].Add(pictureBox4.Image);

                    this.drawMaps();
                }

                if (arry_posBlock[1].Count > 0 && arry_mapBlock[1].Count > 0 && e.Button == MouseButtons.Right)
                {
                    int VCo = e.X / blockSize;
                    int HCo = e.Y / blockSize;

                    for (int i = arry_mapData[1].Count / 4 - 1; i >=0; --i)
                    {
                        if ((int)arry_mapData[1][i * 4 + 0] == VCo && (int)arry_mapData[1][i * 4 + 1] == HCo)
                        {
                            arry_posBlock[1].RemoveAt(i);
                            arry_mapBlock[1].RemoveAt(i);
                            for (int j = 0; j < 4; j++)
                                arry_mapData[1].RemoveAt(i * 4);
                            break;
                        }
                    }
                    this.drawMaps();
                }
            }
            
        }

        void pictureBox3_MouseDown(object sender, MouseEventArgs e)
        {
            AIX = 0;
            MIRROR = 0;
            radioButton1.Checked = true;
            int VCo = e.X / blockSize;
            int HCo = e.Y / blockSize;
            recentBlockNO = (HCo + vScrollBar1.Value) * (pictureBox3.Width / blockSize) + VCo;
            if (recentBlockNO < arry_block.Count)
            {
                recentBlock = (Rectangle)arry_block[recentBlockNO];
                this.drawProper();
            }
        }

        void pictureBox3_MouseMove(object sender, MouseEventArgs e)
        {
            int x = ((int)(e.X / blockSize)) * blockSize;
            int y = ((int)(e.Y / blockSize)) * blockSize;
            Point point = new Point(x,y);
            Size size = new Size(blockSize,blockSize);
            drawRect = new Rectangle(point, size);
            if(x % blockSize == 0 || y % blockSize == 0)
                pictureBox3.Refresh();
        }

        #endregion

        #region 导航菜单操作
        private void 载入模型ToolStripMenuItem_Click(object sender, EventArgs e)
        {
           
        }

        private void 导出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog2.ShowDialog();
        }

        private void 保存ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveName == "")
                saveFileDialog1.ShowDialog();
            else
                saveMapData(saveName);
        }

        private void 另存为ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog();
        }

        private void 新建ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            creatMap form2 = new creatMap();
            form2.Visible = true;
            form2.Location = new Point(this.Location.X + 10, this.Location.Y + 10);
            form2.Disposed += new EventHandler(form2_Disposed);
        }

        void form2_Disposed(object sender, EventArgs e)
        {
            if (!mapSize.IsEmpty && isNewMap)
            {
                this.Release();
                this.InitializeEditor(fileName, null, mapSize);
                isNewMap = false;
            }
        }

        private void 打开ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog2.ShowDialog();
        }

        #endregion

        #region 控件操作
        private void mainForm_Load(object sender, EventArgs e)
        {
            arry_rect = new ArrayList();
            arry_block = new ArrayList();
            arry_posBlock = new ArrayList[2];
            arry_mapBlock = new ArrayList[2];
            arry_mapData = new ArrayList[2];
            arry_mapData[0] = new ArrayList();
            arry_mapData[1] = new ArrayList();
            arry_mapBlock[0] = new ArrayList();
            arry_mapBlock[1] = new ArrayList();
            arry_posBlock[0] = new ArrayList();
            arry_posBlock[1] = new ArrayList();
            arry_modle_resource = new ArrayList();
            hashModle = new Hashtable();
            pictureBox1.Paint += new PaintEventHandler(pictureBox1_Paint);
            pictureBox2.Paint += new PaintEventHandler(pictureBox2_Paint);
            pictureBox3.Paint += new PaintEventHandler(pictureBox3_Paint);
            pictureBox2.MouseDown += new MouseEventHandler(pictureBox2_MouseDown);
            pictureBox2.MouseUp += new MouseEventHandler(pictureBox2_MouseUp);
            pictureBox2.MouseMove += new MouseEventHandler(pictureBox2_MouseMove);
            pictureBox2.MouseClick += new MouseEventHandler(pictureBox2_MouseClick);
            pictureBox3.MouseMove += new MouseEventHandler(pictureBox3_MouseMove);
            pictureBox3.MouseDown += new MouseEventHandler(pictureBox3_MouseDown);

            //地形附加属性控制
            this.radioButton1.CheckedChanged += new EventHandler(radioButton1_CheckedChanged);
            this.radioButton2.CheckedChanged += new EventHandler(radioButton2_CheckedChanged);
            this.radioButton3.CheckedChanged += new EventHandler(radioButton3_CheckedChanged);
            this.radioButton4.CheckedChanged += new EventHandler(radioButton4_CheckedChanged);
            //模型附加属性控制
            this.checkBox4.CheckedChanged += new EventHandler(checkBox4_CheckedChanged);
            this.FormClosing += new FormClosingEventHandler(mainForm_FormClosing);

            //模型删除操作
            treeView1.KeyDown += new KeyEventHandler(treeView1_KeyDown);

            //模型tab装载
            if (treeView1.Nodes.Count > 0)
            {
                for (int i = 0; i < treeView1.Nodes.Count; i++)
                {
                    this.comboBox1.Items.Add(treeView1.Nodes[i].Text);
                }
            }
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            fileName = openFileDialog1.FileName;
            if (arry_block.Count > 0)
                arry_block.Clear();
            if (arry_rect.Count > 0)
                arry_rect.Clear();
            LoadBitmap(fileName,null);
            drawBlocks();
        }

        private void openFileDialog2_FileOk(object sender, CancelEventArgs e)
        {
            loadMapData(openFileDialog2.FileName);
        }

        private void openFileDialog3_FileOk(object sender, CancelEventArgs e)
        {
            this.textBox1.Text = openFileDialog3.FileName;
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            saveName = saveFileDialog1.FileName;
            saveMapData(saveFileDialog1.FileName);
        }

        private void saveFileDialog2_FileOk(object sender, CancelEventArgs e)
        {
            exportMapData(saveFileDialog2.FileName);
        }
        
        //点选模型
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Level > 2)
            {
                int a = e.Node.Parent.Index;
                int b = e.Node.Parent.Parent.Index * 10;
                int c = e.Node.Parent.Parent.Parent.Index * 1000;

                int key = a + b + c;
                if (hashModle.ContainsKey(key))
                {
                    AIX_M = 0;
                    if (checkBox4.Checked)
                        STOP_M = 3;
                    else
                        STOP_M = 0;
                    MIRROR_M = 0;
                    ArrayList _temp = arry_modle_resource[(int)hashModle[key]] as ArrayList;
                    int groupC = Encoding.ASCII.GetBytes(e.Node.Text.Substring(0, 1))[0] - 64;
                    pictureBox4.Image = (Bitmap)_temp[e.Node.Index + groupC];
                    pictureBox4.Update();
                }
            }
        }

        //删除模型
        void treeView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode.Equals(Keys.Delete) && this.treeView1.SelectedNode.Level > 2)
            {               
                int a = this.treeView1.SelectedNode.Parent.Index;
                int b = this.treeView1.SelectedNode.Parent.Parent.Index * 10;
                int c = this.treeView1.SelectedNode.Parent.Parent.Parent.Index * 1000;
                int key = a + b + c;

                if (hashModle.ContainsKey(key))
                {
                    int lastC = this.treeView1.SelectedNode.Parent.Nodes.Count;
                    int groupC = Encoding.ASCII.GetBytes(this.treeView1.SelectedNode.Text.Substring(0, 1))[0] - 64;
                    int _indexDelete = this.treeView1.SelectedNode.Index + groupC;
                    ArrayList _temp = arry_modle_resource[(int)hashModle[key]] as ArrayList;                   
                    _temp.RemoveAt(_indexDelete);
                    this.treeView1.SelectedNode.Remove();
                    if (lastC <= 1)
                    {
                        _temp.Clear();
                        arry_modle_resource.RemoveAt((int)hashModle[key]);
                        hashModle.Remove(key);
                    }
                }
            }
        }

        #region pictrueBox_Paint
        void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            label11.Refresh();
        }

        void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
            if(this.checkBox1.Checked == true)
                ShowLineJoin(mapSize.Width, mapSize.Height, e);
            if (this.checkBox2.Checked == true)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (arry_mapData[j].Count > 0)
                    {
                        for (int i = 0; i < arry_mapData[j].Count / 4; i++)
                        {
                            if (((int)arry_mapData[j][i * 4 + 2] % 4) == 3)
                            {
                                Bitmap _temp_bitmap = arry_mapData[j][i * 4 + 3] as Bitmap;
                                drawRectLineX(e, new Rectangle(((int)arry_mapData[j][i * 4 + 0] - offsetX) * blockSize, ((int)arry_mapData[j][i * 4 + 1] - offsetY) * blockSize, _temp_bitmap.Width, _temp_bitmap.Height));
                            }
                        }
                    }
                }
            }

            if (arry_mapData[0].Count > 0)
            {
                this.保存ToolStripMenuItem.Enabled = true;
                this.另存为ToolStripMenuItem.Enabled = true;
            }
            else
            {
                this.保存ToolStripMenuItem.Enabled = false;
                this.另存为ToolStripMenuItem.Enabled = false;
            }

            if (mouseRect_map != null)
            {
                drawRectLine(e, mouseRect_map);
            }
        }

        void pictureBox3_Paint(object sender, PaintEventArgs e)
        {            
            ShowLineJoin(pictureBox3.Width,pictureBox3.Height,e);
            drawRectLine(e, drawRect);
        }
        #endregion

        #region 地形块阻止属性_radioButtons
        void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
                STOP = 0;
        }

        void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
                STOP = 1;
        }

        void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked)
                STOP = 2;
        }

        void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton4.Checked)
                STOP = 3;
        }
        #endregion

        #region 模型阻止属性_CheckBox
        void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked)
                STOP_M = 3;
            else
                STOP_M = 0;
        }
        #endregion

        void mainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (pictureBox2.Image != null)
            {
                if (MessageBox.Show("是否记录当前地图?", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    saveFileDialog1.ShowDialog();
                }
            }
        }

        private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            int a = e.NewValue;
            temp_a = e.NewValue;
            this.label11.Text = a.ToString();
            this.drawBlocks();
        }

        private void 帮助ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Made by NPC9(c)2007","版权申明",MessageBoxButtons.OK);
        }

        #region 按钮们
        private void button1_Click(object sender, EventArgs e) //地形旋转
        {
            if (pictureBox1.Image != null)
            {
                if (++AIX > 3)
                    AIX = 0;
                Bitmap b = new Bitmap(pictureBox1.Image);
                b = this.RotateFlip(false, b);
                pictureBox1.Image = b;
                pictureBox1.Update();
            }
        }

        private void button8_Click(object sender, EventArgs e) //模型旋转
        {
            if (pictureBox4.Image != null)
            {
                if (++AIX_M > 3)
                    AIX_M = 0;
                Bitmap b = new Bitmap(pictureBox4.Image);
                b = this.RotateFlip(false, b);
                pictureBox4.Image = b;
                pictureBox4.Update();
            }
        }

        private void button2_Click(object sender, EventArgs e)//地形静象
        {
            if (pictureBox1.Image != null)
            {
                if (++MIRROR > 1)
                    MIRROR = 0;
                Bitmap b = new Bitmap(pictureBox1.Image);
                b = this.RotateFlip(true, b);
                pictureBox1.Image = b;
                pictureBox1.Update();
            }
        }

        private void button7_Click(object sender, EventArgs e)//模型静象
        {
            if (pictureBox4.Image != null)
            {
                if (++MIRROR_M > 1)
                    MIRROR_M = 0;
                Bitmap b = new Bitmap(pictureBox4.Image);
                b = this.RotateFlip(true, b);
                pictureBox4.Image = b;
                pictureBox4.Update();
            }
        }

        //地图偏移
        private void button3_Click(object sender, EventArgs e)
        {
            int a = mapSize.Width / blockSize + 1;
            int b = pictureBox2.Width / blockSize + offsetX;
            if(a-b > 0)
                offsetX += 1;
            drawMaps();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            int a = mapSize.Width / blockSize + 1;
            int b = pictureBox2.Width / blockSize + offsetX;
            if(offsetX > 0)
                offsetX -= 1;
            drawMaps();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            int a = mapSize.Height / blockSize + 1;
            int b = pictureBox2.Height / blockSize + offsetY;
            if (offsetY > 0)
                offsetY -= 1;
            drawMaps();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            int a = mapSize.Height / blockSize + 1;
            int b = pictureBox2.Height / blockSize + offsetY;
            if (a - b > 0)
                offsetY += 1;
            drawMaps();
        }

        private void button9_Click(object sender, EventArgs e) //模型文件打开
        {
            openFileDialog3.ShowDialog();
        }

        private void button10_Click(object sender, EventArgs e) //装载模型
        {
            int w = Int32.MaxValue;
            int h = Int32.MaxValue;
            if (textBox1.Text == "" || textBox3.Text == "" || comboBox1.SelectedItem == null || comboBox2.SelectedItem == null || comboBox3.SelectedItem == null)
                MessageBox.Show("please choose correct model's parameter!");
            else if (radioButton6.Checked && (!Int32.TryParse(textBox2.Text, out w) || !Int32.TryParse(textBox4.Text, out h)))
                MessageBox.Show("Please input correct size if you want blocks");
            else if (mapSize.IsEmpty)
                MessageBox.Show("Please Create new Map first!");
            else if (radioButton6.Checked && Convert.ToInt32(textBox2.Text) % blockSize != 0 || Convert.ToInt32(textBox4.Text) % blockSize != 0)
                MessageBox.Show("Please Input correct block params! (param % block == 0)");
            else
            {
                bool isBlocks = this.radioButton6.Checked;
                string FilePath = textBox1.Text;
                string NameModle = textBox3.Text;
                int TypeModle = comboBox1.SelectedIndex * 1000 + comboBox2.SelectedIndex * 10 + comboBox3.SelectedIndex;
                Size SizeModle = new Size(w, h);

                using (Model ml = new Model())
                {
                    if (ml.InitialModle(FilePath, TypeModle, NameModle, isBlocks, SizeModle))
                    {
                        int ttemp_group = 0;
                        int index = (int)ml.arryModle[0];
                        string _temp_NameModle = NameModle;
                        ArrayList temp_modle = new ArrayList(ml.arryModle);
                        temp_modle.RemoveAt(0); //移除树状索引

                        if (hashModle.ContainsKey(index))
                        {
                            int ascII = 65;
                            ArrayList _temp = arry_modle_resource[(int)hashModle[index]] as ArrayList;
                            while (_temp.Contains(ascII) && ascII < 91)
                            {
                                if ((_temp.IndexOf(ascII) + 1) < _temp.Count)
                                {
                                    if (!_temp[_temp.IndexOf(ascII) + 1].GetType().Equals(typeof(Bitmap)))
                                    {
                                        ttemp_group = ascII;
                                        _temp.InsertRange(_temp.IndexOf(ascII) + 1, temp_modle);
                                        goto InitialModle;
                                    }
                                }
                                else
                                {
                                    ttemp_group = ascII;
                                    _temp.AddRange(temp_modle);
                                    goto InitialModle;
                                }
                                ++ascII;
                            }
                            ttemp_group = ascII;
                            temp_modle.Insert(0, ttemp_group);
                            _temp.AddRange(temp_modle);
                        }
                        else
                        {
                            ttemp_group = 65;
                            temp_modle.Insert(0, ttemp_group);
                            arry_modle_resource.Add(temp_modle);
                            hashModle.Add(index, arry_modle_resource.Count - 1);
                        }
                    InitialModle :
                        byte[] byteArray = new byte[] { (byte)ttemp_group };
                        _temp_NameModle = Encoding.ASCII.GetString(byteArray) + _temp_NameModle;
                        InitialTreeNodes(_temp_NameModle, index, temp_modle.Count - 1);
                    }
                }
            }
        }

        private void InitialTreeNodes(string nodeName, int treeIndex, int counts)
        {
            int _temp_a = treeIndex / 1000;
            int _temp_b = (treeIndex - _temp_a * 1000) / 10;
            int _temp_c = treeIndex - _temp_a * 1000 - _temp_b * 10;
            for (int i = 0; i < counts; i++)
            {
                this.treeView1.Nodes[_temp_a].Nodes[_temp_b].Nodes[_temp_c].Nodes.Add(nodeName+i.ToString());
            }
        }
        #endregion

        #endregion

        #region combox
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.comboBox2.Text = "子类";
            this.comboBox2.Items.Clear();
            int temp = treeView1.Nodes[comboBox1.SelectedIndex].Nodes.Count;
            if( temp > 0)
            {
                for (int i = 0; i < temp; i++)
                {
                    this.comboBox2.Items.Add(treeView1.Nodes[comboBox1.SelectedIndex].Nodes[i].Text);
                }
            }
            
        }
        #endregion

        #region checkBoxs
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            pictureBox2.Refresh();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            pictureBox2.Refresh();
        }
        #endregion

        #region Clear_up
        private void Release()
        {
            arry_block.Clear();
            arry_rect.Clear();
            for (int i = 0; i < 2; i++)
            {
                arry_mapBlock[i].Clear();
                arry_mapData[i].Clear();
                arry_posBlock[i].Clear();
                arry_modle_resource.Clear();
                hashModle.Clear();
            }
            for (int tre_0 = 0; tre_0 < this.treeView1.Nodes.Count; tre_0++)
            {
                for (int tre_1 = 0; tre_1 < this.treeView1.Nodes[tre_0].Nodes.Count; tre_1++)
                {
                    for (int tre_2 = 0; tre_2 < this.treeView1.Nodes[tre_0].Nodes[tre_1].Nodes.Count; tre_2++)
                    {
                        this.treeView1.Nodes[tre_0].Nodes[tre_1].Nodes[tre_2].Nodes.Clear();
                    }
                }
            }
            this.pictureBox1.Image = null;
            this.pictureBox4.Image = null;
            this.pictureBox1.Update();
            this.pictureBox4.Update();
        }
        #endregion

        private void button11_Click(object sender, EventArgs e)
        {
            int _mapWidth = 0;
            int _mapHeight = 0;
            int _textureSize = 0;
            using (FileStream fs = File.OpenRead(@"D:\Visual Studio 2005\Projects\TextDx5\TextDx5\map\myMap_01.atl"))
            {
                if (fs.Length > 0)
                {
                    byte[] sumBytes = new byte[fs.Length];
                    fs.Read(sumBytes, 0, sumBytes.Length);

                    int wal = sumBytes[0]; //width基数
                    int wue = sumBytes[1]; //余数

                    int hal = sumBytes[2]; //height基数
                    int hue = sumBytes[3]; //余数

                    _mapWidth = wal * 255 + wue;
                    //_mapWidth = 720;
                    _mapHeight = hal * 255 + hue;

                    _textureSize = sumBytes[4]; //获得块大小

                    int groupMultiple = sumBytes[5]; //倍率
                    int gal = sumBytes[6];
                    int gue = sumBytes[7];
                    int groupCounts = gal * groupMultiple * 255 + gue;

                    byte[] sumBytes_v = new byte[groupCounts];
                    Array.Copy(sumBytes, 8, sumBytes_v, 0, sumBytes_v.Length);
                    groupCounts = Convert.ToInt32(Encoding.Default.GetString(sumBytes_v));

                    sumBytes_v = new byte[sumBytes.Length - sumBytes_v.Length - 8];
                    Array.Copy(sumBytes, sumBytes.Length - sumBytes_v.Length, sumBytes_v, 0, sumBytes_v.Length);


                    for (int i = 0; i < groupCounts; i++)
                    {
                        sumBytes_v = new byte[sumBytes_v.Length - 3];
                        Array.Copy(sumBytes, sumBytes.Length - sumBytes_v.Length, sumBytes_v, 0, sumBytes_v.Length);
                    }

                    using (MemoryStream ms = new MemoryStream(sumBytes_v))
                    {
                        Bitmap image = new Bitmap(ms);
                        ShowWin sw = new ShowWin();
                        sw.Visible = true;
                        sw.showW(image);
                    }
                }
            }
        }
    }
}