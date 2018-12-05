using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace ComVideo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }
        private void Change(string cmd)
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";//指定启动命令窗口
            p.StartInfo.UseShellExecute = false;//不使用系统外壳程序启动进程
            p.StartInfo.CreateNoWindow = true;//不显示dos程序窗口
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;//把错误输出写到StandardError流中
            p.Start();//开启进程
            p.StandardInput.WriteLine(cmd + "&exit");//指定执行的命令
            p.StandardInput.AutoFlush = true;//刷新缓冲区
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;//隐藏窗口样式
            p.WaitForExit();//阻塞等待进程结束
            p.Close();//关闭进程
            p.Dispose();//释放资源
        }
        private void button1_Click(object sender, EventArgs e)
        {
            string temp = Application.StartupPath + "\\Temp\\";//设置临时文件路径
            string strMP4 = "";//记录最终mp4的路径
            //转换临时片头
            Change(Application.StartupPath + "\\ffmpeg -i " + textBox1.Text + " -vcodec copy -acodec copy -vbsf h264_mp4toannexb " + temp + "begin.ts");
            //转换临时片尾
            Change(Application.StartupPath + "\\ffmpeg -i " + textBox3.Text + " -vcodec copy -acodec copy -vbsf h264_mp4toannexb " + temp + "end.ts");
            System.Threading.ThreadPool.QueueUserWorkItem(//使用线程池
                 (P_temp) =>
                 {
                     button1.Enabled = false;
                     for (int i = 0; i < listView1.Items.Count; i++)//遍历要合成的所有视频
                     {
                         //将遍历到的视频转换为临时视频文件
                         Change(Application.StartupPath + "\\ffmpeg -i " + listView1.Items[i].Text + " -vcodec copy -acodec copy -vbsf h264_mp4toannexb " + temp + "temp.ts");
                         //合成临时文件
                         Change("copy/b " + temp + "begin.ts + " + temp + "temp.ts + " + temp + "end.ts / y " + temp + "combine.ts");
                         strMP4 = textBox4.Text.TrimEnd(new char[] { '\\' }) + "\\" + new FileInfo(listView1.Items[i].Text).Name;//记录最终mp4的路径
                         if (File.Exists(strMP4))//判断文件是否已经存在
                             File.Delete(strMP4);//如果存在则删除
                         //生成最终的mp4
                         Change(Application.StartupPath + "\\ffmpeg -i " + temp + "combine.ts -acodec copy -vcodec copy -absf aac_adtstoasc " + strMP4);
                         File.Delete(temp + "temp.ts");//删除临时视频文件
                         File.Delete(temp + "combine.ts");//删除临时合成文件
                     }
                     File.Delete(temp + "begin.ts");//删除临时片头文件
                     File.Delete(temp + "end.ts");//删除临时片尾文件
                     MessageBox.Show("视频合成成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                     button1.Enabled = true;
                 });
        }
        //选择要转换的视频资源所在文件夹
        private void button3_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                listView1.Items.Clear();//清空文件列表
                textBox2.Text = folderBrowserDialog1.SelectedPath;//记录选择路径
                DirectoryInfo dir = new DirectoryInfo(textBox2.Text);
                FileSystemInfo[] files = dir.GetFiles();//获取文件夹中所有文件
                string path = dir.FullName.TrimEnd(new char[] { '\\' }) + "\\";//获取文件所在目录
                string newPath;
                System.Threading.ThreadPool.QueueUserWorkItem(//使用线程池
                     (P_temp) =>
                     {
                         foreach (FileInfo file in files)//遍历所有文件
                         {
                             newPath = file.FullName;
                             if (file.Extension.ToLower() == ".mp4")//如果是视频文件
                             {
                                 if (file.Name.IndexOf(" ") != -1)
                                 {
                                     newPath = path + file.Name.Replace(" ", ""); ;//设置更名后的文件的完整路径
                                     File.Copy(file.FullName, newPath, true);//将更名后的文件复制到原目录下
                                     File.Delete(file.FullName);//删除原目录下的原始文件
                                 }
                                 listView1.Items.Add(newPath);//显示文件列表
                             }
                         }
                     });
            }
        }
        //设置片尾
        private void button4_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox3.Text = openFileDialog1.FileName;//选择片尾
            }
        }
        //设置片头
        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;//选择片头
            }
        }
        //设置视频保存路径
        private void button5_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox4.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //清空缓存文件
            DirectoryInfo dinfo = new DirectoryInfo(Application.StartupPath + "\\Temp\\");
            foreach (FileInfo f in dinfo.GetFiles())
                f.Delete();
        }
    }
}
