﻿open System
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

let cmenu1 = new ContextMenu()
let miSaveZip1 = new MenuItem("Save Directory as &Zip", Enabled = false)
cmenu1.MenuItems.AddRange([|miSaveZip1|])
let cmenu2 = new ContextMenu()
let miSave = new MenuItem("&Save File", Enabled = false)
let miSaveZip2 = new MenuItem("Save Directory as &Zip", Enabled = false)
cmenu2.MenuItems.AddRange([|miSave; miSaveZip2|])

let menu = new MainMenu()
let miFile = new MenuItem("&File")
let miFileOpen = new MenuItem("&Open Image")
let miFileSaveZip = new MenuItem("Save Image as &Zip", Enabled = false)
let miFileExit = new MenuItem("E&xit")
miFile.MenuItems.AddRange(
    [|miFileOpen; miFileSaveZip; new MenuItem("-"); miFileExit|])
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
                             Width = 180,
                             ContextMenu = cmenu2,
                             MultiSelect = false)
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
        it.SubItems.Add(String.Format("{0:#,#}", e.INode.Length)) |> ignore

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
        miSaveZip1.Enabled <- true
        miSaveZip2.Enabled <- false
        miSave.Enabled <- false

treeView1.MouseUp.Add <| fun e ->
    if e.Button = MouseButtons.Right then
        let n = treeView1.GetNodeAt(e.X, e.Y)
        treeView1.SelectedNode <- n
        cmenu1.Show(treeView1, e.Location)

listView1.SelectedIndexChanged.Add <| fun _ ->
    let it = listView1.SelectedItems
    if it.Count > 0 then
        let ent = it.[0].Tag :?> Entry
        showInfo ent
        let isDir = ent.INode.IsDir
        miSaveZip1.Enabled <- false
        miSaveZip2.Enabled <- isDir
        miSave.Enabled <- not isDir

listView1.DoubleClick.Add <| fun _ ->
    let it = listView1.SelectedItems
    if it.Count > 0 then
        let path = (it.[0].Tag :?> Entry).FullName
        if dirdic.ContainsKey(path) then
            treeView1.SelectedNode <- dirdic.[path]

miFileExit.Click.Add <| fun _ -> f.Close()

let mutable root = Unchecked.defaultof<Entry>
let mutable imgfn = ""
let ofd = new OpenFileDialog()

miFileOpen.Click.Add <| fun _ ->
    if ofd.ShowDialog(f) = DialogResult.OK then
#if DEBUG
        do
#else
        try
#endif
            imgfn <- ofd.FileName
            let fs = new FileStream(imgfn, FileMode.Open)
            root <- Open(fs)
            fs.Dispose()
            
            dirdic.Clear()
            treeView1.Nodes.Clear()
            let nroot = dirTree root null
            nroot.Expand()
            treeView1.SelectedNode <- nroot
            miFileSaveZip.Enabled <- true
#if DEBUG
#else
        with e ->
            textBox1.Text <- e.ToString()
            miFileSaveZip.Enabled <- false
            root <- Unchecked.defaultof<Entry>
#endif

let sfd = new SaveFileDialog(Filter = "ZIP Archive (*.zip)|*.zip|All files (*.*)|*.*")

let saveDir fn dir =
    let cur = Cursor.Current
    Cursor.Current <- Cursors.WaitCursor
#if DEBUG
    do
#else
    try
#endif
        use fs = new FileStream(fn, FileMode.Create)
        SaveZip(fs, dir)
#if DEBUG
#else
    with e ->
        MessageBox.Show(e.ToString(), f.Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation) |> ignore
#endif
    Cursor.Current <- cur

miFileSaveZip.Click.Add <| fun _ ->
    sfd.FileName <- imgfn + ".zip"
    if sfd.ShowDialog(f) = DialogResult.OK then
        saveDir sfd.FileName root

miSaveZip1.Click.Add <| fun _ ->
    let n = treeView1.SelectedNode
    if n <> null then
        sfd.FileName <- (if n.Text = "/" then imgfn else n.Text) + ".zip"
        if sfd.ShowDialog(f) = DialogResult.OK then
            saveDir sfd.FileName (n.Tag :?> Entry)

miSaveZip2.Click.Add <| fun _ ->
    let it = listView1.SelectedItems
    if it.Count > 0 then
        sfd.FileName <- it.[0].Text + ".zip"
        if sfd.ShowDialog(f) = DialogResult.OK then
            let ent = it.[0].Tag :?> Entry
            saveDir sfd.FileName ent

miSave.Click.Add <| fun _ ->
    let it = listView1.SelectedItems
    if it.Count > 0 then
        use sfd = new SaveFileDialog()
        sfd.FileName <- it.[0].Text
        if sfd.ShowDialog(f) = DialogResult.OK then
            let ent = it.[0].Tag :?> Entry
            File.WriteAllBytes(sfd.FileName, readAllBytes ent.INode)

[<STAThread>] Application.Run(f)
