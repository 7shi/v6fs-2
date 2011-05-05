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
        private static Dictionary<string, ImageSource> images =
            new Dictionary<string, ImageSource>();

        private static void addImage(string name)
        {
            var img = new BitmapImage();
            img.SetSource(Utils.getResourceStream(name + ".png"));
            images.Add(name, img);
        }

        static MainPage()
        {
            addImage("folder");
            addImage("file");
            addImage("executable");
            addImage("text");
        }

        private V6FS.Entry root;

        public MainPage()
        {
            InitializeComponent();
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

        public class ListEntry
        {
            public ImageSource Icon { get; set; }
            public string Name { get; set; }
            public string Size { get { return string.Format("{0:#,#}", size); } }
            private int size;
            private V6FS.Entry Entry;

            public ListEntry(V6FS.Entry e)
            {
                Entry = e;
                Icon = images[e.Icon];
                Name = e.Name;
                size = e.INode.Length;
            }
        }

        private void dirList(V6FS.Entry dir)
        {
            var list = new List<ListEntry>();
            foreach (var e in dir.Children)
                list.Add(new ListEntry(e));
            dataGrid1.ItemsSource = list;
        }

        private void dirINodes(TextWriter tw, V6FS.Entry e)
        {
            tw.WriteLine();
            e.Write(tw);
            foreach (var child in e.Children)
                dirINodes(tw, child);
        }

        private void treeView1_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var it = e.NewValue as TreeViewItem;
            if (it == null) return;

            var ent = it.Tag as V6FS.Entry;
            if (ent == null) return;

            dirList(ent);
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
                nroot.IsSelected = true;
                dirList(root);
                root.FileSystem.Write(sw);
                dirINodes(sw, root);
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
