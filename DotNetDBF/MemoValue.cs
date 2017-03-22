using System;
using System.IO;
using System.Text;

namespace DotNetDBF
{
    public class MemoValue
    {
        #region Fields
        public const string MemoTerminator = "\x1A";

        private readonly DBFBase _base;
        private readonly string _lockName;
        private readonly string _fileLoc;

        private bool _loaded;
        private bool _new;
        private long _block;
        private string _value;
        #endregion

        #region Consructor
        public MemoValue(string memoValue)
        {
            _lockName = string.Format("DotNetDBF.Memo.new.{0}", Guid.NewGuid());
            Value = memoValue;
        }

        internal MemoValue(long block, DBFBase dbfBase, string fileLoc)
        {
            _block = block;
            _base = dbfBase;
            _fileLoc = fileLoc;
            _lockName = string.Format("DotNetDBF.Memo.read.{0}.{1}.", _fileLoc, _block);
        }
        #endregion

        #region Properties
        internal long Block
        {
            get { return _block; }
        }

        public string Value
        {
            get
            {
                lock (_lockName)
                {
                    if (!_new && !_loaded)
                    {
                        using (BinaryReader reader = new BinaryReader(File.Open(_fileLoc, FileMode.Open, FileAccess.Read, FileShare.Read)))
                        {
                            reader.BaseStream.Seek(_block * _base.BlockSize, SeekOrigin.Begin);
                            string tempString;
                            StringBuilder sb = new StringBuilder();
                            int index;
                            string softReturn = _base.CharEncoding.GetString(new byte[] { 0x8d, 0x0a });

                            do
                            {
                                byte[] data = reader.ReadBytes(_base.BlockSize);

                                tempString = _base.CharEncoding.GetString(data);
                                index = tempString.IndexOf(MemoTerminator);
                                if (index != -1)
                                    tempString = tempString.Substring(0, index);
                                sb.Append(tempString);
                            } while (index == -1);

                            sb.Replace(softReturn, String.Empty);
                            _value = sb.ToString();
                        }
                        _loaded = true;
                    }

                    return _value;
                }
            }
            set
            {
                lock (_lockName)
                {
                    _new = true;
                    _value = value;
                }

            }
        }
        #endregion

        internal void Write(DBFWriter dbfWriter)
        {
            lock (_lockName)
            {
                if (!_new)
                    return;
                if (string.IsNullOrEmpty(dbfWriter.DataMemoLoc))
                    throw new Exception("No Memo Location Set");

                FileStream fs = File.Open(dbfWriter.DataMemoLoc, FileMode.OpenOrCreate, FileAccess.ReadWrite);

                // before proceeding check whether the passed in File object
                // is an empty/non-existent file or not.

                using (BinaryWriter writer = new BinaryWriter(fs, dbfWriter.CharEncoding))
                {
                    if (fs.Length == 0)
                    {
                        DBTHeader header = new DBTHeader();
                        header.Write(writer);
                    }

                    var tValue = _value;
                    if ((tValue.Length + sizeof(int)) % dbfWriter.BlockSize != 0)
                    {
                        tValue = tValue + MemoTerminator;
                    }

                    long position = fs.Seek(0, SeekOrigin.End); // Got to end of file
                    long blockDiff = position % dbfWriter.BlockSize;
                    if (blockDiff != 0)
                    {
                        position = fs.Seek(dbfWriter.BlockSize - blockDiff, SeekOrigin.Current);
                    }
                    _block = position/dbfWriter.BlockSize;
                    byte[] data = dbfWriter.CharEncoding.GetBytes(tValue);
                    int dataLength = data.Length;
                    int newDiff = (dataLength % dbfWriter.BlockSize);
                    writer.Write(data);
                    if (newDiff != 0)
                        writer.Seek(dbfWriter.BlockSize - (dataLength % dbfWriter.BlockSize), SeekOrigin.Current);
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
            if (obj as MemoValue == null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;

            return Value.Equals(((MemoValue)obj).Value);
        }
    }

}
