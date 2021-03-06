﻿using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using Baidu.Aip.ImageSearch;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Diagnostics;


/*
////////////////////////////////////////////// 问题
XP下API安全链接出错

////////////////////////////////////////////// 进度
本地搜索增加3个DATA索引
更新功能
帮助功能
安装程序
*/

namespace BaiduSimilarImage
{
    /// <summary>
    /// 程序主窗口
    /// </summary>
    public partial class HomeForm : Form
    {
        public HomeForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 更新地址
        /// </summary>
        private string appUpURL;


        #region 程序配置
        /// <summary>
        /// 读取程序配置
        /// </summary>
        private void Settings()
        {
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(@"Documents\Settings.xml");
                XmlNode rootNode = xmlDocument.DocumentElement;// 定位根节点
                foreach (XmlNode xmlNode in rootNode.ChildNodes)
                {
                    if (xmlNode.Name == "Text")
                    {
                        this.Text = xmlNode.Attributes["name"].Value + " - " + Application.ProductVersion;// 程序标题
                        aboutAppMenu.Text = "关于 " + xmlNode.Attributes["name"].Value;// 关于菜单
                        continue;
                    }
                    if (xmlNode.Name == "Up")
                    {
                        appUpURL = xmlNode.Attributes["path"].Value;// 程序更新地址
                        continue;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("无权限访问程序配置文件，程序将自动退出，请尝试使用管理员权限重新运行本程序", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
                return;
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("程序配置文件不存在，程序将自动退出", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("访问程序配置文件时发生错误，程序将自动退出，错误描述如下\r\n\r\n" + ex.ToString(), "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
                return;
            }

            try// 读取程序图标
            {
                Icon = new Icon(Path.Combine(Application.StartupPath, @"Skin\Icon.ico"));
                imageAddButton.Image = Image.FromFile(@"Skin\Add.png");
                imageUpButton.Image = Image.FromFile(@"Skin\Up.png");
                imageDeleteButton.Image = Image.FromFile(@"Skin\Delete.png");
                apiSettingsButton.Image = Image.FromFile(@"Skin\Setting.png");
                imageSortButton.Image = Image.FromFile(@"Skin\Sort.png");
                helpButton.Image = Image.FromFile(@"Skin\Help.png");
                emptyFolderButton.Image = Image.FromFile(@"Skin\EmptyFolder.png");
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("无权限加载工具栏图标文件，请尝试使用管理员权限重新运行本程序", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
                return;
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("工具栏图标文件不存在，程序将自动退出", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载工具栏图标时发生如下错误，程序将自动退出，错误描述如下\r\n\r\n" + ex.ToString(), "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
                return;
            }
        }
        #endregion 程序配置

        #region 初始化
        /// <summary>
        /// 窗口载入时
        /// </summary>
        private void HomeForm_Load(object sender, EventArgs e)
        {
            // Control.CheckForIllegalCrossThreadCalls = false;// 关闭back访问限制
            Settings();// 程序配置
            AppUpdata.Updata(appUpURL, false);// 程序升级
            if (ApiFunction.GetDepotList() != null) onlineDepotCombobox.DataSource = localDepotCombobox.DataSource = viewComboBox.DataSource = ApiFunction.GetDepotList();// 所有图库下拉列表数据源
            LocalListViewSettings();// 本地搜索列表配置
            onlinePicturebox.AllowDrop = true;// 重写AllowDrop使其接受拖放
        }
        #endregion 初始化

        #region 在线搜索
        /// <summary>
        /// onlineAPI实例
        /// </summary>
        private Api onlineApi;

        /// <summary>
        /// 在线图库选择
        /// </summary>
        private void onlineDepotCombobox_SelectedIndexChanged(object sender, EventArgs e)
        {
            onlineApi = ApiFunction.GetApi(onlineDepotCombobox.Text);// 获取API配置
            if (onlineApi != null) onlineQuantityCombobox.Text = onlineApi.Quantity.ToString();// 返回数
        }

        /// <summary>
        /// 在线搜索按钮
        /// </summary>
        private void onlineSearchButton_Click(object sender, EventArgs e)
        {
            if (onlinePicturebox.Image == null)
            {
                if (onlinePathTextBox.Text == "")// 图片未加载 - 路径空
                {
                    MessageBox.Show("未选择图片位置", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!File.Exists(onlinePathTextBox.Text))// 图片未加载 - 路径错误
                {
                    MessageBox.Show("图片位置不正确", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!ApiFunction.AcceptFormatByExtension(onlinePathTextBox.Text))// 图片未加载 - 格式不支持
                {
                    MessageBox.Show(ApiFunction.GetError("216201"), "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);// 扩展名不受支持，获取错误提示
                    return;
                }
            }

            if (onlineDepotCombobox.Text == "")
            {
                MessageBox.Show("未选择图库", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!Directory.Exists(onlineApi.Path))
            {
                MessageBox.Show("本地图片库 " + onlineApi.Path + "不存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (searchBack.IsBusy)// 在线搜索异步调用
            {
                MessageBox.Show("请先停止搜索", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }

            byte[] bytes = ApiFunction.ImagetoByte(onlinePicturebox.Image);// 图片转换字节
            var back = new object[3];// 装箱
            back[0] = onlineDepotCombobox.Text;// 图库
            back[1] = bytes;// 图片
            back[2] = int.Parse(onlineQuantityCombobox.Text);// 返回数
            searchBack.RunWorkerAsync(back);// 传入
            progressBar.Value = 1;
            progressLabel.Text = "准备搜索...";

            searchTtabontrol.Enabled = false;
            toolPanel.Enabled = false;
            searchMenuStrip.Enabled = false;
        }

        /// <summary>
        /// 在线搜索后台
        /// </summary>
        private void searchBack_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            var back = e.Argument as object[];// 拆箱
            Api api = ApiFunction.GetApi((string)back[0]);// 获取API配置
            if (api != null)
            {
                byte[] bytes = (byte[])back[1];// 图片
                int quantity = (int)back[2];// 返回数
                searchBack.ReportProgress(50, "搜索中");// 进度

                ImageSearch client = ApiFunction.GetClient(api.Appid, api.Apikey, api.Secreykey, api.Timeout);// 搜索后台启用
                if (client == null) return;

                JObject json = ApiFunction.Search(client, bytes, api.Tags1, api.Tags2, quantity);// 入库
                if (json == null) return;

                e.Result = json;
            }
        }

        /// <summary>
        /// 在线搜索进度
        /// </summary>
        private void searchBack_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            progressBar.Value = (e.ProgressPercentage < 101) ? e.ProgressPercentage : progressBar.Value;
            progressLabel.Text = progressBar.Value.ToString() + "% " + e.UserState as string;
        }

        /// <summary>
        /// 在线搜索后台完成
        /// </summary>
        private void searchBack_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            searchTtabontrol.Enabled = true;
            toolPanel.Enabled = true;
            searchMenuStrip.Enabled = true;

            if (e.Error != null)// 错误
            {
                MessageBox.Show("搜索后台错误如下\r\n\r\n" + e.Error.ToString(), "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            JObject json = e.Result as JObject;// 接收传出
            if (json == null || json.ToString() == "")
            {
                MessageBox.Show("API返回了错误，搜索已终止", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (json.Property("error_code") != null && json.Property("error_code").ToString() != "") MessageBox.Show(ApiFunction.GetError(json["error_code"].ToString()));// 返回Json包含错误信息
            if (json.Property("result_num") != null && json.Property("result_num").ToString() != "")// 返回Json正确
            {
                int num = json["result_num"].Value<int>();// 总数
                if (num == 0)
                {
                    MessageBox.Show("图库返回了0条结果", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    progressBar.Value = 100;
                    progressLabel.Text = "搜索完成";
                    return;
                }
                JToken result = json["result"];
                List<string> list = new List<string>();
                for (int i = 0; i < num; i++) list.Add(result[i]["brief"].ToString());
                progressBar.Value = 100;
                progressLabel.Text = "搜索完成";
                ApiSearchResultForm SearchBEForm = new ApiSearchResultForm(list, onlineDepotCombobox.Text, onlineApi.Path);
                SearchBEForm.ShowDialog();
            }
        }

        /// <summary>
        /// 浏览图片按钮
        /// </summary>
        private void onlineOpenPath_Click(object sender, EventArgs e)
        {
            OpenFileDialog openfiledialog = new OpenFileDialog();
            if (openfiledialog.ShowDialog() != DialogResult.OK) return;
            if (ApiFunction.AcceptFormatByExtension(openfiledialog.FileName))
            {
                onlinePicturebox.ImageLocation = openfiledialog.FileName;
                onlinePathTextBox.Text = openfiledialog.FileName;
                progressBar.Value = 0;
                progressLabel.Text = "已选择图片";
            }
            else MessageBox.Show(ApiFunction.GetError("216201"), "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);// 扩展名不受支持，获取错误提示
        }

        /// <summary>
        /// 位置输入实时检测
        /// </summary>
        private void onlinePathTextBox_TextChanged(object sender, EventArgs e)
        {
            if (File.Exists(onlinePathTextBox.Text) && ApiFunction.AcceptFormatByExtension(onlinePathTextBox.Text)) onlinePicturebox.ImageLocation = onlinePathTextBox.Text;// 路径与格式同时检测
        }

        /// <summary>
        /// 拖放输入
        /// </summary>
        private void onlinePicturebox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.All;
            else e.Effect = DragDropEffects.None;
        }

        /// <summary>
        /// 拖放动作
        /// </summary>
        private void onlinePicturebox_DragDrop(object sender, DragEventArgs e)
        {
            string imageName = (e.Data.GetData(DataFormats.FileDrop, false) as string[])[0];// 获取第一个文件名
            if (ApiFunction.AcceptFormatByExtension(imageName))
            {
                onlinePicturebox.ImageLocation = imageName;
                onlinePathTextBox.Text = imageName;
                progressBar.Value = 0;
                progressLabel.Text = "已选择图片";
            }
            else MessageBox.Show(ApiFunction.GetError("216201"), "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);// 扩展名不受支持，获取错误提示
        }

        /// <summary>
        /// 清除搜索图片
        /// </summary>
        private void onlineClearButton_Click(object sender, EventArgs e)
        {
            if (searchBack.IsBusy)// 在线搜索异步调用
            {
                MessageBox.Show("请先停止搜索", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            onlinePicturebox.Image = null;
            progressBar.Value = 0;
            onlinePathTextBox.Text = "";
            progressLabel.Text = "等待用户操作";
        }
        #endregion 在线搜索

        #region 本地搜索
        /// <summary>
        /// 本地图库
        /// </summary>
        private Api outlineApi;

        /// <summary>
        /// 本地图库路径
        /// </summary>
        private string localImagePath;

        /// <summary>
        /// 本地图库选择时
        /// </summary>
        private void localDepotCombobox_SelectedIndexChanged(object sender, EventArgs e)
        {
            outlineApi = ApiFunction.GetApi(localDepotCombobox.Text);
            if (outlineApi != null) localImagePath = outlineApi.Path;
        }

        /// <summary>
        /// 本地搜索结果列表
        /// </summary>
        private void LocalListViewSettings()
        {
            localListView.Columns.Add("文件名");
            localListView.Columns.Add("位置");
            localListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        /// <summary>
        /// 本地匹配查找按钮
        /// </summary>
        private void localSearchButton_Click(object sender, EventArgs e)
        {
            if (localTypeTextBox.Text == "")
            {
                MessageBox.Show("请输入内容", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (localDepotCombobox.Text == "")
            {
                MessageBox.Show("未选择图库", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!Directory.Exists(outlineApi.Path))
            {
                MessageBox.Show(outlineApi.Path + " 目录不存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MethodInfo function = new Reflections().Compiler(outlineApi.Table);// 传入类名调用实时编译
            if (function == null) return;
            string newType = (string)function.Invoke(null, new object[] { localTypeTextBox.Text });// 调用函数获取返回值
            if (localTypeTextBox.Text == newType)
            {
                MessageBox.Show(localTypeTextBox.Text + "不是" + localDepotCombobox.Text);
                return;
            }

            if (localListView.Items.Count > 0)// 清空列表
            {
                localListView.Clear();
                LocalListViewSettings();
            }

            string path = Path.GetDirectoryName(Path.Combine(outlineApi.Path, newType));// 目录
            string type = localTypeTextBox.Text;// 订单号

            progressBar.Value = 50;
            progressLabel.Text = "查找中...";

            List<string> list = new List<string>();// 返回的文件列表
            if (Directory.Exists(path))
            {
                #region 获取文件列表
                Stack<string> stack = new Stack<string>(20);// 栈
                stack.Push(path);// 主目录入栈
                while (stack.Count > 0)// 栈不为空时遍历
                {
                    string mainPath = stack.Pop();// 取栈中第一个目录

                    string[] subPaths = null;
                    try { subPaths = Directory.GetDirectories(mainPath); }// 栈目录的子目录列表
                    #region 异常
                    catch (UnauthorizedAccessException ex)
                    {
                        if (MessageBox.Show("无权限操作，请尝试使用管理员权限运行本程序，描述如下\r\n\r\n" + ex + "\r\n\r\n是否继续？", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK) continue;
                        else return;
                    }
                    catch (FileNotFoundException ex)
                    {
                        if (MessageBox.Show("文件或文件夹不存在，描述如下\r\n\r\n" + ex + "\r\n\r\n是否继续归类？", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK) continue;
                        else return;
                    }
                    catch (Exception ex)
                    {
                        if (MessageBox.Show("发生未知如下错误\r\n\r\n" + ex + "\r\n\r\n是否继续归类？", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK) continue;
                        else return;
                    }
                    #endregion 异常

                    string[] files = null;
                    try { files = Directory.GetFiles(mainPath); }// 栈目录的文件列表
                    #region 异常
                    catch (UnauthorizedAccessException ex)
                    {
                        if (MessageBox.Show("无权限操作，请尝试使用管理员权限运行本程序，描述如下\r\n\r\n" + ex + "\r\n\r\n是否继续？", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK) continue;
                        else return;
                    }
                    catch (FileNotFoundException ex)
                    {
                        if (MessageBox.Show("文件或文件夹不存在，描述如下\r\n\r\n" + ex + "\r\n\r\n是否继续归类？", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK) continue;
                        else return;
                    }
                    catch (Exception ex)
                    {
                        if (MessageBox.Show("发生未知如下错误\r\n\r\n" + ex + "\r\n\r\n是否继续归类？", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK) continue;
                        else return;
                    }
                    #endregion 异常

                    if (files.Length > 0) foreach (string file in files) list.Add(file);// 栈目录的文件遍历到List
                    if (subPaths.Length > 0) foreach (string subPath in subPaths) stack.Push(subPath);// 栈目录的子目录列表入栈
                }
                #endregion 获取文件列表
            }
            else
            {
                MessageBox.Show(type + "订单号正确，但目录 " + path + " 不存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                progressBar.Value = 100;
                progressLabel.Text = "查找完成";
                return;
            }

            foreach (string s in list)// 遍历
            {
                if (Path.GetFileName(s).Contains(type))// 只获取包含订单号的文件
                {
                    ListViewItem item = localListView.Items.Add(Path.GetFileName(s));// 文件名
                    item.SubItems.Add(s);// 位置
                }
            }

            if (localListView.Items.Count > 0) localListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);// 自动列宽
            else MessageBox.Show(type + "订单号正确，但目录 " + path + " 内没有查找到相关文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);

            progressBar.Value = 100;
            progressLabel.Text = "查找完成";
        }

        /// <summary>
        /// 本地相似查找按钮
        /// </summary>
        private void localSearch2Button_Click(object sender, EventArgs e)
        {
            if (localTypeTextBox.Text == "")
            {
                MessageBox.Show("请输入内容", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (localDepotCombobox.Text == "")
            {
                MessageBox.Show("未选择图库", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!Directory.Exists(outlineApi.Path))
            {
                MessageBox.Show(outlineApi.Path + " 目录不存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (localListView.Items.Count > 0)// 清空结果
            {
                localListView.Clear();
                LocalListViewSettings();
            }

            if (localSearch2Button.Text == "数据库查找")
            {
                localSearch2Button.Text = "停止";
                if (!outlineBack.IsBusy)// 后台占用
                {
                    var back = new object[3];
                    back[0] = outlineApi;
                    back[1] = localDepotCombobox.Text;
                    back[2] = localTypeTextBox.Text;
                    outlineBack.RunWorkerAsync(back);// 传入
                    progressBar.Value = 1;
                    progressLabel.Text = "% 开始查找...";
                }
            }
            else
            {
                localSearch2Button.Text = "数据库查找";
                if (outlineBack.IsBusy) outlineBack.CancelAsync();
            }
        }

        /// <summary>
        /// 相似搜索后台开始
        /// </summary>
        private void outlineBack_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            var back = e.Argument as object[];// 接收
            Api api = (Api)back[0];
            string depot = (string)back[1];
            string type = (string)back[2];
            string select = "SELECT Names, Path FROM " + api.Table + " WHERE Names LIKE '%" + type.Replace("'", "''") + "%'";
            DataTable datatable = SqlFunction.Select(depot, select);
            if (datatable == null)
            {
                MessageBox.Show("数据库操作错误，结束搜索", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DataTable result = new DataTable();
            result.Columns.Add("文件名", Type.GetType("System.String"));
            result.Columns.Add("位置", Type.GetType("System.String"));
            foreach (DataRow datarows in datatable.Rows)
            {
                string path = Path.Combine(api.Path, datarows["Path"].ToString());
                string name = Path.GetFileName(path);
                DataRow datarow;
                datarow = result.NewRow();
                datarow["文件名"] = name;
                datarow["位置"] = path;
                result.Rows.Add(datarow);
            }

            e.Result = result;// 传出
        }

        /// <summary>
        /// 相似搜索后台进度
        /// </summary>
        private void outlineBack_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            progressBar.Value = (e.ProgressPercentage < 101) ? e.ProgressPercentage : progressBar.Value;
            progressLabel.Text = progressBar.Value.ToString() + "% 正在查找：" + e.UserState as string;
        }

        /// <summary>
        /// 相似搜索后台完成
        /// </summary>
        private void outlineBack_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            localSearch2Button.Text = "数据库查找";

            if (e.Error != null)
            {
                MessageBox.Show("查找文件错误如下\r\n\r\n" + e.Error.ToString(), "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                progressLabel.Text = "查找文件后台错误";
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show("查找已取消", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                progressLabel.Text = "查找已取消";
                return;
            }

            DataTable datatable = e.Result as DataTable;// 接收

            if (datatable == null)// 空数据
            {
                progressBar.Value = 100;
                MessageBox.Show("查找失败，返回了空结果", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                progressLabel.Text = "查找失败，返回了空结果";
                return;
            }

            // 0结果
            if (datatable.Rows.Count == 0)
            {
                progressBar.Value = 100;
                MessageBox.Show("没有找到相关文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                progressLabel.Text = "没有找到相关文件";
                return;
            }

            // 遍历
            localListView.BeginUpdate();// 挂起UI，避免闪烁并提速
            ListTable.ToView(datatable, localListView);
            localListView.EndUpdate();// 绘制UI。
            localListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);// 自动列宽
            localListView.Items[localListView.Items.Count - 1].EnsureVisible();// 定位尾部

            progressBar.Value = 100;
            progressLabel.Text = "查找完成";
        }

        /// <summary>
        /// 从列表打开按钮
        /// </summary>
        private void localOpenPathButton_Click(object sender, EventArgs e)
        {
            if (localListView.SelectedItems.Count < 1)// 没有选中文件
            {
                MessageBox.Show("没有选中文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (File.Exists(localListView.SelectedItems[0].SubItems[1].Text))// 打开
            {
                System.Diagnostics.ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo("Explorer.exe");
                processStartInfo.Arguments = "/e,/select," + localListView.SelectedItems[0].SubItems[1].Text;
                System.Diagnostics.Process.Start(processStartInfo);
            }
            else MessageBox.Show("文件不存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// 双击列表
        /// </summary>
        private void localListView_DoubleClick(object sender, EventArgs e)
        {
            if (localListView.SelectedItems.Count < 1) return;// 空列表
            if (File.Exists(localListView.SelectedItems[0].SubItems[1].Text)) System.Diagnostics.Process.Start(localListView.SelectedItems[0].SubItems[1].Text);// 打开
            else MessageBox.Show("文件不存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        #endregion 本地搜索

        #region 工具菜单
        /// <summary>
        /// 程序退出菜单
        /// </summary>
        private void closeAppMenu_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
            return;
        }

        /// <summary>
        /// 程序关于菜单
        /// </summary>
        private void aboutAppMenu_Click(object sender, EventArgs e)
        {
            MessageBox.Show("程序名：" + Text + "\r\n版本：" + Application.ProductVersion + "\r\n开发者：Jinzhenting@Aliyun.com\r\n", "关于", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }

        /// <summary>
        /// 程序更新菜单
        /// </summary>
        private void updataAppMenuItem_Click(object sender, EventArgs e)
        {
            AppUpdata.Updata(appUpURL, true);
        }

        /// <summary>
        /// 删除空白文件夹菜单
        /// </summary>
        private void deleteEmptyMenuItem_Click(object sender, EventArgs e)
        {
            EmptyFolder();
        }

        /// <summary>
        /// 删除空白文件夹按钮
        /// </summary>
        private void emptyFolderButton_Click(object sender, EventArgs e)
        {
            EmptyFolder();
        }
        /// <summary>
        /// 删除空白文件夹
        /// </summary>
        private void EmptyFolder()
        {
            EmptyFolderForm emptyfolderform = new EmptyFolderForm();
            emptyfolderform.ShowDialog();
        }

        /// <summary>
        /// 图片整理菜单
        /// </summary>
        private void imageSortMenuItem_Click(object sender, EventArgs e)
        {
            ImageSort();
        }
        /// <summary>
        /// 图片整理按钮
        /// </summary>
        private void imageSortButton_Click(object sender, EventArgs e)
        {
            ImageSort();
        }
        /// <summary>
        /// 图片整理
        /// </summary>
        private void ImageSort()
        {
            ImageSortForm sortform = new ImageSortForm();
            sortform.ShowDialog();
        }

        /// <summary>
        /// 接口配置菜单
        /// </summary>
        private void apiSettingsMenuItem_Click(object sender, EventArgs e)
        {
            ApiSettings();
        }
        /// <summary>
        /// 接口配置按钮
        /// </summary>
        private void apiSettingsButton_Click(object sender, EventArgs e)
        {
            ApiSettings();
        }
        /// <summary>
        /// 接口配置
        /// </summary>
        private void ApiSettings()
        {
            DepotAppSettingsForm apiform = new DepotAppSettingsForm();
            apiform.ShowDialog();
        }

        /// <summary>
        /// 图库入库菜单
        /// </summary>
        private void imageAddMenuItem_Click(object sender, EventArgs e)
        {
            ImageAdd();
        }
        /// <summary>
        /// 图库入库按钮
        /// </summary>
        private void imageAddButton_Click(object sender, EventArgs e)
        {
            ImageAdd();
        }
        /// <summary>
        /// 图库入库
        /// </summary>
        private void ImageAdd()
        {
            ImageAddForm addform = new ImageAddForm();
            addform.ShowDialog();
        }

        /// <summary>
        /// 图库更新菜单
        /// </summary>
        private void imageUpMenuItem_Click(object sender, EventArgs e)
        {
            ImageUp();
        }
        /// <summary>
        /// 图库更新按钮
        /// </summary>
        private void imageUpButton_Click(object sender, EventArgs e)
        {
            ImageUp();
        }
        /// <summary>
        /// 图库更新
        /// </summary>
        private void ImageUp()
        {
            ImageUpForm imageupform = new ImageUpForm();
            imageupform.ShowDialog();
        }

        /// <summary>
        /// 图库删除菜单
        /// </summary>
        private void imageDeleteMenuItem_Click(object sender, EventArgs e)
        {
            ImageDelete();
        }
        /// <summary>
        /// 图库删除按钮
        /// </summary>
        private void imageDeleteButton_Click(object sender, EventArgs e)
        {
            ImageDelete();
        }
        /// <summary>
        /// 图库删除功能
        /// </summary>
        private void ImageDelete()
        {
            ImageDeleteForm deleteform = new ImageDeleteForm();
            deleteform.ShowDialog();
        }

        /// <summary>
        /// 程序帮助菜单
        /// </summary>
        private void helpStripmenu_Click(object sender, EventArgs e)
        {
            Help();
        }
        /// <summary>
        /// 程序帮助按钮
        /// </summary>
        private void helpButton_Click(object sender, EventArgs e)
        {
            Help();
        }
        /// <summary>
        /// 程序帮助功能
        /// </summary>
        private void Help()
        {
            AppHelpForm helpform = new AppHelpForm();
            helpform.ShowDialog();
        }

        /// <summary>
        /// 程序选项菜单
        /// </summary>
        private void settingsStripmenu_Click(object sender, EventArgs e)
        {
            AppSettingsForm AppSettingsForm = new AppSettingsForm();
            AppSettingsForm.AppUpPath = appUpURL;
            AppSettingsForm.ShowDialog();
        }

        /// <summary>
        /// 图片右键菜单定义
        /// </summary>
        private void imageBoxMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (onlinePicturebox.Image == null) imageBoxCopyMenuStrip.Enabled = false;// 复制
            else imageBoxCopyMenuStrip.Enabled = true;

            Image img = Clipboard.GetImage();// 粘贴
            if (img == null) imageBoxPasteMenuStrip.Enabled = false;
            else imageBoxPasteMenuStrip.Enabled = true;

            if (onlinePicturebox.Image == null) imageBoxDeleteMenuStrip.Enabled = false;// 删除
            else imageBoxDeleteMenuStrip.Enabled = true;
        }

        /// <summary>
        /// 图片右键菜单复制
        /// </summary>
        private void imageBoxCopyMenuStrip_Click(object sender, EventArgs e)
        {
            Clipboard.SetImage(onlinePicturebox.Image);
        }

        /// <summary>
        /// 图片右键菜单粘贴
        /// </summary>
        private void imageBoxPasteMenuStrip_Click(object sender, EventArgs e)
        {
            Image img = Clipboard.GetImage();
            if (img != null)
            {
                onlinePicturebox.Image = img;
                onlinePathTextBox.Text = "系统剪贴板";
                progressBar.Value = 0;
                progressLabel.Text = "已选择图片";
            }
            else MessageBox.Show("系统剪贴板没有图像", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }

        /// <summary>
        /// 图片右键菜单删除
        /// </summary>
        private void imageBoxDeleteMenuStrip_Click(object sender, EventArgs e)
        {
            onlinePicturebox.Image = null;
            progressBar.Value = 0;
            onlinePathTextBox.Text = "";
            progressLabel.Text = "等待用户操作";
        }

        #endregion 工具菜单


        #region 本地图片浏览
        
        private List<string> viewList = new List<string>();

        /// <summary>
        /// 异步加载图片
        /// </summary>
        private void viewBack_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            ImageList imageList = new ImageList();// 定义项目列表
            imageList.ImageSize = new Size(256, 256);
            imageList.ColorDepth = ColorDepth.Depth32Bit;
            Api viewlineApi = ApiFunction.GetApi(localDepotCombobox.Text);
            try
            {
                if (viewlineApi != null && Directory.Exists(viewlineApi.Path))
                {
                    DirectoryInfo directoryinfo = new DirectoryInfo(viewlineApi.Path);// 在 viewlineApi.Path 中查找图片
                    DirectoryInfo[] directoryinfos = directoryinfo.GetDirectories();
                    for (int i = 0; i < directoryinfos.Length; i++)// 获取子文件夹列表
                    {
                        if (viewBack.CancellationPending)// 检测后台取消
                        {
                            e.Cancel = true;
                            return;
                        }

                        viewBack.ReportProgress(Percents.Get(i + 1, directoryinfos.Length), directoryinfos[i].FullName);//进度
                    
                        DirectoryInfo[] directoryInfo2= directoryinfos[i].GetDirectories();// 获取每个子文件夹的子文件夹列表
                        FileInfo[] fileInfos = new DirectoryInfo(directoryInfo2[0].FullName).GetFiles(directoryinfos[i].Name + "*.bmp");// 在2层子文件夹的第一个文件夹中搜索Jpg
                        if (fileInfos.Length > 0)
                        {
                            imageList.Images.Add(ZoomImage(Image.FromFile(fileInfos[0].FullName), 256, 256));// 等比缩放图片// 加载第一张Jpg到缩略图容器
                            viewList.Add(fileInfos[0].FullName);// 加载图片名到集
                        }
                        else MessageBox.Show("文件夹 " + directoryinfos[i].FullName + " 中没有查找到Jpg", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("文件夹 " + viewlineApi.Path + " 访问失败", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    viewBack.ReportProgress(1, "文件夹 " + viewlineApi.Path + " 访问失败");
                    e.Cancel = true;
                    return;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("缩略图载入错误，描述如下\r\n\r\n" + ex, "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            e.Result = imageList;
        }

        /// <summary>
        /// 等比缩放
        /// </summary>
        /// <param name="inImage">输入图片</param>
        /// <param name="OutHeight">输出高</param>
        /// <param name="OutWidth">输出宽</param>
        /// <returns>输出图片</returns>
        private Image ZoomImage(Image inImage, int OutHeight, int OutWidth)
        {
            try
            {
                int width = 0, height = 0;
                // 按比例缩放
                int inWidth = inImage.Width;
                int inHeight = inImage.Height;
                if (inHeight > OutHeight || inWidth > OutWidth)
                {
                    if ((inWidth * OutHeight) > (inHeight * OutWidth))
                    {
                        width = OutWidth;
                        height = (OutWidth * inHeight) / inWidth;
                    }
                    else
                    {
                        height = OutHeight;
                        width = (inWidth * OutHeight) / inHeight;
                    }
                }
                else
                {
                    width = inWidth;
                    height = inHeight;
                }
                Bitmap outBitmap = new Bitmap(OutWidth, OutHeight);
                Graphics graphics = Graphics.FromImage(outBitmap);
                graphics.Clear(Color.Transparent);
                // 设置画布的描绘质量           
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(inImage, new Rectangle((OutWidth - width) / 2, (OutHeight - height) / 2, width, height), 0, 0, inImage.Width, inImage.Height, GraphicsUnit.Pixel);
                graphics.Dispose();
                // 设置压缩质量       
                EncoderParameters encoderParams = new EncoderParameters();
                long[] quality = new long[1];
                quality[0] = 100;
                EncoderParameter encoderParam = new EncoderParameter(Encoder.Quality, quality);
                encoderParams.Param[0] = encoderParam;
                inImage.Dispose();
                return outBitmap;
            }
            catch
            {
                return inImage;
            }
        }

        /// <summary>
        /// 异步加载图片进度
        /// </summary>
        private void viewBack_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            progressLabel.Text = progressBar.Value + "% 载入缩略图" + e.UserState as string;
        }

        /// <summary>
        /// 异步加载图片完成
        /// </summary>
        private void viewBack_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show("缩略图载入错误如下\r\n\r\n" + e.Error.ToString(), "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                progressBar.Value = 0;
                return;
            }

            if (e.Cancelled)
            {
                progressLabel.Text = "已取消载入缩略图";
                progressBar.Value = 0;
                return;
            }

            progressLabel.Text = "列表载入完成";
            progressBar.Value = 100;

            ImageList imagelist = (ImageList)e.Result;
            for (int i = 0; i < viewList.Count; i++)// 把项目名遍历到ListView
            {
                ListViewItem ListViewitem = new ListViewItem();// 定义单个项目
                ListViewitem.ImageIndex = i;
                ListViewitem.Text = Path.GetFileNameWithoutExtension(viewList[i]);
                viewListView.Items.Add(ListViewitem);
            }
            viewListView.LargeImageList = imagelist;
        }

        /// <summary>
        /// 载入或刷新本地图片预览图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void viewReButton_Click(object sender, EventArgs e)
        {
            if (viewComboBox.Text == "背景墙" && !viewBack.IsBusy)
            {
                if (viewListView == null || viewListView.Items.Count > 0) viewListView.Items.Clear();
                viewBack.RunWorkerAsync();
            }
            else MessageBox.Show("此图库暂不支持本地浏览", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// 本地图片浏览打开文件夹
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void viewOpenFolderButton_Click(object sender, EventArgs e)
        {
            if (viewListView.SelectedItems.Count < 1)
            {
                MessageBox.Show("没有选中图片", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            string imagePath = viewList[viewListView.SelectedItems[0].Index];
            if (File.Exists(imagePath))
            {
                ProcessStartInfo psi = new ProcessStartInfo("Explorer.exe");
                psi.Arguments = "/e,/select," + imagePath;
                Process.Start(psi);
            }
            else MessageBox.Show("图片" + imagePath + "不存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// 双击打开本地图片浏览列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void viewListView_DoubleClick(object sender, EventArgs e)
        {
            OpenView();
        }

        /// <summary>
        /// 打开本地图片浏览列表按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void viewOpenFileButton_Click(object sender, EventArgs e)
        {
            OpenView();
        }

        /// <summary>
        /// 打开本地图片浏览列表图片
        /// </summary>
        private void OpenView()
        {
            if (viewListView.SelectedItems.Count < 1) return;
            string imagePath = viewList[viewListView.SelectedItems[0].Index].Replace(".bmp", ".jpg");
            if (File.Exists(imagePath)) Process.Start(imagePath);
            else MessageBox.Show("图片" + imagePath + "不存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        #endregion 本地图片浏览

        //
    }
}
