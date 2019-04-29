using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EnumReader {
    public class EnumValidator {
        private readonly Action<String> _logError;
        private readonly Action<String> _logInfo;
        
        private Assembly _loadedAssembly;
        
        // Delegates for event handlers. Implemented by user.
        public delegate void LogEnumNameDelegate(String name);
        public delegate void LogTuplesDelegate(IList<(Int32 value, String enumValues)> tuples);
        public delegate void LogInconsistentEnumValuesDelegate(String previousName
            , String currentName
            , Int32 previousValue
            , Int32 currentValue);

        // Event handlers. User binds to these.
        public event LogEnumNameDelegate LogEnumName;
        public event LogTuplesDelegate LogEnumDuplicates;
        public event LogInconsistentEnumValuesDelegate LogEnumNonConsistentValues;

        public EnumValidator(Action<String> logInfo = null, Action<String> logError = null) {
            _logError = logError;
            _logInfo = logInfo;
        }

        /// <summary>
        /// Set reference to pre-loaded assembly.
        /// </summary>
        /// <param name="loadedAssembly"></param>
        public void SetLoadedAssembly(Assembly loadedAssembly) {
            _loadedAssembly = loadedAssembly;
        }

        /// <summary>
        /// Load assembly from file path.
        /// </summary>
        /// <param name="filePath"></param>
        /// <exception cref="NullReferenceException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public void LoadAssemblyFromFilePath(String filePath) {
            // Get target file and directory.
            String assemblyFilePath = filePath;
            String assemblyDirectory = Path.GetDirectoryName(assemblyFilePath);
            
            if (assemblyFilePath == null) throw new NullReferenceException(nameof(assemblyFilePath));
            if (assemblyDirectory == null) throw new NullReferenceException(nameof(assemblyDirectory));

            if (!File.Exists(assemblyFilePath)) {
                _logError?.Invoke($"Could not find file {assemblyFilePath}");
                return;
            }
            
            // Create custom assembly resolver to load the dependent DLLs from target directory. 
            AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) => {
                if (eventArgs == null) throw new ArgumentNullException(nameof(eventArgs));
                if (eventArgs.Name == null) throw new ArgumentNullException(nameof(eventArgs.Name));
                
                try {
                    String fileName = $"{eventArgs.Name.Split(',')[0]}.dll".ToLower(); 
                    String fullPath = Path.Combine(assemblyDirectory, fileName);
                    return Assembly.LoadFrom(fullPath);
                } catch (Exception) {
                    return null;
                }
            };

            Assembly loadedAssembly;
            try {
                loadedAssembly = Assembly.LoadFrom(assemblyFilePath);
            } catch (Exception e) {
                _logError?.Invoke($"Failed to load assembly: {e}");
                return;
            }
            
            if (loadedAssembly == null) throw new InvalidOperationException($"Null {loadedAssembly}");
            _loadedAssembly = loadedAssembly;
        }

        public void Run() {
            if (_loadedAssembly == null) throw new InvalidOperationException($"{nameof(_loadedAssembly)} was null");

            _logInfo?.Invoke($"Loaded assembly {_loadedAssembly.FullName}");
            
            // Get enums from loaded assembly.
            Type[] q = _loadedAssembly.GetTypes();
            IEnumerable<Type> enums = q.Where(type => type.IsEnum);

            // Iterate assemblies.
            foreach (Type e in enums) {
                LogEnumName?.Invoke(e.Name);
                
                List<String> enumNames = Enum.GetNames(e).ToList();
                IDictionary<Int32, IList<String>> membersByInt = new Dictionary<Int32, IList<String>>();

                Int32? previousEnumValue = null;
                String previousEnumName = null;
                
                // Iterate all enum members and gather the values for each integer key.
                foreach (String enumName in enumNames) {
                    Int32 enumValue = Convert.ToInt32(Enum.Parse(e, enumName));
                    if (!membersByInt.ContainsKey(enumValue)) {
                        membersByInt[enumValue] = new List<String>();
                    }

                    // If the value is not incremented by one, log it.
                    if (previousEnumValue.HasValue && previousEnumValue.Value != enumValue + 1) {
                        LogEnumNonConsistentValues?.Invoke(previousEnumName
                            , enumName
                            , previousEnumValue.Value
                            , enumValue);
                    }
                    previousEnumValue = enumValue;
                    previousEnumName = enumName;
                    
                    membersByInt[enumValue]?.Add(enumName);
                }

                // Find out names with duplicate integer keys (len > 1).
                IList<(Int32 value, String enumValues)> valueTuples = membersByInt
                    .Where(pair => pair.Value != null)
                    .Where(pair => pair.Value.Count > 1)
                    .Select(pair => (pair.Key, string.Join(", ", pair.Value)))
                    .ToList();

                if (!valueTuples.Any()) continue;

                // Output duplicate names.
                LogEnumDuplicates?.Invoke(valueTuples);
            }
        }
    }
}