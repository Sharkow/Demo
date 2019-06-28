using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using TestDataGeneratorSH.OutputGeneration;
using System.Runtime.Serialization;
using ScriptFunctionsLib;

namespace TestDataGeneratorSH.ValueGenerators.Sensors
{
    /// <summary>
    /// Скриптовый генератор.
    /// Генерируемое значение определяется скриптом, написанным пользователем.
    /// </summary>
    [DataContract]
    [KnownType(typeof(DataGenRoutine.SensorFieldTypeTitles))]
    public class StringScriptGenerator : SensorValGeneratorBase
    {
#region Fields

        DynamicCodeExecutorSH scriptExecutor;

        [DataMember]
        string scriptText;

        [DataMember]
        List<DataGenRoutine.SensorFieldTypeTitles> requiredSeedTypes;
        
        /// <summary>
        /// Доступные команды в скриптах для датчиков (свойства, методы, ключевые слова...),
        /// для отображения в виде подсказки.
        /// </summary>
        static readonly string[] SENSOR_SCRIPT_CUSTOM_COMMANDS =
        { "inputs[]", "elapsedMS", "ran", "startDateTime", "customObjects[]" };
        static readonly string[] SENSOR_SCRIPT_STANDARD_COMMANDS =
        { "bool", "double", "int", "uint", "long", "ulong", "short", "ushort", "decimal", "float",
            "string", "char", "byte", "object", "if", "while", "for", "do", "foreach", "brake", "continue",
            "switch", "true", "false", "null", "return" };

        static string[] namespaces = new string[] 
                                { 
                                    "System",
                                    "ScriptFunctionsLib", 
                                    "System.Collections",
                                    "System.Collections.Generic",
                                    "System.Linq",
                                    "System.Text"
                                };

        static string[] assemblyNames = new string[]
                                {
                                    "System.dll",
                                    typeof(ScriptFunctions).Assembly.Location,
                                    "System.Core.dll",
                                    "System.Data.dll",
                                };


        static Random ran;

        /// <summary>
        /// Список объектов, который доступен при написании скрипта.
        /// Предназначен для хранения любых данных для нужд скрипта, это возможность хранить данные между
        /// обновлениями датчика, то есть между вызовами скрипта.
        /// </summary>
        static List<object> customObjects;

#endregion


#region Properties

        public static string[] AutoCompleteList
        {
            get
            {
                string []libMethods = ScriptFunctions.AvailableMethodNames;
                for (int i = 0; i < libMethods.Length; i++)
                    libMethods[i] += "()";

                return SENSOR_SCRIPT_CUSTOM_COMMANDS.Concat(SENSOR_SCRIPT_STANDARD_COMMANDS.Concat(libMethods)).ToArray();
            }
        }


        public string ScriptText
        {
            get { return scriptText; }
            set { scriptText = value; }
        }

        

        public override DataGenRoutine.SensorFieldTypeTitles[] RequiredSeedTypes
        {
            get { return (requiredSeedTypes != null) ? requiredSeedTypes.ToArray() : new DataGenRoutine.SensorFieldTypeTitles[0]; }
        }

        public List<DataGenRoutine.SensorFieldTypeTitles> RequiredSeedTypeList
        {
            get { return requiredSeedTypes; }
        }
#endregion


#region Public Methods

        protected override void Initialize()
        {
            scriptText = "";
            requiredSeedTypes = new List<DataGenRoutine.SensorFieldTypeTitles>();

            scriptExecutor = new DynamicCodeExecutorSH();
        }

        public override string PrepareForGenerationResults()
        {
            try
            {
                scriptExecutor.Compile(PreprocessScript(), "object",
                    "string[] inputs, ulong elapsedMS, Random ran, DateTime startDateTime, List<object> customObjects",
                    assemblyNames, namespaces);
            }
            catch (ScriptCompilingException e)
            {
                scriptExecutor.Unload();

                string compileErrorsInfo = "Ошибки компиляции скрипта:";

                foreach (System.CodeDom.Compiler.CompilerError cErr in e.CompilerErrors)
                    compileErrorsInfo += "\n\nСтрока " + cErr.Line + ": " + cErr.ErrorText;

                return compileErrorsInfo;
            }

            //Параметры скрипта важно сбрасывать перед каждым циклом генерации!
            ran = new Random();
            customObjects = new List<object>();

            return null;
        }

        public override string GetValue(ulong elapsedMS, string[] latestSeedValues = null)
        {
            return scriptExecutor.Execute(new object[] { latestSeedValues, elapsedMS, ran, StartDateTime, customObjects }).ToString();
        }

        public override void PostGeneration()
        {
            scriptExecutor.Unload();
        }

        public override void CheckValidity(object sender = null)
        {
            Valid = scriptText.Contains("return ");
        }


        /// <summary>
        /// Мы хотим позволить пользователю вызывать методы из библиотеки ScriptFunctionsLib непосредственно,
        /// без необходимости писать имя класса. Для этого используем препроцессинг - к каждому имени библиотечного метода
        /// прибавляем имя класса - ScriptFunctions.
        /// </summary>
        string PreprocessScript()
        {
            string[] scriptFunctionList = ScriptFunctions.AvailableMethodNames;
            string res = scriptText;

            foreach (string funcName in scriptFunctionList)
                res = res.Replace(funcName, "ScriptFunctions." + funcName);

            return res;
        }
#endregion

    }
}