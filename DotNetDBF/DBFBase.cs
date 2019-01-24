/*
 Serves as the base class of DBFReader adn DBFWriter.
 
 This file is part of DotNetDBF packege.
 
 original author (javadbf): anil@linuxense.com 2004/03/31
 license: LGPL (http://www.gnu.org/copyleft/lesser.html)

 Support for choosing implemented character Sets as
 suggested by Nick Voznesensky <darkers@mail.ru>
 
 ported to C# (DotNetDBF): Jay Tuley <jay+dotnetdbf@tuley.name> 6/28/2007
 
 */
/**
 Base class for DBFReader and DBFWriter.
 */

using System;
using System.Text;

namespace DotNetDBF
{
    public abstract class DBFBase
    {

        public Encoding CharEncoding { get; set; } = Encoding.GetEncoding("utf-8");

        public int BlockSize { get; set; } = 512;
        
        private string _nullSymbol;
        public string NullSymbol
        {
            get => _nullSymbol ?? DBFFieldType.Unknown;
            set
            {
                if (value != null && value.Length != 1)
                    throw new ArgumentException(nameof(NullSymbol));
                _nullSymbol = value;
            }
        }
	
    }
}