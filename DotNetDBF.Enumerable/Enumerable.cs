using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using LinFu.DynamicProxy;

namespace DotNetDBF.Enumerable
{
    static public class Enuemrable
    {


         static private ModuleBuilder _builder;
        static public ModuleBuilder Builder
        {
            get
            {
                if (_builder == null) {
                    var tPlainName = "DotNetDBFEnumProxyInterfaceDynamicAssembly";
                    var tName = new AssemblyName(tPlainName);


#if DEBUG
                    var access = AssemblyBuilderAccess.RunAndSave;
#else
            var access = AssemblyBuilderAccess.Run;
#endif
                    var ab =
                            AppDomain.CurrentDomain.DefineDynamicAssembly(
                                    tName,
                                    access);
#if DEBUG
                    _builder =
                            ab.DefineDynamicModule(tName.Name,String.Format(
                  "{0}.mod", tPlainName), true);
                    
#else
           _builder =
                            ab.DefineDynamicModule(tName.Name);
#endif
                }
                return _builder;
            }
        }


        static private Dictionary<string, Type> _typeHash = new Dictionary<string, Type>();
   

        static public Type DynamicType(this DBFReader aReader)
        {
            lock ("com.dotnetdbf.typehash")
            {
                var tFields = aReader.GetSelectFields();
                var tHash = aReader.GetSelectFields().Aggregate("",(accum,each)=> string.Format("{0}|{1}:{2}", accum, each.Name.ToLower(), each.DataType));

                if (_typeHash.ContainsKey(tHash))
                {
                    return _typeHash[tHash];
                }
                var tDataSet = tFields;

                var tb = Builder.DefineType(
                        "A" + Guid.NewGuid().ToString("N"),
                        TypeAttributes.Abstract | TypeAttributes.Public | TypeAttributes.Class,
                        typeof(object));

                foreach (DBFField tColumn in tDataSet)
                {
                    var tPropName = tColumn.Name.ToLower();
                    var tPropType = tColumn.Type;
                    var pb = tb.DefineProperty(
                            tPropName, System.Reflection.PropertyAttributes.None, tPropType, null);


                    var tGet = tb.DefineMethod(
                            "get_" + tPropName,
                            MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.SpecialName
                            | MethodAttributes.Public,
                            tPropType,
                            null);

                    pb.SetGetMethod(tGet);
                }

                var tType = tb.CreateType();
                _typeHash.Add(tHash, tType);
                return tType;
            }
        }


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

                var tOrderedProps = tProps.OrderBy(it => it).ToArray();

                var tReturn = new List<T>();
                object[] t = aReader.NextRecord(tProps,tOrderedProps);
                while (t != null)
                {

                    tReturn.Add((T)Activator.CreateInstance(tType, t));
                    t = aReader.NextRecord(tProps, tOrderedProps);
                }



                return tReturn;
            }

            return AllRecords<T>(aReader);
        }

        static public IEnumerable<T> AllRecords<T>(this DBFReader aReader)
        {
            var tType = typeof(T);

                var tProperties = tType.GetProperties();
                var tProps = tProperties
                    .Select(
                    it =>
                    Array.FindIndex(aReader.Fields,
                                    jt => jt.Name.Equals(it.Name, StringComparison.InvariantCultureIgnoreCase))).ToArray();

                var tOrderedProps = tProps.OrderBy(it => it).ToArray();
                var tReturn = new List<T>();
                object[] t = aReader.NextRecord(tProps, tOrderedProps);  
                var tFactory = new ProxyFactory();
                Type tNewType = tFactory.CreateProxyType(
                           tType,
                           new[] { typeof(DBFIntercepter.IDBFObjectArrayWrapped) });
                  

            while (t != null)
                {
                  
                     var proxyInstance = (IProxy)Activator.CreateInstance(tNewType);
                    proxyInstance.Interceptor = new DBFIntercepter(t, tProperties);


                    tReturn.Add((T)proxyInstance);
                    t = aReader.NextRecord(tProps, tOrderedProps);
                }



                return tReturn;
            
        }

        static public IEnumerable<object> DynamicAllRecords(this DBFReader aReader, Type type)
        {
            return DynamicAllRecords(aReader, type, null, null);
        }


        static public IEnumerable<object> DynamicAllRecords(this DBFReader aReader, Type type, string whereColumn, object whereColumnEquals)
        {
            var tType = type;

            var tProperties = tType.GetProperties();

            int? tWhereColumn=null;
            if(!String.IsNullOrEmpty(whereColumn))
            {
                tWhereColumn = Array.FindIndex(aReader.Fields,
                                it => it.Name.Equals(whereColumn, StringComparison.InvariantCultureIgnoreCase));
            }

            var tProps = tProperties
                .Select(
                it =>
                Array.FindIndex(aReader.Fields,
                                jt => jt.Name.Equals(it.Name, StringComparison.InvariantCultureIgnoreCase))).ToArray();

            var tOrderedProps = tProps.OrderBy(it => it).ToArray();
            var tReturn = new List<object>();
            object[] t = aReader.NextRecord(tProps, tOrderedProps);
            var tFactory = new ProxyFactory();
            Type tNewType = tFactory.CreateProxyType(
                       tType,
                       new[] { typeof(DBFIntercepter.IDBFObjectArrayWrapped) });


            while (t != null)
            {
                if (tWhereColumn.HasValue)
                {
                    if(!t[tWhereColumn.Value].Equals(whereColumnEquals))
                    {
                        t = aReader.NextRecord(tProps, tOrderedProps);
                        continue;
                    }
                }

                var proxyInstance = (IProxy)Activator.CreateInstance(tNewType);
                proxyInstance.Interceptor = new DBFIntercepter(t, tProperties);
          

                tReturn.Add(proxyInstance);
                t = aReader.NextRecord(tProps, tOrderedProps);
            }



            return tReturn;

        }


        public class DBFIntercepter : IInterceptor
        {
            public interface IDBFObjectArrayWrapped
            {
                object[] GetDataRow();
            }
          


            private object[] _wrapper;
            private PropertyInfo[] _props;

            public DBFIntercepter(object[] aWrapper,PropertyInfo[] aProps)
            {
                _wrapper = aWrapper;
                _props = aProps;
            }

            #region Implementation of IInterceptor
            public object Intercept(InvocationInfo info)
            {
                if (info.TargetMethod.Name.Contains("get_"))
                {
                    return GetProperty(info);
                }

                if (info.TargetMethod.Name.Equals("GetDataRow"))
                {
                    return _wrapper;
                }
                   var tNewArgs =
                        info.Arguments.Select(
                                it => it.GetType() == info.Target.GetType()
                                    ? ((IDBFObjectArrayWrapped)it).GetDataRow() : it).ToArray();
                return info.TargetMethod.Invoke(_wrapper, tNewArgs);

            }

            private object GetProperty(InvocationInfo info)
            {
                var tLookup = info.TargetMethod
                        .Name
                        .Replace("get_",
                                 String.Empty)
                        .ToLower();
                var tIndex = Array.FindIndex(_props,
                                             it => it.Name.Equals(tLookup, StringComparison.InvariantCultureIgnoreCase));

                return _wrapper[tIndex] ?? Activator.CreateInstance(info.TargetMethod.ReturnType);
            }

            #endregion
        }
    }
}
