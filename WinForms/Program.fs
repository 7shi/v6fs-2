open System
open System.Collections.Generic
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

let f = new Form(Text = "V6FS", Menu = menu, Width = 780, Height = 560)
let treeView1 = new TreeView(Dock = DockStyle.Left,
                             HideSelection = false,
                             ShowRootLines = false,
                             ImageList = icons,
                             Width = 130)
let split1 = new Splitter(Dock = DockStyle.Left)
let listView1 = new ListView(Dock = DockStyle.Left,
                             HideSelection = false,
                             FullRowSelect = true,
                             View = View.Details,
                             HeaderStyle = ColumnHeaderStyle.Nonclickable,
                             SmallImageList = icons,
                             Width = 180)
let clmName = new ColumnHeader(Text = "Name", Width = 90)
let clmSize = new ColumnHeader(Text = "Size", Width = 60,
                               TextAlign = HorizontalAlignment.Right)
listView1.Columns.AddRange([|clmName; clmSize|])
let split2 = new Splitter(Dock = DockStyle.Left)
let panel1 = new Panel(Dock = DockStyle.Fill)
let mono = new Font(FontFamily.GenericMonospace, Control.DefaultFont.Size)
let textBox1 = new TextBox(Dock = DockStyle.Top,
                           HideSelection = false,
                           WordWrap = false,
                           Multiline = true,
                           Font = mono,
                           ScrollBars = ScrollBars.Both,
                           Height = 180)
let split3 = new Splitter(Dock = DockStyle.Top)
let textBox2 = new TextBox(Dock = DockStyle.Fill,
                           HideSelection = false,
                           WordWrap = false,
                           Multiline = true,
                           Font = mono,
                           ScrollBars = ScrollBars.Both)
panel1.Controls.AddRange([| textBox2; split3; textBox1 |])
f.Controls.AddRange([| panel1; split2; listView1; split1; treeView1 |])

let dirdic = new Dictionary<string, TreeNode>()

let rec dirTree (dir:Entry) (n:TreeNode) =
    let nn = if n <> null then n.Nodes else treeView1.Nodes
    let n = nn.Add(dir.Icon, dir.Name)
    n.Tag <- dir
    dirdic.Add(dir.FullName, n)
    for e in dir.Children do
        if e.INode.IsDir then
            dirTree e n |> ignore
    n

let dirList (dir:Entry) =
    listView1.Items.Clear()
    for e in dir.Children do
        let it = listView1.Items.Add(e.Name, e.Icon)
        it.Tag <- e
        it.SubItems.Add(e.INode.Length.ToString()) |> ignore

let rec dirINodes (tw:TextWriter) (e:Entry) =
    tw.WriteLine()
    e.Write(tw)
    for child in e.Children do
        dirINodes tw child

let showInfo (e:Entry) =
    use sw = new StringWriter()
    if e.INode.inode = 1 then
        e.FileSystem.Write sw
        sw.WriteLine()
    e.Write sw
    textBox1.Text <- sw.ToString()
    textBox2.Text <-
        let bytes = readAllBytes e.INode
        if e.Icon = "text" then
            getText bytes
        else
            getHexDump bytes

treeView1.AfterSelect.Add <| fun e ->
    if e.Node <> null then
        let ent = e.Node.Tag :?> Entry
        dirList ent
        showInfo ent

listView1.SelectedIndexChanged.Add <| fun _ ->
    let it = listView1.SelectedItems
    if it.Count > 0 then
        let ent = it.[0].Tag :?> Entry
        showInfo ent

listView1.DoubleClick.Add <| fun _ ->
    let it = listView1.SelectedItems
    if it.Count > 0 then
        let path = (it.[0].Tag :?> Entry).FullName
        if dirdic.ContainsKey(path) then
            treeView1.SelectedNode <- dirdic.[path]

miFileExit.Click.Add <| fun _ -> f.Close()

let mutable root = Unchecked.defaultof<Entry>
let ofd = new OpenFileDialog()

miFileOpen.Click.Add <| fun _ ->
    if ofd.ShowDialog(f) = DialogResult.OK then
        try
            let fs = new FileStream(ofd.FileName, FileMode.Open)
            root <- Open(fs)
            fs.Dispose()
            
            dirdic.Clear()
            treeView1.Nodes.Clear()
            let nroot = dirTree root null
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
