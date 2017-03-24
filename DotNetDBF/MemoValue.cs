using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DotNetDBF
{
    public class MemoValue
    {

        public const string MemoTerminator = "\x1A";
        private bool _loaded;
        private bool _new;
        
        

        public MemoValue(string aValue)
        {
            _lockName = $"DotNetDBF.Memo.new.{Guid.NewGuid()}";
            Value = aValue;
         
        }


        internal MemoValue(long block, DBFBase aBase, string fileLoc)
        {
            _block = block;
            _base = aBase;
            _fileLoc = fileLoc;
            _lockName = $"DotNetDBF.Memo.read.{_fileLoc}.{_block}.";
        }

        private readonly DBFBase _base;
        private readonly string _lockName;
        private long _block;
        private readonly string _fileLoc;
        private string _value;

        internal long Block
        {
            get
            {
               return _block;
            }
        }

        internal void Write(DBFWriter aBase)
        {
            lock (_lockName)
            {
                if (!_new)
                    return;
                if (string.IsNullOrEmpty(aBase.DataMemoLoc))
                    throw new Exception("No Memo Location Set");

                var raf =
                    File.Open(aBase.DataMemoLoc,
                              FileMode.OpenOrCreate,
                              FileAccess.ReadWrite);

                /* before proceeding check whether the passed in File object
                    is an empty/non-existent file or not.
                    */
             

                using (var tWriter = new BinaryWriter(raf, aBase.CharEncoding))
                {
                    if (raf.Length == 0)
                    {
                        var tHeader = new DBTHeader();
                        tHeader.Write(tWriter);
                    }

                    var tValue = _value;
                    if ((tValue.Length + sizeof(int)) % aBase.BlockSize != 0)
                    {
                        tValue = tValue + MemoTerminator;
                    }

                    var tPosition = raf.Seek(0, SeekOrigin.End); //Got To End Of File
                    var tBlockDiff = tPosition%aBase.BlockSize;
                    if (tBlockDiff != 0)
                    {
                        tPosition = raf.Seek(aBase.BlockSize - tBlockDiff, SeekOrigin.Current);
                    }
                    _block = tPosition/aBase.BlockSize;
                    var tData = aBase.CharEncoding.GetBytes(tValue);
                    var tDataLength = tData.Length;
                    var tNewDiff = (tDataLength%aBase.BlockSize);
                    tWriter.Write(tData);
                    if (tNewDiff != 0)
                        tWriter.Seek(aBase.BlockSize - (tDataLength % aBase.BlockSize), SeekOrigin.Current);
                }
            }
        }


        public string Value
        {
            get
            {
                lock (_lockName)
                {
                 
                    if (!_new && !_loaded)
                    {
                        using (var reader =new BinaryReader(
                            File.Open(_fileLoc, 
                            FileMode.Open, 
                            FileAccess.Read,
                            FileShare.Read)))
                        {
                            reader.BaseStream.Seek(_block*_base.BlockSize, SeekOrigin.Begin);
                            string tString;
                            var tStringBuilder = new StringBuilder();
                            int tIndex;
                            var tSoftReturn = _base.CharEncoding.GetString(new byte[] {0x8d, 0x0a});

                            do
                            {
                                var tData = reader.ReadBytes(_base.BlockSize);
                               
                                tString = _base.CharEncoding.GetString(tData);
                                tIndex = tString.IndexOf(MemoTerminator);
                                if (tIndex != -1)
                                    tString = tString.Substring(0, tIndex);
                                tStringBuilder.Append(tString);
                            } while (tIndex == -1);
                            _value = tStringBuilder.ToString().Replace(tSoftReturn,String.Empty);
                        }
                        _loaded = true;
                    }

                    return _value;
                }
            }set
            {
                lock (_lockName)
                {
                    _new = true;

                   
                    _value = value;
                }

            }
        }

        public override int GetHashCode()
        {
            return _lockName.GetHashCode();
        }
        public override string ToString()
        {
            return Value;
        } 
        public override bool Equals(object obj)
        {
            if(obj as MemoValue == null)
                return false;
            if(ReferenceEquals(this,obj))
            {
                return true;
            }
            return Value.Equals(((MemoValue)obj).Value);
        }
    }

}
