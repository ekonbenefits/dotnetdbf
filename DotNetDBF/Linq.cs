using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace DotNetDBF.Linq
{
    static public class Linq
    {

        static public IEnumerable<T> AllRecords<T>(this DBFReader aReader, T aTemplate)
        {
            var tType = typeof(T);
            if (tType.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Any())
            {
                var tProps = tType.GetProperties()
                    .Select(
                    it =>
                    Array.FindIndex(aReader.Fields,
                                    jt => jt.Name.Equals(it.Name, StringComparison.InvariantCultureIgnoreCase))).ToArray();


                var tReturn = new List<T>();
                object[] t = aReader.NextRecord(tProps);
                while(t != null)
                {

                    tReturn.Add((T)Activator.CreateInstance(tType, t));
                    t = aReader.NextRecord(tProps);
                }



                return tReturn;
            }

            throw new Exception("Use Annoymous Types Only");
        }
    }
}
