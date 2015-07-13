using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using ImpromptuInterface;
using ImpromptuInterface.Dynamic;
namespace DotNetDBF.Enumerable
{
    /// <summary>
    /// Interface to get the contents of the DBF Wrapper
    /// </summary>
    public interface IDBFIntercepter
    {
        /// <summary>
        /// Does field exist in row
        /// </summary>
        /// <returns></returns>
        bool Exists(string fieldName);
        /// <summary>
        /// Gets the data row.
        /// </summary>
        /// <returns></returns>
        object[] GetDataRow();
    }

    public class DBFIntercepter : DBFEnumerable.DBFIntercepter
    {
        public DBFIntercepter(object[] wrappedObj, string[] fieldNames) : base(wrappedObj, fieldNames)
        {
        }
    }


    /// <summary>
    /// DBF Dynamic Wrapper
    /// </summary>
    public abstract class BaseDBFIntercepter : ImpromptuObject, IDBFIntercepter
    {
        private readonly string[] _fieldNames;
        private readonly object[] _wrappedArray;

        protected BaseDBFIntercepter(object[] wrappedObj, string[] fieldNames)
        {
            _wrappedArray = wrappedObj;
            _fieldNames = fieldNames;

        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return _fieldNames;
        }

        public bool Exists(string fieldName)
        {
            return _fieldNames.Contains(fieldName);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;
            var tLookup = binder.Name;
            var tIndex = Array.FindIndex(_fieldNames,
                                         it => it.Equals(tLookup, StringComparison.InvariantCultureIgnoreCase));

            if (tIndex < 0)
                return false;


            result = _wrappedArray[tIndex];


            Type outType;
            if (TryTypeForName(tLookup, out outType))
            {
                result = Impromptu.CoerceConvert(result, outType);
            }

            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {

            var tLookup = binder.Name;
            var tIndex = Array.FindIndex(_fieldNames,
                                         it => it.Equals(tLookup, StringComparison.InvariantCultureIgnoreCase));

            if (tIndex < 0)
                return false;

            Type outType;
            if (TryTypeForName(tLookup, out outType))
            {
                value = Impromptu.CoerceConvert(value, outType);
            }

            _wrappedArray[tIndex] = value;

            return true;
        }

        public object[] GetDataRow()
        {
            return _wrappedArray;
        }
    }

    /// <summary>
    /// Enumerable API
    /// </summary>
    public static partial class DBFEnumerable
    {

        /// <summary>
        /// New Blank Row Dynamic object that matches writer;
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <returns></returns>
        public static dynamic NewBlankRow(this DBFWriter writer)
        {
            var fields = writer.Fields.Select(it => it.Name).ToArray();
            var obj = new object[fields.Length];
            return new Enumerable.DBFIntercepter(obj, fields);
        }


        /// <summary>
        /// Writes the record.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        public static void WriteRecord(this DBFWriter writer, Enumerable.IDBFIntercepter value)
        {
            writer.WriteRecord(value.GetDataRow());
        }

        /// <summary>
        /// Adds the record.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        public static void AddRecord(this DBFWriter writer, Enumerable.IDBFIntercepter value)
        {
            writer.AddRecord(value.GetDataRow());
        }

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

            var tProperties = tType.GetProperties()
                .Where(
                    it =>
                        Array.FindIndex(reader.Fields,
                            f => f.Name.Equals(it.Name, StringComparison.InvariantCultureIgnoreCase)) >= 0);
            var tProps = tProperties
                .Select(
                it =>
                Array.FindIndex(reader.Fields,
                                jt => jt.Name.Equals(it.Name, StringComparison.InvariantCultureIgnoreCase))).Where(it=> it >= 0).ToArray();
            
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

                    var tIntercepter = new Enumerable.DBFIntercepter(t, tProperties.Select(it => it.Name).ToArray());

                    tReturn.Add(tIntercepter.ActLike<T>(typeof(Enumerable.IDBFIntercepter)));
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


                var tIntercepter = new Enumerable.DBFIntercepter(t, tProperties);


                tReturn.Add(tIntercepter);
                t = reader.NextRecord();
            }


            return tReturn;

        }

      
    }
}
