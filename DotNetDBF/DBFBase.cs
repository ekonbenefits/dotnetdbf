/*
 Serves as the base class of DBFReader adn DBFWriter.
 
 This file is part of DotNetDBF packege.
 
 original author (javadbf): anil@linuxense.com 2004/03/31
 license: LGPL (http://www.gnu.org/copyleft/lesser.html)

 Support for choosing implemented character Sets as
 suggested by Nick Voznesensky <darkers@mail.ru>
 
 ported to C# (DotNetDBF): Jay Tuley <jay+dotnetdbf@tuley.name> 6/28/2007
 
 */
using System.Globalization;
using System.Text;

namespace DotNetDBF
{
    /// <summary>
    /// Base class for <see cref="DBFReader"/> and <see cref="DBFWriter"/>.
    /// </summary>
    public abstract class DBFBase
    {
        protected Encoding _charEncoding = Encoding.GetEncoding("utf-8");
        protected int _blockSize = 512;
        protected NumberFormatInfo _numberFormatProvider = NumberFormatInfo.InvariantInfo;

        public Encoding CharEncoding
        {
            get { return _charEncoding; }
            set { _charEncoding = value; }
        }

        public int BlockSize
        {
            get { return _blockSize; }
            set { _blockSize = value; }
        }

        public NumberFormatInfo NumberFormatProvider
        {
            get { return _numberFormatProvider; }
            set { _numberFormatProvider = value; }
        }
    }
}