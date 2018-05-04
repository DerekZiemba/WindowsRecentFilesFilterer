//using System;
//using System.IO;
//using System.Data;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Collections;
//using System.Collections.Generic;
//using System.Collections.Concurrent;
//using System.ComponentModel;
//using System.Data.SqlClient;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Reflection;
//using System.Reflection.Emit;
//using System.Runtime;
//using System.Runtime.CompilerServices;
//using System.Runtime.InteropServices;
//using System.Runtime.Serialization;
//using static System.Runtime.CompilerServices.MethodImplOptions;


//namespace ZMBA {

//   public static class RuntimeCompiler {

//      private static class RCCache<T> {       
//         public static object CtorLock = new object();
//         public static Func<T> DefaultConstructor;
//         public static T MultiArgConstructor;
//      }

//      private static T GetCachedOrCreate<T>(object lockobj, ref T cached, Func<object> factory) where T : class {
//         if(cached is null) {
//            lock(lockobj) {
//               if(cached != null) {
//                  return cached;
//               }
//               cached = (T)factory();
//            }
//         }
//         return cached;
//      }

//      #region Constructors

//      public static Func<T> GetDefaultConstructor<T>() {
//         return GetCachedOrCreate(RCCache<T>.CtorLock, ref RCCache<T>.DefaultConstructor, CreateDelegate);
//         object CreateDelegate() {
//            ConstructorInfo ctor = typeof(T).GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
//            LambdaExpression lambda = Expression.Lambda(typeof(Func<T>), Expression.New(ctor));
//            return lambda.Compile();
//         }
//      }

//      public static TDelegate GetConstructor<TDelegate>() where TDelegate : class {
//         return GetCachedOrCreate(RCCache<TDelegate>.CtorLock, ref RCCache<TDelegate>.MultiArgConstructor, CreateDelegate);
//         object CreateDelegate() {
//            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
//            Type delegateType = typeof(TDelegate);
//            MethodInfo invoker = delegateType.GetMethod("Invoke");
//            ParameterInfo[] invokerParams = invoker.GetParameters();
//            Type classType = invoker.ReturnType;

//            ParameterExpression[] args = new ParameterExpression[invokerParams.Length];
//            Type[] argTypes = new Type[args.Length];

//            for(int i = 0; i < invokerParams.Length; i++) {
//               args[i] = Expression.Parameter(argTypes[i] = invokerParams[i].ParameterType);
//            }

//            ConstructorInfo calleeInfo = classType.GetConstructor(flags, null, argTypes, null);
//            NewExpression calleeExp = Expression.New(calleeInfo, args);
//            LambdaExpression lambda = Expression.Lambda(delegateType, calleeExp, args);

//            return lambda.Compile();
//         }
//      }


//      #endregion Constructors


//      #region "Functions"


//      /// <summary>
//      /// Compiles a function to call a private or internal instance method with no performance cost after creation
//      /// </summary>
//      /// <typeparam name="TDelegate">Func<TRet>: A delegate where the first param is the instance and the final param is the result, if there is one.</typeparam>
//      public static TDelegate CompileFunctionCaller<TDelegate>(MethodInfo calleeInfo) {
//         Type delegateType = typeof(TDelegate);
//         ParameterInfo[] callerArgs = delegateType.GetMethod("Invoke").GetParameters();
//         ParameterInfo[] calleeArgs = calleeInfo.GetParameters();

//         Type[] callerArgTypes = new Type[callerArgs.Length];
//         Type[] calleeArgTypes = new Type[calleeArgs.Length];
//         ParameterExpression[] callerParams = new ParameterExpression[callerArgs.Length];
//         Expression[] calleeParams = new Expression[calleeArgs.Length];
//         Type instanceType = callerArgTypes[0] = callerArgs[0].ParameterType;
//         Expression instanceParam = callerParams[0] = Expression.Parameter(instanceType);

