using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using LinFu.DynamicProxy;

namespace DotNetDBF.Enumerable
{
    static public class Enuemrable
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
                while (t != null)
                {

                    tReturn.Add((T)Activator.CreateInstance(tType, t));
                    t = aReader.NextRecord(tProps);
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


                var tReturn = new List<T>();
                object[] t = aReader.NextRecord(tProps);  
                var tFactory = new ProxyFactory();
                Type tNewType = tFactory.CreateProxyType(
                           tType,
                           new[] { typeof(DBFIntercepter.IDBFObjectArrayWrapped) });
                  

            while (t != null)
                {
                  
                     var proxyInstance = (IProxy)Activator.CreateInstance(tNewType);
                    proxyInstance.Interceptor = new DBFIntercepter(t, tProperties);


                    tReturn.Add((T)proxyInstance);
                    t = aReader.NextRecord(tProps);
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

                return _wrapper[tIndex];
            }

            #endregion
        }
    }
}
