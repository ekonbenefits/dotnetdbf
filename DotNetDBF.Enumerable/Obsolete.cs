using System;
using System.Collections.Generic;

namespace DotNetDBF.Enumerable
{

    public static partial class DBFEnumerable
    {
        [Obsolete("DotNetDBF.Enumerable.IDBFIntercepter is the new interface name")]
        public interface IDBFIntercepter : DotNetDBF.Enumerable.IDBFIntercepter
        {

        }

        [Obsolete("DotNetDBF.Enumerable.DBFIntercepter is the new class name")]
        public class DBFIntercepter : DotNetDBF.Enumerable.Enuemrable.DBFIntercepter, IDBFIntercepter
        {
            public DBFIntercepter(object[] wrappedObj, string[] fieldNames)
                : base(wrappedObj, fieldNames)
            {
            }
        }
    }


    [Obsolete("DBFEnumerable is the new class name")]
    public static class Enuemrable
    {
        /// <summary>
        /// New Blank Row Dynamic object that matches writer;
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <returns></returns>
        public static dynamic NewBlankRow(DBFWriter writer)
        {
            return writer.NewBlankRow();
        }


        /// <summary>
        /// Writes the record.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        public static void WriteRecord(DBFWriter writer, IDBFIntercepter value)
        {
            writer.WriteRecord(value);
        }

        /// <summary>
        /// Adds the record.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        public static void AddRecord(DBFWriter writer, IDBFIntercepter value)
        {
            writer.AddRecord(writer,value);
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
        static public IEnumerable<T> AllRecords<T>(DBFReader reader, T prototype = null) where T : class
        {

            return reader.AllRecords(prototype);
        }

        /// <summary>
        /// Returns a list of dynamic objects whose properties and types match up with that database name.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="whereColumn">The where column name.</param>
        /// <param name="whereColumnEquals">What the were column should equal.</param>
        /// <returns></returns>
        public static IEnumerable<dynamic> DynamicAllRecords(DBFReader reader, string whereColumn = null,
                                                             dynamic whereColumnEquals = null)
        {

            return reader.DynamicAllRecords(whereColumn, (object)whereColumnEquals);
        }


        [Obsolete("DotNetDBF.Enumerable.IDBFIntercepter is the new interface name")]
        public interface IDBFIntercepter : DotNetDBF.Enumerable.IDBFIntercepter
        {

        }

        [Obsolete("DotNetDBF.Enumerable.DBFIntercepter is the new class name")]
        public class DBFIntercepter : DotNetDBF.Enumerable.BaseDBFIntercepter, IDBFIntercepter
        {
            public DBFIntercepter(object[] wrappedObj, string[] fieldNames)
                : base(wrappedObj, fieldNames)
            {
            }
        }

    }
}