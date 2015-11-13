@set FSC="C:\Program Files (x86)\Microsoft SDKs\F#\3.0\Framework\v4.0\fsc"
@set FSCOPT=--platform:x86 --nologo --standalone --lib:C:\Windows\Microsoft.NET\Framework\v4.0.30319
@set SRCS=V6FS\Utils.fs V6FS\Crc.fs V6FS\Deflate.fs V6FS\Zip.fs V6FS\V6Type.fs V6FS\V6FS.fs WinForms\Program.fs
@set RSRCS=--resource:V6FS\folder.png --resource:V6FS\file.png --resource:V6FS\text.png --resource:V6FS\executable.png
%FSC% %FSCOPT% --target:winexe --out:V6FS.exe %SRCS% %RSRCS%
