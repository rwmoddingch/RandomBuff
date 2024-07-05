using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text.RegularExpressions;

using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace MultiLayerHelper
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            listView1.View = View.LargeIcon;
            listView1.LargeImageList = new ImageList();
            listView1.LargeImageList.ImageSize = new Size(60, 100);
            listView1.Columns.Add("Depth Column", -2, HorizontalAlignment.Left);

        }

        private void button1_Click(object sender, EventArgs e)
        {
            FileDialog dialog = new OpenFileDialog()
            {
                Multiselect = false,
                Filter = "图片文件|*.png"
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                listView1.BeginUpdate();
                ListViewItem item = new ListViewItem();
                listView1.LargeImageList.Images.Add(Image.FromFile(dialog.FileName));
                item.ImageIndex = listView1.LargeImageList.Images.Count - 1;

                var depth = PopWindow();
                item.Text = $"Depth:{depth}";
                item.Name = dialog.FileName;
                item.Tag = depth;

                int index = 0;
                for (; index < listView1.Items.Count; index++)
                    if ((float)(listView1.Items[index].Tag) < depth)
                        break;

                listView1.Items.Insert(index, item);
                listView1.EndUpdate();
            }
        }
        public static Bitmap ScaleToSize(Bitmap bitmap, int width, int height)
        {
            if (bitmap.Width == width && bitmap.Height == height)
            {
                return bitmap;
            }

            var scaledBitmap = new Bitmap(width, height);
            using (var g = Graphics.FromImage(scaledBitmap))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(bitmap, 0, 0, width, height);
            }

            return scaledBitmap;
        }
        private void button1_DragDrop(object sender, DragEventArgs e)
        {
            if (e?.Data?.GetData(DataFormats.FileDrop) is Array data && data.GetValue(0) is { } value)
            {
                string path = value.ToString();
                if (path.Split('.')[^1].ToLower() == "png")
                {
                    listView1.BeginUpdate();
                    ListViewItem item = new ListViewItem();
                    listView1.LargeImageList.Images.Add(new Bitmap(path));
                    item.ImageIndex = listView1.LargeImageList.Images.Count - 1;

                    var depth = PopWindow();
                    item.Text = $"Depth:{depth}";
                    item.Name = path;
                    item.Tag = depth;


                    int index = 0;
                    for (; index < listView1.Items.Count; index++)
                        if ((float)(listView1.Items[index].Tag) < depth)
                            break;
                    listView1.Items.Insert(index, item);
                    listView1.EndUpdate();
                }
            }
        }

        private float PopWindow()
        {
            PopForm form = new PopForm();
            form.ShowDialog(this);
            return form.depth;
        }

        private void button1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.All;
            else
                e.Effect = DragDropEffects.None;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FileDialog dialog = new SaveFileDialog()
            {
                Filter = "图片文件|*.png"
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {

                var width = int.Parse(textBox1.Text);
                var height = width * 5 / 3;
                Bitmap image = new Bitmap(width * listView1.Items.Count, height * 2);
                for (int i = 0; i < listView1.Items.Count; i++)
                {
                    var resizeImg = ScaleToSize(new Bitmap(listView1.Items[i].Name), width, height);
                    var color = Color.FromArgb((int)((float)listView1.Items[i].Tag * 255), 0, 0);
                    for (int x = 0; x < width; x++)
                        for (int y = 0; y < height; y++)
                        {
                            image.SetPixel(i * width + x, y, resizeImg.GetPixel(x, y));
                            image.SetPixel(i * width + x, height + y, color);

                        }
                }

                image.Save(dialog.FileName);
                Process p = new Process();
                p.StartInfo.FileName = "explorer.exe";
                p.StartInfo.Arguments = $@" /select, {dialog.FileName}";
                p.Start();
            }
        }

        private void textBox1_Validated(object sender, EventArgs e)
        {
            const string pattern = @"^\d{1,5}$";

            string content = ((TextBox)sender).Text;

            if (!(Regex.IsMatch(content, pattern)))
                textBox1.Text = "300";
        }
    }
}