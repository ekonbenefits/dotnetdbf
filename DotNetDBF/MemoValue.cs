using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DotNetDBF
{
    public class MemoValue
    {

        public const string MemoTerminator = "\x1A\x1A";
        private bool _loaded;
        private bool _new;
        
        

        public MemoValue(string aValue)
        {
            _lockName = string.Format("DotNetDBF.Memo.new.{0}", Guid.NewGuid());
            Value = aValue;
         
        }


        internal MemoValue(long block, DBFBase aBase, string fileLoc)
        {
            _block = block;
            _base = aBase;
            _fileLoc = fileLoc;
            _lockName = string.Format("DotNetDBF.Memo.read.{0}.{1}.", _fileLoc, _block);
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
                    var tDataLength = tData.Length + sizeof (int);
                    var tNewDiff = (tDataLength%aBase.BlockSize);
                    var tCount = (tDataLength/aBase.BlockSize) + (tNewDiff != 0 ? 1 : 0);
                    tWriter.Write(tCount);
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
                          
                            var tCount =reader.ReadInt32();
                            var tData = reader.ReadBytes((_base.BlockSize * tCount) - sizeof(int));
                            var tString = _base.CharEncoding.GetString(tData);
                            _value = tString.Substring(0,
                                                     tString.IndexOf(MemoTerminator,
                                                                     (tString.Length/_base.BlockSize)*_base.BlockSize));
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
