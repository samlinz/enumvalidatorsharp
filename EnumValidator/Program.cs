using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EnumReader {
    /// <summary>
    /// Enum validator enumerates an external assembly, iterates all enum members and invokes
    /// relevant events to inform about inconsistencies, like duplicate integer values for enums.
    /// </summary>
    internal static class Program {
        /// <summary>
        /// Program entry point.
        /// </summary>
        /// <param name="args">Command line arguments, only argument is DLL/EXE file path.</param>
        /// <exception cref="ArgumentNullException"></exception>
        private static void Main(String[] args) {
            // Validate arguments.
            if (args == null) throw new ArgumentNullException(nameof(args));
            if (args.Length != 1) {
                Console.Error.WriteLine("Missing assembly as argument");
                Console.Read();
                return;
            }

            // Create validator object.
            var enumValidator = new EnumValidator(Console.WriteLine
                , s => {
                    Console.Error.WriteLine(s);
                    Console.Read();
                }
            );
            
            // Log enum names in event handler.
            enumValidator.LogEnumName += name => {
                Console.WriteLine($"Found enum {name}");
            };
            
            // Log duplicate enum members in event handler.
            enumValidator.LogEnumDuplicates += tuples => {
                if (tuples == null) throw new ArgumentNullException(nameof(tuples));

                Console.WriteLine("");
                Console.WriteLine(new String('*', 10));
                Console.WriteLine($"WARNING! Found duplicate enum integers");

                foreach ((Int32 value, String enumValues) duplicates in tuples) {
                    Console.WriteLine($"VALUE {duplicates.value} KEYS '{duplicates.enumValues}'");
                }

                Console.WriteLine(new String('*', 10));
                Console.WriteLine("");
            };
            
            // Load assembly.
            enumValidator.LoadAssemblyFromFilePath(args[0]);

            // Run validator.
            enumValidator.Run();

            Console.WriteLine("Done!");
            Console.Read();
        }
    }
}