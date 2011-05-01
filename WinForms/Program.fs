open System
open System.Drawing
open System.IO
open System.Windows.Forms

Application.EnableVisualStyles()
Application.SetCompatibleTextRenderingDefault(false)

let menu = new MainMenu()
let miFile = new MenuItem("&File")
let miFileOpen = new MenuItem("&Open")
let miFileSaveZip = new MenuItem(Text = "Save as &Zip", Enabled = false)
let miFileExit = new MenuItem("E&xit")
miFile.MenuItems.AddRange([|miFileOpen; miFileSaveZip; new MenuItem("-"); miFileExit|])
menu.MenuItems.Add(miFile) |> ignore

let f = new Form(Text = "V6FS", Menu = menu)
let mono = new Font(FontFamily.GenericMonospace, Control.DefaultFont.Size)
let textBox1 = new TextBox(Dock = DockStyle.Fill,
                           WordWrap = false,
                           Multiline = true,
                           Font = mono,
                           ScrollBars = ScrollBars.Both)
f.Controls.Add(textBox1)

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
                for child in e.Children do
                    dir(child)
            dir(root)
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
