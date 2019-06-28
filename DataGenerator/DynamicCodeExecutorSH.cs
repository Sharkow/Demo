using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CSharp;
using System.Runtime.Serialization;

namespace TestDataGeneratorSH
{
    /// <summary>
    /// Компилирует и исполняет переданный скрипт в отдельном домене, во избежание утечек памяти.
    /// </summary>
    public class DynamicCodeExecutorSH
    {
        bool canExecute = false;
        AppDomain workerAppDomain;
        AppDomainSetup appDomainSetup;
        DynamicCompiler compiler;

        #region Methods

        /// <summary>
        /// Компилирует переданный скрипт в отдельном домене. После этого можно вызывать метод Execute.
        /// Во избежание утечек памяти после окончания использования скрипта следует вызвать Unload для выгрузки домена!
        /// </summary>
        /// <param name="script">Скрипт, который необходимо сгенерировать, в виде строки.
        /// В действительности скрипт является содержимым метода, который будет вызван при Execute.
        /// Таким образом, в скрипте допустимы только те конструкции, которые допустимы внутри метода C#.
        /// Нельзя объявлять классы, методы, добавлять using пространства имён и т.д.</param>
        /// <param name="returnType">Тип возвращаемого скриптом значения в виде строки.
        /// При несответствии фактическому возвращенному значению выбрасывается исключение с ошибкой компиляции.</param>
        /// <param name="parameterSignature">Сигнатура параметров метода, в виде строки.
        /// В скрипте будут доступны переданные параметры. При вызове Execute объект args должен соответствовать сигнатуре.</param>
        /// <param name="assemblies">Имена файлов сборок (dll, exe), которые должны быть доступны в скрипте.</param>
        /// <param name="namespaces">Пространства имён, доступные скрипту. (Будут прописаны с директивой using).</param>
        public void Compile(string script, string returnType, string parameterSignature, string[] assemblies, string[] namespaces)
        {
            appDomainSetup = new AppDomainSetup();
            appDomainSetup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
            appDomainSetup.DisallowBindingRedirects = false;
            appDomainSetup.DisallowCodeDownload = true;
            appDomainSetup.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;

            workerAppDomain = AppDomain.CreateDomain("Compiler", null, appDomainSetup);

            string usingStatements = string.Empty;

            if (namespaces != null)
                foreach (string ns in namespaces)
                    usingStatements += "using " + ns + "; ";

            //Чтобы можно было корректно выдать номер строки с ошибкой в скрипте, сам скрипт должен начинаться с первой строки.
            string code = usingStatements +
                @"namespace ScriptInterfaceSH { public static class MainDynamicClass { public static " + returnType +
                " MainDynamicMethod(" + parameterSignature + ") { " + script + " } } }";


            //Компилятор создаётся в отдельном домене. Домен можно выгрузить, избежав утечек памяти.
            //Если компилировать скрипт в основном домене, то скомпилированные сборки невозможно будет выгрузить - это будут утечки памяти.
            compiler = (DynamicCompiler)workerAppDomain.CreateInstanceAndUnwrap(
                typeof(DynamicCompiler).Assembly.GetName().Name,
                typeof(DynamicCompiler).FullName);

            compiler.Compile(code, assemblies, "MainDynamicMethod");

            canExecute = true;
        }

        /// <summary>
        /// Возвращает объект, возвращенный скриптом.
        /// Перед вызовом необходимо скомпилировать скрипт методом Compile.
        /// </summary>
        public object Execute(object[] args)
        {
            if (!canExecute)
                throw new Exception("Attempt to execute script before compiling!");
            
            return compiler.Execute(args);
        }

        /// <summary>
        /// Выгружает скомпилированную сборку из памяти. Вызывайте этот метод во избежание утечек памяти после окончания использования
        /// скомпилированного скрипта. После данного вызова метод Execute не может быть вызван, пока не будет скомпилирован новый скрипт (Compile).
        /// </summary>
        public void Unload()
        {
            canExecute = false;
            AppDomain.Unload(workerAppDomain);
        }

        #endregion
    }

    #region Dynamic Compilers

    [Serializable]
    internal class DynamicCompiler
    {
        Assembly generatedAssembly;
        MethodInfo methodToExecute;

        public void Compile(string expressionCode, string[] assemblies, string methodToExecute)
        {
            CompilerParameters compilerParameters = new CompilerParameters(assemblies);
            compilerParameters.GenerateExecutable = false;
            compilerParameters.GenerateInMemory = true;

            Dictionary<string, string> providerOptions = new Dictionary<string, string>();
            providerOptions.Add("CompilerVersion", "v4.0");
            CodeDomProvider codeProvider = new CSharpCodeProvider(providerOptions);

            using (codeProvider)
            {
                CompilerResults results = codeProvider.CompileAssemblyFromSource(compilerParameters, expressionCode);

                if (results.Errors.Count > 0)
                {
                    throw new ScriptCompilingException(results.Errors);
                }
                else
                {
                    generatedAssembly = results.CompiledAssembly;
                }
            }
            this.methodToExecute = generatedAssembly.GetExportedTypes()[0].GetMethod(methodToExecute, BindingFlags.Public | BindingFlags.Static);
        }

        public object Execute(/*string type, string method, */object[] args)
        {
            if (generatedAssembly == null)
                throw new InvalidOperationException();

            return this.methodToExecute.Invoke(null, args);
        }
    }
    #endregion


    [Serializable]
    public class ScriptCompilingException : Exception
    {
        CompilerErrorCollection errors;

        /// <summary>
        /// Список ошибок и warnings, которые возникли в процессе компиляции
        /// </summary>
        public CompilerErrorCollection CompilerErrors
        {
            get { return errors; }
        }

        public ScriptCompilingException(CompilerErrorCollection errors)
        {
            this.errors = errors;
        }

        /// <summary>
        /// Чтобы Exception можно было кидать из DynamicCompiler, он тоже должен быть сериализуемым.
        /// При этом, должен быть определен такой конструктор, иначе будет гг...
        /// </summary>
        protected ScriptCompilingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
