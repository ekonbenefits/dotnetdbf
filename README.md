dotnetdbf
=========

This is a basic file parser written in C# for reading and writing xBase DBF files.
Pure .NET direct access to xBase DBF. No need OBBC/JDBC for readind and writeing xBase DBF.
xBase DBF files are used by Clipper and FoxPro.

Compilable on Linux (Mono).
For .net 4.0 projects there is an enumeration framework in which makes it easy to use Linq to Objects. 

Code derived from javadbf.

## Get The Binaries
Use [NuGet](http://nuget.org/packages/dotnetdbf/) from Visual Studio

## Quick start 
Writng ODF

<pre>
using (Stream fos = File.Open(dbffile, FileMode.OpenOrCreate, FileAccess.ReadWrite))
      using (var writer = new DBFWriter())
      {        
          writer.CharEncoding = Encoding.GetEncoding(866);
          writer.Signature = DBFSigniture.DBase3;
          writer.LanguageDriver = 0x26; // Eq to CP866
          var field1 = new DBFField("DOCDATE", NativeDbType.Date);
          var field2 = new DBFField("DOCNUMBER", NativeDbType.Char, 50);                    
          ...
          var field9 = new DBFField("f9", NativeDbType.Char, 20);
          writer.Fields = new[] { field1, field2, field3, field4, field5, field6, field7, field8, field9 };
          foreach (var item in items)
          {
              writer.AddRecord(
              ...
              );
          }
          writer.Write(fos);
      }
</pre>
