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
            var uri = new Uri("/V6FS;component/" + name + ".png", UriKind.Relative);
            var s = Application.GetResourceStream(uri);
            var img = new BitmapImage();
            img.SetSource(s.Stream);
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
        private Command cmdSaveImage, cmdSaveFile, cmdSaveDir;

        public MainPage()
        {
            InitializeComponent();

            var canvas = cmenuFile.Parent as Canvas;
            canvas.Children.Remove(cmenuFile);

            cmdSaveImage = new Command(saveImage);
            cmdSaveFile = new Command(saveFile);
            cmdSaveDir = new Command(saveDir);
            menuFileSaveImage.Command = cmdSaveImage;
            menuFileSaveFile.Command = menuSaveFile.Command = cmdSaveFile;
            menuFileSaveDir.Command = menuSaveDir1.Command = menuSaveDir2.Command = cmdSaveDir;
        }

        private Point mousePos;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            mousePos = e.GetPosition(null);
            base.OnMouseMove(e);
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
            private int size;
            public string Size { get { return string.Format("{0:#,#}", size); } }
            public V6FS.Entry Entry;

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

        private V6FS.Entry target;

        private void showInfo(V6FS.Entry e)
        {
            target = e;
            var sw = new StringWriter();
            if (e.INode.inode == 1)
            {
                e.FileSystem.Write(sw);
                sw.WriteLine();
            }
            e.Write(sw);
            textBox1.Text = sw.ToString();
            var bytes = V6FS.readAllBytes(e.INode);
            if (e.Icon == "text")
                textBox2.Text = Utils.getText(bytes);
            else
                textBox2.Text = Utils.getHexDump(bytes);
            var isDir = e.INode.IsDir;
            cmdSaveFile.CanExecute(!isDir);
            cmdSaveDir.CanExecute(isDir);
        }

        private void treeView1_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var it = e.NewValue as TreeViewItem;
            if (it == null) return;

            var ent = it.Tag as V6FS.Entry;
            if (ent == null) return;

            dirList(ent);
            showInfo(ent);
        }

        private void dataGrid1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var items = e.AddedItems;
            if (items.Count == 0) return;

            var it = items[0] as ListEntry;
            if (it == null) return;

            showInfo(it.Entry);
        }

        private T getElement<T>(Point p, UIElement e) where T : UIElement
        {
            var elems = VisualTreeHelper.FindElementsInHostCoordinates(p, e);
            var it = (from i in elems where i is T select i as T).GetEnumerator();
            return it.MoveNext() ? it.Current : null;
        }

        private void treeView1_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var it = getElement<TreeViewItem>(e.GetPosition(null), treeView1);
            if (it != null) it.IsSelected = true;
        }

        private void dataGrid1_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var it = getElement<DataGridRow>(e.GetPosition(null), dataGrid1);
            if (it != null) dataGrid1.SelectedIndex = it.GetIndex();
        }

        private void menuFile_Click(object sender, RoutedEventArgs e)
        {
            var trans = menuFile.TransformToVisual(this);
            var p = trans.Transform(new Point(0, menuFile.ActualHeight));
            cmenuFile.HorizontalOffset = p.X - mousePos.X;
            cmenuFile.VerticalOffset = p.Y - mousePos.Y;
            cmenuFile.IsOpen = !cmenuFile.IsOpen;
        }

        private void menuFileOpen_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != true) return;

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
                showInfo(root);
                cmdSaveImage.CanExecute(true);
            }
#if !DEBUG
            catch (Exception ex)
            {
                textBox1.Text = ex.ToString();
                textBox2.Text = "";
                cmdSaveImage.CanExecute(false);
                root = null;
            }
#endif
        }

        private void saveZip(V6FS.Entry e)
        {
            var sfd = new SaveFileDialog();
            sfd.Filter = "Zip ファイル (*.zip)|*.zip|すべてのファイル (*.*)|*.*";
            if (sfd.ShowDialog() != true) return;

#if !DEBUG
            try
#endif
            {
                using (var fs = sfd.OpenFile())
                    V6FS.SaveZip(fs, e);
            }
#if !DEBUG
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
#endif
        }

        private void saveImage()
        {
            saveZip(root);
        }

        private void saveFile()
        {
            if (target == null) return;

            var sfd = new SaveFileDialog();
            sfd.Filter = "すべてのファイル (*.*)|*.*";
            if (sfd.ShowDialog() != true) return;

#if !DEBUG
            try
#endif
            {
                var bytes = V6FS.readAllBytes(target.INode);
                using (var fs = sfd.OpenFile())
                    fs.Write(bytes, 0, bytes.Length);
            }
#if !DEBUG
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
#endif
        }

        private void saveDir()
        {
            if (target != null) saveZip(target);
        }
    }
}
