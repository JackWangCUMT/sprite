﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;


namespace WindowsFormsApplication1
{
    enum SpriteType
    {
        Vertical,
        Horizontal
    }

    public partial class sprite : Form
    {

        const string HTML_TEMPLATE = @"
<!doctype html>
<html lang=""en"">
<head>
	<meta charset=""UTF-8"">
	<title>{2}</title>
	<style>
		.{2} {{ background: url({2}.png); display: block; }}
		{0}
	</style>
</head>
<body>
	{1}
</body>
</html>";
        const string ALL_HTML_TEMPLATE = @"
<!doctype html>
<html lang=""en"">
<head>
	<meta charset=""UTF-8"">
	<title>All in one</title>
	<style>
		{0}
	</style>
</head>
<body>
	{1}
</body>
</html>";
        const string IMAGE_STILE_V = "\t\t{0} {{ width: {1}px; height: {2}px; background-position: 0 {3}px; }}{4}";
        const string IMAGE_STILE_H = "\t\t{0} {{ width: {1}px; height: {2}px; background-position: {3}px 0; }}{4}";
        const string IMAGE_CONTAINER = "\t<a class=\"{2} {0}\"></a>{1}";
        const string COMPRESS_CMD = "optipng.exe sprite_uncompressed.png -clobber -quiet -out sprite.png";
        const string COMPRESS_FOLDER_CMD = "optipng.exe {0} -clobber -quiet -dir {1}";
        const string IMAGE_FILE = "sprite";
        const string IMAGE_NAME = "sprite.png";
        const string IAMGE_BAK = "sprite.png.bak";
        const string IMAGE_UNCOMPRESSED_NAME = "sprite_uncompressed.png";
        const string COMPRESS_FOLDER = "compressed";

        static List<string> pseudoClassList;
        static string path = AppDomain.CurrentDomain.BaseDirectory + "Build-Sprite";

        StringBuilder sbForStyle;
        StringBuilder sbForHtml;

        public sprite()
        {
            InitializeComponent();

            this.MaximizeBox = false;
            this.MinimizeBox = false;

            pseudoClassList = new List<string>();
            pseudoClassList.Add("link");
            pseudoClassList.Add("visited");
            pseudoClassList.Add("hover");
            pseudoClassList.Add("active");
            pseudoClassList.Add("focus");
            pseudoClassList.Add("before");
            pseudoClassList.Add("after");
            pseudoClassList.Add("lang");

            #region name rule test
            /*
            string s = GetClassName("wechat"); // wechat
            string s1 = GetClassName("wechat_hover"); // wechat:hover
            string s2 = GetClassName("wechat-hover"); // wechat:hover
            string s3 = GetClassName("ibg_wechat_hover"); // ibg_wechat:hover
            string s4 = GetClassName("ibg-wechat-hover"); // ibg-wechat:hover

            string s5 = GetClassName("wechat.ibg"); // .wechat .ibg
            string s6 = GetClassName("wechat.ibg.wechat_focus"); // .wechat .ibg .wechat:focus
            string s7 = GetClassName("wechat.ibg.wechat_none"); // .wechat .ibg . wechat_none
            string s8 = GetClassName("wechat_hover.wechat_none"); // .wechat:hover .wechat_none
            string s9 = GetClassName("wechat-hover.wechat-none"); // .wechat:hover .wechat-none
            */
            #endregion
        }

