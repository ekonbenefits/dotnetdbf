using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using ImpromptuInterface;

namespace DotNetDBF.Enumerable
{
    static public class Enuemrable
    {



        /// <summary>
        /// Return all the records. T should be interface with getter properties that match types and names of the database. 
        /// Optionally instead of T being and interface you can pass in an annoymous object with properties that match that 
        /// database and then you'll get an IEnumerable of that annonymous type with the data filled in.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader">The reader.</param>
        /// <param name="prototype">The prototype. Anonymous class instance</param>
        /// <returns></returns>
        static public IEnumerable<T> AllRecords<T>(this DBFReader reader, T prototype =null) where T : class 
        {
            var tType = typeof(T);
            
            var tProperties = tType.GetProperties();
            var tProps = tProperties
                .Select(
                it =>
                Array.FindIndex(reader.Fields,
                                jt => jt.Name.Equals(it.Name, StringComparison.InvariantCultureIgnoreCase))).ToArray();
            
            var tOrderedProps = tProps.OrderBy(it => it).ToArray();
            var tReturn = new List<T>();


            if (tType.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Any())
            {
                object[] tAnon = reader.NextRecord(tProps, tOrderedProps);
                while (tAnon != null)
                {

                    tReturn.Add((T)Activator.CreateInstance(tType, tAnon));
                    tAnon = reader.NextRecord(tProps, tOrderedProps);
                }



                return tReturn;
            }
               
            var t = reader.NextRecord(tProps, tOrderedProps);  

            while (t != null)
                {

                    var tIntercepter = new DBFIntercepter(t, tProperties.Select(it => it.Name).ToArray());

                    tReturn.Add(tIntercepter.ActLike<T>(typeof(IDBFIntercepter)));
                    t = reader.NextRecord(tProps, tOrderedProps);
                }



                return tReturn;
            
        }



        /// <summary>
        /// Returns a list of dynamic objects whose properties and types match up with that database name.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="whereColumn">The where column name.</param>
        /// <param name="whereColumnEquals">What the were column should equal.</param>
        /// <returns></returns>
        static public IEnumerable<dynamic> DynamicAllRecords(this DBFReader reader, string whereColumn = null, dynamic whereColumnEquals = null)
        {
           
            var tProperties = reader.GetSelectFields().Select(it=>it.Name).ToArray();

            int? tWhereColumn=null;
            if(!String.IsNullOrEmpty(whereColumn))
            {
                tWhereColumn = Array.FindIndex(tProperties,
                                it => it.Equals(whereColumn, StringComparison.InvariantCultureIgnoreCase));
            }

      
            var tReturn = new List<object>();
            object[] t = reader.NextRecord();

            while (t != null)
            {
                if (tWhereColumn.HasValue)
                {
                    dynamic tO = t[tWhereColumn.Value];
                    if(!tO.Equals(whereColumnEquals))
                    {
                        t = reader.NextRecord();
                        continue;
                    }
                }


                var tIntercepter = new DBFIntercepter(t, tProperties);


                tReturn.Add(tIntercepter);
                t = reader.NextRecord();
            }


            return tReturn;

        }

        public interface IDBFIntercepter
        {
            object[] GetDataRow();
        }

        public class DBFIntercepter : ImpromptuObject, IDBFIntercepter
        {
            private readonly string[] _fieldNames;
            private readonly object[] _wrappedArray;

            public DBFIntercepter(object[] wrappedObj, string[] fieldNames)
            {
                _wrappedArray = wrappedObj;
                _fieldNames = fieldNames;

            }

            public override IEnumerable<string> GetDynamicMemberNames()
            {
                return _fieldNames;
            }

            public override bool  TryGetMember(GetMemberBinder binder, out object result)
            {
                result = null;
                var tLookup = binder.Name;
                var tIndex = Array.FindIndex(_fieldNames,
                                             it => it.Equals(tLookup, StringComparison.InvariantCultureIgnoreCase));

                if(tIndex <0)
                    return false;


                result = _wrappedArray[tIndex];

                if (result == null)
                {
                    Type outType;
                    if (TryTypeForName(tLookup, out outType) && outType.IsValueType)
                    {
                        result = Activator.CreateInstance(outType);
                    }
                }
                return true;
            }

            public object[] GetDataRow()
            {
                return _wrappedArray;
            }
        }
    }
}
