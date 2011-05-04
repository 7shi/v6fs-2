open System
open System.Drawing
open System.IO
open System.Windows.Forms

Application.EnableVisualStyles()
Application.SetCompatibleTextRenderingDefault(false)

let getResourceBitmap name =
    use s = Utils.getResourceStream(name)
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

let f = new Form(Text = "V6FS", Menu = menu, Width = 600, Height = 400)
let mono = new Font(FontFamily.GenericMonospace, Control.DefaultFont.Size)
let split1 = new SplitContainer(Dock = DockStyle.Fill)
let textBox1 = new TextBox(Dock = DockStyle.Fill,
                           WordWrap = false,
                           Multiline = true,
                           Font = mono,
                           ScrollBars = ScrollBars.Both)
split1.Panel1.Controls.Add(textBox1)
let listView1 = new ListView(Dock = DockStyle.Fill,
                             FullRowSelect = true,
                             View = View.Details,
                             SmallImageList = icons)
let clmName = new ColumnHeader(Text = "Name", Width = 200)
let clmSize = new ColumnHeader(Text = "Size", Width = 64, TextAlign = HorizontalAlignment.Right)
listView1.Columns.AddRange([|clmName; clmSize|])
split1.Panel2.Controls.Add(listView1)
f.Controls.Add(split1)
split1.SplitterDistance <- f.ClientSize.Width / 2

let listDir(dir:V6FS.Entry) =
    listView1.Items.Clear()
    for e in dir.Children do
        listView1.Items.Add(e.Name, e.Icon) |> ignore

miFileExit.Click.Add <| fun _ -> f.Close()

let mutable root = Unchecked.defaultof<V6FS.Entry>
let ofd = new OpenFileDialog()

miFileOpen.Click.Add <| fun _ ->
    if ofd.ShowDialog(f) = DialogResult.OK then
        let sw = new StringWriter()
        try
            let fs = new FileStream(ofd.FileName, FileMode.Open)
            root <- V6FS.Open(fs)
            fs.Dispose()
            
            root.FileSystem.Write(sw)
            let rec dir (e:V6FS.Entry) =
                sw.WriteLine()
                e.Write(sw)
                if not(Utils.isCurOrParent e.Name) then
                    for child in e.Children do
                        dir(child)
            dir(root)
            listDir root
            miFileSaveZip.Enabled <- true
        with e ->
#if DEBUG
            reraise()
#else
            sw.WriteLine(e.ToString())
            miFileSaveZip.Enabled <- false
            root <- Unchecked.defaultof<V6FS.Entry>
#endif
        textBox1.Text <- sw.ToString()

let sfd = new SaveFileDialog(Filter = "ZIP Archive (*.zip)|*.zip|All files (*.*)|*.*")

miFileSaveZip.Click.Add <| fun _ ->
    if sfd.ShowDialog(f) = DialogResult.OK then
        let cur = Cursor.Current
        Cursor.Current <- Cursors.WaitCursor
        try
            use fs = new FileStream(sfd.FileName, FileMode.Create)
            V6FS.SaveZip(fs, root)
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