//         if(instanceType != calleeInfo.DeclaringType) {
//            instanceParam = Expression.Convert(instanceParam, calleeInfo.DeclaringType);
//         }

//         for(int idx = 1, ee = 0; idx < callerArgs.Length; idx++, ee++) {
//            calleeArgTypes[ee] = callerArgTypes[idx] = callerArgs[idx].ParameterType;
//            calleeParams[ee] = callerParams[idx] = Expression.Parameter(callerArgs[idx].IsOut ? callerArgTypes[idx].MakeByRefType() : callerArgTypes[idx]);
//            if(calleeArgs[ee].ParameterType != calleeArgTypes[ee]) {
//               calleeArgTypes[ee] = calleeArgs[ee].IsOut ? calleeArgs[ee].ParameterType.MakeByRefType() : calleeArgs[ee].ParameterType;
//               calleeParams[ee] = Expression.Convert(calleeParams[ee], calleeArgTypes[ee]);
//            }
//         }

//         return (TDelegate)(object)Expression.Lambda(delegateType, Expression.Call(instanceParam, calleeInfo, calleeParams), callerParams).Compile();
//      }

//      public static TDelegate CompileFunctionCaller<TDelegate>(Type instanceType, string name, Type[] args) {
//         BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.InvokeMethod;
//         return CompileFunctionCaller<TDelegate>(instanceType.GetMethod(name, flags, args));
//      }

//      public static TDelegate CompileFunctionCaller<TDelegate>(string name) {
//         ParameterInfo[] callerArgs = typeof(TDelegate).GetMethod("Invoke").GetParameters();
//         return CompileFunctionCaller<TDelegate>(callerArgs[0].ParameterType, name, callerArgs.Skip(1).Select(x => x.ParameterType).ToArray());

//         //BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.InvokeMethod;
//         //Type delegateType = typeof(TDelegate);
//         //ParameterInfo[] callerArgs = delegateType.GetMethod("Invoke").GetParameters();       
//         //ParameterExpression[] callerParams = new ParameterExpression[callerArgs.Length];
//         //ParameterExpression[] calleeParams = new ParameterExpression[callerArgs.Length-1];

//         //Type[] calleeArgTypes = new Type[callerArgs.Length-1];
//         //Type instanceType = callerArgs[0].ParameterType;
//         //callerParams[0] = Expression.Parameter(instanceType);

//         //for(int idx = 1, ee = 0; idx < callerArgs.Length; idx++, ee++) {          
//         //   calleeParams[ee] = callerParams[idx] = Expression.Parameter(calleeArgTypes[ee] = callerArgs[idx].ParameterType);
//         //}

//         //MethodInfo calleeInfo = instanceType.GetMethod(name, flags, calleeArgTypes);
//         //return (TDelegate)(object)Expression.Lambda(delegateType, Expression.Call(Expression.Parameter(instanceType), calleeInfo, calleeParams), callerParams).Compile();

//      }

//      public static TDelegate CompileFunctionCaller<TDelegate>() {
//         return CompileFunctionCaller<TDelegate>(typeof(TDelegate).Name);
//      }

//      /// <summary>
//      /// Compiles a function to call a private or internal static method with no performance cost after creation
//      /// </summary>
//      /// <typeparam name="TDelegate">Func<TRet>: A delegate where the first param is the instance and the final param is the result, if there is one.</typeparam>
//      public static TDelegate CompileStaticFunctionCaller<TDelegate>(MethodInfo calleeInfo) {
//         Type delegateType = typeof(TDelegate);
//         ParameterInfo[] callerArgs = delegateType.GetMethod("Invoke").GetParameters();
//         ParameterInfo[] calleeArgs = calleeInfo.GetParameters();

//         Type[] callerArgTypes = new Type[callerArgs.Length];
//         Type[] calleeArgTypes = new Type[calleeArgs.Length];
//         ParameterExpression[] callerParams = new ParameterExpression[callerArgs.Length];
//         Expression[] calleeParams = new Expression[calleeArgs.Length];

