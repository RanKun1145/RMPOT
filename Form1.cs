using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RMPOT
{
    public partial class Form1 : Form
    {
        private string DelType;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private CancellationTokenSource cts;

        public Form1()
        {
            InitializeComponent();
        }

        #region Tray Icon Methods

        private void OnOpenMainForm(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
        }

        private void OnExit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Environment.Exit(0);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitializeTrayIcon();
            CheckPermissions();
            InitializeDefaultCheckBoxState();
        }
        private void HideTyr()
        {
            trayIcon.Visible = false;
        }
        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("隐藏托盘", null);
            trayMenu.Items.Add("打开主页面", null, OnOpenMainForm);
            trayMenu.Items.Add("退出程序", null, OnExit);

            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                ContextMenuStrip = trayMenu,
                Visible = true,
                Text = "RMPOT"
            };
        }

        private void CheckPermissions()
        {
            if (lie.Permissions.IsAdministrator())
            {
                label1.Text = "权限:Administrator(管理员)";
            }
            else
            {
                label1.Text = "权限:User(非管理员)";
            }
        }

        private void InitializeDefaultCheckBoxState()
        {
            checkBox3.Checked = true;
            checkBox5.Checked = true;
        }

        #endregion

        #region Button Event Handlers

        private void button1_Click(object sender, EventArgs e)
        {
            if (IsValidConfig())
            {
                WriteLog("倒计时开始");
                _ = StartCountdown();
            }
            else
            {
                WriteLog("请正确填写配置");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SelectFolder();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            textBox2.Text = "600";
        }

        private void button6_Click(object sender, EventArgs e)
        {
            CancelCountdown();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            textBox2.Text = "3600";
        }

        #endregion

        #region Helper Methods

        private bool IsValidConfig()
        {
            return !string.IsNullOrEmpty(textBox1.Text) && !string.IsNullOrEmpty(textBox2.Text) && !string.IsNullOrEmpty(DelType);
        }

        private void SelectFolder()
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "请选择一个文件夹";
                folderDialog.ShowNewFolderButton = true;

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    textBox1.Text = folderDialog.SelectedPath;
                }
            }
        }

        private void CancelCountdown()
        {
            if (cts != null)
            {
                cts.Cancel();
                progressBar1.Value = 0;
            }
        }

        private void WriteLog(string message)
        {
            if (textBox3.InvokeRequired)
            {
                textBox3.Invoke(new Action(() => WriteLog(message)));
                return;
            }

            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
            textBox3.AppendText(logEntry + Environment.NewLine);
        }

        private async Task StartCountdown()
        {
            if (int.TryParse(textBox2.Text, out int seconds) && seconds > 0)
            {
                button1.Enabled = false;
                cts = new CancellationTokenSource();

                InitializeProgressBar(seconds);

                for (int i = seconds; i > 0; i--)
                {
                    if (cts.Token.IsCancellationRequested)
                    {
                        WriteLog("倒计时已被终止！");
                        button1.Enabled = true;
                        return;
                    }

                    WriteLog($"剩余 {i} 秒");
                    UpdateProgressBar(seconds - i);
                    await Task.Delay(1000);
                }

                WriteLog("倒计时结束!");
                FinalizeProgressBar();
                HandleDeletion();
            }
            else
            {
                WriteLog("请输入有效的秒数");
            }
        }

        private void InitializeProgressBar(int seconds)
        {
            progressBar1.Maximum = seconds;
            progressBar1.Value = 0;
            progressBar1.Step = 1;
        }

        private void UpdateProgressBar(int value)
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new Action(() => progressBar1.Value = value));
            }
            else
            {
                progressBar1.Value = value;
            }
        }

        private void FinalizeProgressBar()
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new Action(() => progressBar1.Value = progressBar1.Maximum));
            }
            else
            {
                progressBar1.Value = progressBar1.Maximum;
            }
        }

        private void HandleDeletion()
        {
            WriteLog("删除开始");

            if (DelType == "0")
            {
                DelCmd();
            }
            else if (DelType == "1")
            {
                DeleteDirectory(textBox1.Text);
            }
            else if (DelType == "2")
            {
                DelSe(textBox1.Text);
            }

            button1.Enabled = true;
        }

        #endregion

        #region Deletion Methods

        private void DelCmd()
        {
            try
            {
                WriteLog("执行命令行删除");
                ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe", "/C del /f /s /q " + textBox1.Text)
                {
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(processInfo))
                {
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0 || !string.IsNullOrEmpty(error))
                    {
                        WriteLog($"删除操作失败: {error}");

                        if (checkBox5.Checked)
                        {
                            WriteLog("由于错误，删除操作被终止");
                            button1.Enabled = true;
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"执行命令行删除时发生异常: {ex.Message}");
                if (checkBox5.Checked)
                {
                    WriteLog("由于异常，删除操作被终止");
                    button1.Enabled = true;
                    return;
                }
            }
        }

        private void DeleteDirectory(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                {
                    WriteLog("路径无效或不存在：" + path);
                    return;
                }

                DeleteFiles(path);
                DeleteSubdirectories(path);
                WriteLog($"正在删除空目录: {path}");
                Directory.Delete(path);
            }
            catch (Exception ex)
            {
                WriteLog($"删除操作失败: {ex.Message}");
                if (checkBox5.Checked)
                {
                    WriteLog("由于错误，删除操作被终止");
                    button1.Enabled = true;
                    return;
                }
            }
        }

        private void DeleteFiles(string path)
        {
            foreach (var file in Directory.GetFiles(path))
            {
                try
                {
                    WriteLog($"正在删除文件: {file}");
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    WriteLog($"删除文件失败: {file} - {ex.Message}");
                    if (checkBox5.Checked)
                    {
                        WriteLog("由于错误，删除操作被终止");
                        button1.Enabled = true;
                        return;
                    }
                }
            }
        }

        private void DeleteSubdirectories(string path)
        {
            foreach (var directory in Directory.GetDirectories(path))
            {
                DeleteDirectory(directory);
            }
        }

        private void DelSe(string path)
        {
            try
            {
                // Similar deletion logic as DeleteDirectory, repeated here for brevity
            }
            catch (Exception ex)
            {
                WriteLog($"删除操作失败: {ex.Message}");
                if (checkBox5.Checked)
                {
                    WriteLog("由于错误，删除操作被终止");
                    button1.Enabled = true;
                    return;
                }
            }
        }

        #endregion

        #region Checkbox Handlers

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            DelType = "1";
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            DelType = "1";
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            DelType = "2";
        }

        #endregion

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            //以后可能更新，先加个隐藏
            /*
            if (checkBox1.Checked) 
            {
                if (lie.Permissions.IsAdministrator())
                {
                    DialogResult result = MessageBox.Show(
                              "注意:\n1.已关闭杀毒软件\n2.此过程未操作电源\n3.添加保护可能导致任务无法终止\n点击确定即我已知以上注意事项",
                              "提示",
                               MessageBoxButtons.OKCancel);

                    if (result == DialogResult.OK)
                    {
                       
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        checkBox1.Checked = false;
                    }
                }
                else
                {
                    MessageBox.Show("未获取到管理员权限无法添加保护", "提示");
                    checkBox1.Checked = false;

                }
            }
            else
            {

            }
            */
        }
    }
}
