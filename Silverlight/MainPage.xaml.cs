using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Silverlight
{
    public partial class MainPage : UserControl
    {
        private V6FS.Entry root;

        public MainPage()
        {
            InitializeComponent();
            addImage("folder");
            addImage("file");
            addImage("executable");
            addImage("text");
        }

        private Dictionary<string, BitmapImage> images =
            new Dictionary<string, BitmapImage>();

        private void addImage(string name)
        {
            var img = new BitmapImage();
            img.SetSource(Utils.getResourceStream(name + ".png"));
            images.Add(name, img);
        }

        private TreeViewItem createNode(string icon, string text, object tag)
        {
            var st = new StackPanel { Orientation = Orientation.Horizontal };
            var img = new Image
            {
                Source = images[icon],
                Margin = new Thickness(0, 0, 4, 0)
            };
            st.Children.Add(img);
            st.Children.Add(new TextBlock { Text = text });
            return new TreeViewItem { Header = st, Tag = tag };
        }

        private Dictionary<string, TreeViewItem> dirdic =
            new Dictionary<string, TreeViewItem>();

        private TreeViewItem dirTree(V6FS.Entry dir, TreeViewItem n)
        {
            var items = n != null ? n.Items : treeView1.Items;
            var nn = createNode(dir.Icon, dir.Name, dir);
            items.Add(nn);
            dirdic.Add(dir.FullName, nn);
            foreach (var e in dir.Children)
            {
                if (e.INode.IsDir) dirTree(e, nn);
            }
            return nn;
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != true) return;

            var sw = new StringWriter();
#if !DEBUG
            try
#endif
            {
                using (var fs = ofd.File.OpenRead())
                    root = V6FS.Open(fs);

                dirdic.Clear();
                treeView1.Items.Clear();
                var nroot = dirTree(root, null);
                nroot.IsExpanded = true;
                root.FileSystem.Write(sw);
                Action<V6FS.Entry> dir = null;
                dir = ent =>
                {
                    sw.WriteLine();
                    ent.Write(sw);
                    foreach (var child in ent.Children)
                        dir(child);
                };
                dir(root);
                btnSaveZip.IsEnabled = true;
            }
#if !DEBUG
            catch (Exception ex)
            {
                sw.WriteLine(ex.ToString());
                btnSaveZip.IsEnabled = false;
                root = null;
            }
#endif
            textBox1.Text = sw.ToString();
        }

        private void btnSaveZip_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.Filter = "Zip ファイル (*.zip)|*.zip|すべてのファイル (*.*)|*.*";
            if (sfd.ShowDialog() != true) return;

#if !DEBUG
            try
#endif
            {
                using (var fs = sfd.OpenFile())
                    V6FS.SaveZip(fs, root);
            }
#if !DEBUG
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
#endif
        }
    }
}