//         for(int idx = 0; idx < callerArgs.Length; idx++) {
//            calleeArgTypes[idx] = callerArgTypes[idx] = callerArgs[idx].ParameterType;
//            calleeParams[idx] = callerParams[idx] = Expression.Parameter(callerArgs[idx].IsOut ? callerArgTypes[idx].MakeByRefType() : callerArgTypes[idx]);
//            if(calleeArgs[idx].ParameterType != calleeArgTypes[idx]) {
//               calleeArgTypes[idx] = calleeArgs[idx].IsOut ? calleeArgs[idx].ParameterType.MakeByRefType() : calleeArgs[idx].ParameterType;
//               calleeParams[idx] = Expression.Convert(calleeParams[idx], calleeArgTypes[idx]);
//            }
//         }

//         MethodCallExpression calleeExp = Expression.Call(null, calleeInfo, calleeParams);
//         LambdaExpression lambda = Expression.Lambda(delegateType, calleeExp, callerParams);
//         return (TDelegate)(object)lambda.Compile();
//      }

//      public static TDelegate CompileStaticFunctionCaller<TDelegate>(Type type, string name, Type[] args) {
//         BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.InvokeMethod;
//         MethodInfo calleeInfo = type.GetMethod(name, flags, args) ?? type.GetMethod(name, flags | BindingFlags.FlattenHierarchy, args);
//         return CompileStaticFunctionCaller<TDelegate>(calleeInfo);
//      }

//      public static TDelegate CompileStaticFunctionCaller<TDelegate>(Type type, string name) {
//         return CompileStaticFunctionCaller<TDelegate>(type, name, typeof(TDelegate).GetMethod("Invoke").GetParameters().Select(x => x.ParameterType).ToArray());
//      }


//      #endregion


//      #region Properties 


//      public static Func<TInst, TValue> CompilePropertyReader<TInst, TValue>(string name) {
//         BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.GetProperty;
//         Type instType = typeof(TInst);
//         ParameterExpression instanceParam = Expression.Parameter(instType);
//         PropertyInfo prop = instType.GetProperty(name, flags, typeof(TValue));
//         LambdaExpression lambda = Expression.Lambda(typeof(Func<TInst, TValue>), Expression.Property(instanceParam, prop), instanceParam);
//         return (Func<TInst, TValue>)lambda.Compile();
//      }

//      public static Action<TInst, TValue> CompilePropertyWriter<TInst, TValue>(string name) {
//         BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.SetProperty;
//         Type instType = typeof(TInst);
//         ParameterExpression instanceParam = Expression.Parameter(instType);
//         PropertyInfo prop = instType.GetProperty(name, flags, typeof(TValue));
//         LambdaExpression lambda = Expression.Lambda(typeof(Action<TInst, TValue>), Expression.Property(instanceParam, prop), instanceParam);
//         return (Action<TInst, TValue>)lambda.Compile();
//      }

//      #endregion



//      #region Fields 


//      public static Func<TInst, TValue> CompileFieldReader<TInst, TValue>(string name) {
//         BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.GetField;
//         Type instType = typeof(TInst);
//         ParameterExpression instanceParam = Expression.Parameter(instType);
//         LambdaExpression lambda = Expression.Lambda(typeof(Func<TInst, TValue>), Expression.Field(instanceParam, instType.GetField(name, flags)), instanceParam);
//         return (Func<TInst, TValue>)lambda.Compile();
//      }

//      public static Action<TInst, TValue> CompileFieldWriter<TInst, TValue>(string name) {
//         BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.SetField;
//         Type instType = typeof(TInst);
//         ParameterExpression instanceParam = Expression.Parameter(instType);
//         LambdaExpression lambda = Expression.Lambda(typeof(Action<TInst, TValue>), Expression.Field(instanceParam, instType.GetField(name, flags)), instanceParam);
//         return (Action<TInst, TValue>)lambda.Compile();
//      }


//      #endregion


//   }

//}