        private bool MakeDirectory(List<string> directories, string imagePath)
        {
            string rootDirectory = Path.GetDirectoryName(imagePath);
            if (!rootDirectory.EndsWith(@"\"))
            {
                rootDirectory += @"\";
            }

            try
            {
                foreach (string directory in directories)
                {
                    string dir = directory.Replace(rootDirectory, string.Empty);
                    string newDir = Path.Combine(path, dir);

                    Directory.CreateDirectory(newDir);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            string imagePath = txtPath.Text.Trim();
            if (!string.IsNullOrEmpty(imagePath))
            {
                if (Directory.Exists(path))
                {
                    DirectoryInfo di = new DirectoryInfo(path);
                    di.Delete(true);
                }

                SpriteType type = rbtnVertical.Checked ? SpriteType.Vertical : SpriteType.Horizontal;
                if (!chkAll.Checked)
                {
                    // combime images by folder
                    sbForStyle = new StringBuilder();
                    sbForHtml = new StringBuilder();

                    List<string> dirList = new List<string>();
                    dirList.Add(imagePath);
                    string[] directories = Directory.GetDirectories(imagePath, "*", SearchOption.AllDirectories);

                    foreach (string directory in directories)
                    {
                        dirList.Add(directory);
                    }

                    if (MakeDirectory(dirList, imagePath))
                    {
                        CombineBitmapByFolder(dirList, type, imagePath);
                    }
                }
                else
                {
                    // combine images all in one
                    string[] images = Directory.GetFiles(imagePath, "*.png", SearchOption.AllDirectories);
                    switch (images.Length)
                    {
                        case 0:
                            MessageBox.Show(string.Format("There is none of a png iamge in {0}, please change a folder or add png images here.", imagePath), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            break;
                        case 1:
                            MessageBox.Show(string.Format("There is only one png file in {0}, it doesn't need to combime.", imagePath), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            break;
                        default:
                            bool compressed = chkCompress.Checked;
                            string imageName = compressed ? IMAGE_UNCOMPRESSED_NAME : IMAGE_NAME;
                            Bitmap mergedImage = CombineBitmap(images, type, IMAGE_FILE, path);
                            mergedImage.Save(string.Format("{0}\\{1}", path, imageName));
                            if (compressed)
                            {
                                CompressImgae();
                            }
                            MessageBox.Show("The sprite image and style have been generated successfully! :)", "successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            break;
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select the image path via clicking the \"Browser...\" button", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CombineBitmapByFolder(List<string> directories, SpriteType spriteType, string imagePath)
        {
            foreach (string directory in directories)
            {
                string[] images = Directory.GetFiles(directory, "*.png", SearchOption.TopDirectoryOnly);
                if (images.Length > 1)
                {

                    bool compressed = chkCompress.Checked;
                    string imageName;
                    string temp;
                    imageName = temp = Path.GetFileName(directory);
                    string imageUncompressedName = string.Format("{0}_uncompressed.png", imageName); ;
                    imageName = compressed ? imageUncompressedName : string.Format("{0}.png", imageName);

                    string rootDirectory = Path.GetDirectoryName(imagePath);
                    if (!rootDirectory.EndsWith(@"\"))
                    {
                        rootDirectory += @"\";
                    }
                    string newDir = Path.Combine(path, directory.Replace(rootDirectory, string.Empty));
                    Bitmap mergedImage = CombineBitmap(images, spriteType, temp, newDir);
                    mergedImage.Save(string.Format("{0}\\{1}", newDir, imageName));
                    mergedImage.Save(string.Format("{0}\\{1}", path, imageName));
                    if (compressed)
                    {
                        CompressImgae();
                    }
                }
            }

            ConvertToOne(imagePath);

            MessageBox.Show("The sprite image and style have been generated successfully! :)", "successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ConvertToOne(string imagePath)
        {
            string name = Path.GetDirectoryName(imagePath);
            string currentPath = Path.Combine(path, name);

            string filePath = string.Format("{0}\\allInOne.html", path);

            StreamWriter sw = new StreamWriter(filePath, false, Encoding.UTF8);
            sw.Write(string.Format(ALL_HTML_TEMPLATE, sbForStyle.ToString(), sbForHtml.ToString()));
            sw.Close();
            sw.Dispose();
        }

        private Bitmap CombineBitmap(string[] files, SpriteType spriteType, string imageName, string directory)
        {
            //read all images into memory
            List<Bitmap> images = new List<Bitmap>();
            Bitmap finalImage = null;

            try
            {
                int width = 0;
                int height = 0;
                StringBuilder imageStyle = new StringBuilder();
                StringBuilder imageContainer = new StringBuilder();

                foreach (string image in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(image);
                    string className = GetClassName(fileName);

                    // process the filename lick "en.wechat" to "wechat"
                    fileName = fileName.Substring(fileName.LastIndexOf('.') + 1);

                    //create a Bitmap from the file and add it to the list
                    Bitmap bitmap = new Bitmap(image);

                    //update the size of the final bitmap
                    if (spriteType == SpriteType.Horizontal)
                    {
                        string style = string.Format(IMAGE_STILE_H, className, bitmap.Width, bitmap.Height, (0 - width), Environment.NewLine);
                        imageStyle.Append(style);

                        if (className.IndexOf(":") == -1)
                        {
                            string container = string.Format(IMAGE_CONTAINER, fileName, Environment.NewLine, imageName);
                            imageContainer.Append(container);
                        }

                        width += bitmap.Width;
                        height = bitmap.Height > height ? bitmap.Height : height;
                    }
                    else
                    {
                        string style = string.Format(IMAGE_STILE_V, className, bitmap.Width, bitmap.Height, (0 - height), Environment.NewLine);
                        imageStyle.Append(style);

                        if (className.IndexOf(":") == -1)
                        {
                            string container = string.Format(IMAGE_CONTAINER, fileName, Environment.NewLine, imageName);
                            imageContainer.Append(container);
                        }

                        height += bitmap.Height;
                        width = bitmap.Width > width ? bitmap.Width : width;
                    }

                    images.Add(bitmap);
                }

                //create a bitmap to hold the combined image
                finalImage = new Bitmap(width, height);

                //get a graphics object from the image so we can draw on it
                using (Graphics g = Graphics.FromImage(finalImage))
                {
                    //set background color
                    g.Clear(Color.Transparent);

                    //go through each image and draw it on the final image
                    int offset = 0;
                    foreach (Bitmap image in images)
                    {
                        if (spriteType == SpriteType.Horizontal)
                        {
                            g.DrawImage(image,
                              new Rectangle(offset, 0, image.Width, image.Height));
                            offset += image.Width;
                        }
                        else
                        {
                            g.DrawImage(image,
                              new Rectangle(0, offset, image.Width, image.Height));
                            offset += image.Height;
                        }
                    }
                }

                string styleStr = SortStyle(imageStyle.ToString());
                string htmlStr = imageContainer.ToString();
                WriteFile(styleStr, htmlStr, imageName, directory);

                if (!chkAll.Checked)
                {
                    sbForStyle.AppendLine(string.Format("\t.{0} {{ background: url({0}.png); display: block; }}", imageName));
                    sbForStyle.AppendLine(styleStr);
                    sbForHtml.AppendLine(htmlStr);
                }

                return finalImage;
            }
            catch (Exception ex)
            {
                if (finalImage != null)
                    finalImage.Dispose();

                throw ex;
            }
            finally
            {
                //clean up memory
                foreach (Bitmap image in images)
                {
                    image.Dispose();
                }
            }
        }

        private static string SortStyle(string style)
        {
            string result = style;
            if (style.IndexOf(":active") > -1 && style.IndexOf(":hover") > -1)
            {
                Regex regex = new Regex(@"(\t\t.*:(?:active|hover).*\r\n)", RegexOptions.Multiline);
                MatchCollection matches = regex.Matches(result);
                int index = 0;
                List<string> list = new List<string>();

                foreach (Match match in matches)
                {
                    // .appmsg:active { width: 50px; height: 56px; background-position: 0 -56px; }
                    // .appmsg:hover { width: 50px; height: 56px; background-position: 0 -112px; }
                    string temp = match.Groups[0].Value;
                    Match m = Regex.Match(temp, @"\t\t(.*):(active|hover).*");
                    if (m.Success)
                    {
                        if (m.Groups[2].Value == "active")
                        {
                            // active
                            string nextRow = matches[index + 1].Groups[0].Value;
                            if (nextRow.IndexOf(string.Format("{0}:hover", m.Groups[1])) > -1)
                            {
                                list.Add(temp);
                                list.Add(nextRow);
                            }
                        }
                    }

                    index++;
                }

                int length = list.Count;
                for (index = 0; index < length; index++)
                {
                    if (index % 2 == 0)
                    {
                        // in active mode
                        result = result.Replace(list[index + 1], list[index].Replace("active","wuqiang"));
                        // active => hover
                        result = result.Replace(list[index], list[index + 1]);                        
                    }
                }

                result = result.Replace("wuqiang", "active");
            }

            return result;
        }

        private static void CompressImgae()
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WorkingDirectory = path;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/c" + COMPRESS_CMD;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo = startInfo;

            try
            {
                if (process.Start())
                {
                    process.WaitForExit();
                }
            }
            catch { }
            finally
            {
                if (process != null)
                {
                    process.Close();
                    string imageBak = string.Format("{0}/{1}", path, IAMGE_BAK);
                    if (File.Exists(imageBak))
                    {
                        File.Delete(imageBak);
                    }
                }
            }
        }

        private static void CompressImgae(string fileName)
        {
            string cmd = string.Format(COMPRESS_FOLDER_CMD, fileName, COMPRESS_FOLDER);
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WorkingDirectory = path;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/c" + cmd;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo = startInfo;

            try
            {
                if (process.Start())
                {
                    process.WaitForExit();
                }
            }
            catch { }
            finally
            {
                if (process != null)
                {
                    process.Close();
                }
            }
        }

        private static string GetClassName(string fileName)
        {
            string[] cascading = fileName.Split('.');
            int length = cascading.Length;
            StringBuilder className = new StringBuilder();

            for (int index = 0; index < length; index++)
            {
                className.Append(" .");
                string temp = cascading[index];
                int indexConnector = temp.LastIndexOf('-');
                int indexUnderline = temp.LastIndexOf('_');

                if (indexConnector == -1 && indexUnderline == -1)
                {
                    // filename e.g: wechat
                    // return        wechat
                    //return temp;
                    className.Append(temp);
                    continue;
                }

                string name = ParseFileName(temp, Math.Max(indexConnector, indexUnderline));
                if (string.IsNullOrEmpty(name))
                {
                    //return ParseFileName(temp, indexUnderline);
                    className.Append(ParseFileName(temp, indexUnderline));
                    continue;
                }
                else
                {
                    //return name;
                    className.Append(name);
                }

            }

            return className.ToString().TrimStart(' ');
        }

        private static string ParseFileName(string name, int index)
        {
            string pseudo;
            if (index > -1)
            {
                pseudo = name.Substring(index + 1);
                if (pseudoClassList.Contains(pseudo))
                {
                    // filename e.g:  wechat-hover 
                    //                wechat_hover  
                    // return        .wechat:hover
                    return string.Format("{0}:{1}", name.Substring(0, index), pseudo);
                }

                return name;
            }
            return string.Empty;
        }

        private static void WriteFile(string style, string container, string imageName, string directory)
        {
            string filePath = string.Format("{0}\\{1}.html", directory, imageName);

            StreamWriter sw = new StreamWriter(filePath, false, Encoding.UTF8);
            sw.Write(string.Format(HTML_TEMPLATE, style, container, imageName));
            sw.Close();
            sw.Dispose();
        }

        private void btnBrowser_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = fbdDialog.ShowDialog();
            if (dialogResult.ToString().ToLower() == "ok")
            {
                txtPath.Text = fbdDialog.SelectedPath;
            }
        }

        private void llMail_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("Mailto:112055730@qq.com");
        }

        private void sprite_HelpButtonClicked(object sender, CancelEventArgs e)
        {
            MessageBox.Show(@"Here are the rules of image naming:

wechat => .wechat
wechat_hover => .wechat:hover
wechat-hover => .wechat:hover
wechat_ibg_hover => .wechat_ibg:hover
wechat-ibg-hover => .wechat-ibg:hover
wechat.ibg => .wechat .ibg
wechat.ibg.wechat_focus => .wechat .ibg .wechat:focus
wechat.ibg.wechat_none => .wechat .ibg . wechat_none
wechat_hover.wechat_none => .wechat:hover .wechat_none
wechat-hover.wechat-none => .wechat:hover .wechat-none

For more information, please visit: https://github.com/wuqiang1985/sprite
",
 "image naming rule", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnCompress_Click(object sender, EventArgs e)
        {
            string imagePath = txtPath.Text.Trim();

            if (!string.IsNullOrEmpty(imagePath))
            {
                string[] images = Directory.GetFiles(imagePath, "*.png", SearchOption.AllDirectories);

                switch (images.Length)
                {
                    case 0:
                        MessageBox.Show(string.Format("There is none of a png iamge in {0}, please change a folder or add png images here.", imagePath), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;
                    default:
                        foreach (string fileName in images)
                        {
                            CompressImgae(fileName);
                        }
                        MessageBox.Show("The images have beed compressed successfully! :)", "successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                }
            }
            else
            {
                MessageBox.Show("Please select the image path via clicking the \"Browser...\" button", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
