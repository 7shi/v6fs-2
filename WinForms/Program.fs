open System
open System.Drawing
open System.IO
open System.Windows.Forms
open Utils
open V6FS

Application.EnableVisualStyles()
Application.SetCompatibleTextRenderingDefault(false)

let getResourceBitmap name =
    use s = getResourceStream(name)
    new Bitmap(s)

let icons = new ImageList()
for png in ["folder"; "file"; "executable"; "text"] do
    icons.Images.Add(png, getResourceBitmap(png + ".png"))

let menu = new MainMenu()
let miFile = new MenuItem("&File")
let miFileOpen = new MenuItem("&Open")
let miFileSaveZip = new MenuItem(Text = "Save as &Zip", Enabled = false)
let miFileExit = new MenuItem("E&xit")
miFile.MenuItems.AddRange([|miFileOpen; miFileSaveZip; new MenuItem("-"); miFileExit|])
menu.MenuItems.Add(miFile) |> ignore

let f = new Form(Text = "V6FS", Menu = menu, Width = 640, Height = 400)
let mono = new Font(FontFamily.GenericMonospace, Control.DefaultFont.Size)
let split1 = new SplitContainer(Dock = DockStyle.Fill)
let split2 = new SplitContainer(Dock = DockStyle.Fill)
split1.Panel2.Controls.Add(split2)
let textBox1 = new TextBox(Dock = DockStyle.Fill,
                           HideSelection = false,
                           WordWrap = false,
                           Multiline = true,
                           Font = mono,
                           ScrollBars = ScrollBars.Both)
split2.Panel2.Controls.Add(textBox1)
let treeView1 = new TreeView(Dock = DockStyle.Fill,
                             HideSelection = false,
                             ShowRootLines = false,
                             ImageList = icons)
split1.Panel1.Controls.Add(treeView1)
let listView1 = new ListView(Dock = DockStyle.Fill,
                             HideSelection = false,
                             FullRowSelect = true,
                             View = View.Details,
                             SmallImageList = icons)
let clmName = new ColumnHeader(Text = "Name", Width = 100)
let clmSize = new ColumnHeader(Text = "Size", Width = 60,
                               TextAlign = HorizontalAlignment.Right)
listView1.Columns.AddRange([|clmName; clmSize|])
split2.Panel1.Controls.Add(listView1)
f.Controls.Add(split1)
split1.SplitterDistance <- 140
split2.SplitterDistance <- 200

let rec treeDir (dir:Entry) (n:TreeNode) =
    let nn = if n <> null then n.Nodes else treeView1.Nodes
    let n = nn.Add(dir.Icon, dir.Name)
    n.Tag <- dir
    for e in dir.children do
        if e.INode.IsDir then
            treeDir e n |> ignore
    n

let listDir(dir:Entry) =
    listView1.Items.Clear()
    for e in dir.Children do
        listView1.Items.Add(e.Name, e.Icon) |> ignore

treeView1.AfterSelect.Add <| fun e ->
    if e.Node <> null then
        listDir(e.Node.Tag :?> Entry)

miFileExit.Click.Add <| fun _ -> f.Close()

let mutable root = Unchecked.defaultof<Entry>
let ofd = new OpenFileDialog()

miFileOpen.Click.Add <| fun _ ->
    if ofd.ShowDialog(f) = DialogResult.OK then
        let sw = new StringWriter()
        try
            let fs = new FileStream(ofd.FileName, FileMode.Open)
            root <- Open(fs)
            fs.Dispose()
            
            root.FileSystem.Write(sw)
            let rec dir (e:Entry) =
                sw.WriteLine()
                e.Write(sw)
                for child in e.Children do
                    dir(child)
            dir(root)
            treeView1.Nodes.Clear()
            let nroot = treeDir root null
            nroot.Expand()
            treeView1.SelectedNode <- nroot
            miFileSaveZip.Enabled <- true
        with e ->
#if DEBUG
            reraise()
#else
            sw.WriteLine(e.ToString())
            miFileSaveZip.Enabled <- false
            root <- Unchecked.defaultof<Entry>
#endif
        textBox1.Text <- sw.ToString()

let sfd = new SaveFileDialog(Filter = "ZIP Archive (*.zip)|*.zip|All files (*.*)|*.*")

miFileSaveZip.Click.Add <| fun _ ->
    if sfd.ShowDialog(f) = DialogResult.OK then
        let cur = Cursor.Current
        Cursor.Current <- Cursors.WaitCursor
        try
            use fs = new FileStream(sfd.FileName, FileMode.Create)
            SaveZip(fs, root)
        with e ->
#if DEBUG
            reraise()
#else
            MessageBox.Show(e.ToString(), f.Text,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Exclamation) |> ignore
#endif
        Cursor.Current <- cur

[<STAThread>] Application.Run(f)
