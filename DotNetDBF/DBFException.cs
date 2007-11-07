/*
 DBFException
 Represents exceptions happen in the JAvaDBF classes.
 
 This file is part of DotNetDBF packege.
 
 original author (javadbf): anil@linuxense.com 2004/03/31
 license: LGPL (http://www.gnu.org/copyleft/lesser.html)
 
 ported to C# (DotNetDBF): Jay Tuley <jay+dotnetdbf@tuley.name> 6/28/2007
 
 */

using System;
using System.IO;

namespace DotNetDBF
{
    public class DBFException : IOException
    {
        public DBFException() : base()
        {
        }

        public DBFException(String msg) : base(msg)
        {
        }

        public DBFException(String msg, Exception internalException)
            : base(msg, internalException)
        {
        }
    }
}